using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Kinect;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using KinectShowcaseCommon.Kinect_Processing;
using KinectShowcaseCommon.Helpers;
using KinectShowcaseCommon.ProcessHandling;
using System.Diagnostics;

namespace KinectLogin
{
    public partial class MainWindow : Window, KinectManager.StateListener
    {
        private const float HARD_THRESHOLD = 0.8f;
        private const float MEDIUM_THRESHOLD = 1.5f;
        private const float EASY_THRESHOLD = 3.0f;
        private const int numJoints = 25;
        private const int numRecords = 3;

        private KinectSensor kinectsensor;
        private string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\";

        enum ScreenStates
        {
            HOME, TUTORIAL, SELECTUSER_ENROLL, SELECTUSER_LOGIN, RECORDING_ENROLL,
            RECORDING_LOGIN_GUESS, RECORDING_LOGIN_GIVEN, RESULT
        };

        //Algorithm based variables
        // the very PAINFUL remapping of joint points fml
        //unfortunately, need to remap the skeleton points to original storage
        //in the future hopefully this is less... silly
        int[] jointorder = { (int)JointType.Head, (int)JointType.Neck,(int)JointType.SpineShoulder,
                               (int)JointType.ShoulderLeft, (int)JointType.ElbowLeft,
                               (int)JointType.WristLeft, (int)JointType.HandLeft, (int)JointType.ThumbLeft, (int)JointType.HandTipLeft,
                               (int)JointType.ShoulderRight, (int)JointType.ElbowRight,
                               (int)JointType.WristRight, (int)JointType.HandRight, (int)JointType.ThumbRight, (int)JointType.HandTipRight,
                               (int)JointType.SpineMid, (int)JointType.SpineBase,
                               (int)JointType.HipLeft, (int)JointType.KneeLeft,
                               (int)JointType.AnkleLeft, (int)JointType.FootLeft,
                               (int)JointType.HipRight, (int)JointType.KneeRight,
                               (int)JointType.AnkleRight, (int)JointType.FootRight};

        //given state
        ScreenStates currentState;
        int currentUser;

        //counts number of recording
        int recordNumber;
        //Screen Buttons
        List<Button> allButtons, welcomeButtons, selectUserButtons;

        //Global Timer
        DispatcherTimer timer;
        int timerCount;

        //global skeleton buffer
        Body[] bStorage;
        BodyFrameReader bodyFrameReader = null;
        //recordingStuffs
        RecordingBuffer rb;

        float acceptThreshold = 5.0f;

        //reuse of old Data allocation -- can support more "advanced" modes but kinda overkill in this mode
        MathNet.Numerics.LinearAlgebra.Generic.Matrix<float>[][][] dataStore;
        string[] userNames;
        bool[] userHasData; //simple storage to check if data has been allocated
        bool dataEnrolled;

        private KinectManager kinectManager;
        private Point DEBUG_currentKinectHandPos = new Point(0.5f, 0.5f);
        private bool DEBUG_handIsOpen = true;

        /// <summary>
        /// Array of arrays of contiguous line segements that represent a skeleton.
        /// </summary>
        private static readonly JointType[][] SkeletonSegmentRuns = new JointType[][]
        {
            new JointType[]
            {
                JointType.Head, JointType.SpineShoulder, JointType.SpineMid, JointType.SpineBase
            },
            new JointType[]
            {
                JointType.HandTipLeft, JointType.ThumbLeft, JointType.HandLeft, JointType.WristLeft, JointType.ElbowLeft, JointType.ShoulderLeft,
                JointType.SpineShoulder,
                JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight, JointType.ThumbRight, JointType.HandTipRight
            },
            new JointType[]
            {
                JointType.FootLeft, JointType.AnkleLeft, JointType.KneeLeft, JointType.HipLeft,
                JointType.SpineBase,
                JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight
            }
        };

        public MainWindow()
        {

            //this must ALWAYS be first
            InitializeComponent();

            //// one sensor is currently supported
            this.kinectsensor = KinectSensor.GetDefault();
            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectsensor.DepthFrameSource.FrameDescription;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectsensor.BodyFrameSource.OpenReader();
            this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            // open the sensor
            kinectsensor.Open();
            recordNumber = 0;
            //initialization Buttons for visibility


            initButtons();

            this.SetThreshold(HARD_THRESHOLD);

            currentUser = -1; //bugs out?!
            currentState = ScreenStates.HOME;

            //initialize global skeleton buffer
            bStorage = new Body[6]; //"hard"-coded limit of 6 (more like 2)

            //setup timer
            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(timerTick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100); //trigger every 50 ms (faster updates in progress bar)
            timerCount = 0;

            //initialize dataStore -- 4 user limit
            dataStore = new MathNet.Numerics.LinearAlgebra.Generic.Matrix<float>[4][][];
            for (int i = 0; i < dataStore.Length; i++)
            {
                //for each user give space for "1" gesture
                dataStore[i] = new MathNet.Numerics.LinearAlgebra.Generic.Matrix<float>[1][];
                for (int j = 0; j < dataStore[i].Length; j++)
                {
                    //each gesture give space for "1" sample of the gesture
                    dataStore[i][j] = new MathNet.Numerics.LinearAlgebra.Generic.Matrix<float>[numRecords];
                }
            }
            userNames = new string[]{"User 1", "User 2", "User 3", "User 4"};
            userHasData = new bool[4] { false, false, false, false };
            dataEnrolled = false;

            //set up recordingBuffer
            rb = new RecordingBuffer(5 * 30, 25); //full joints 5 seconds;
            rb.clearBuffer();


            //set state to welcome
            setProgramState(ScreenStates.HOME);


            this.kinectManager = KinectManager.Default;

            //setup the cursor with images
            Image[] cursorImages = new Image[3];
            Image cursorImage0 = new Image();
            cursorImage0.Source = ImageExtensions.ToBitmapSource(KinectLogin.Properties.Resources.hand1);
            cursorImages[0] = cursorImage0;
            Image cursorImage1 = new Image();
            cursorImage1.Source = ImageExtensions.ToBitmapSource(KinectLogin.Properties.Resources.hand2);
            cursorImages[1] = cursorImage1;
            Image cursorImage2 = new Image();
            cursorImage2.Source = ImageExtensions.ToBitmapSource(KinectLogin.Properties.Resources.hand3);
            cursorImages[2] = cursorImage2;

            this.cursorView.SetCursorImages(cursorImages);

            this.kinectManager.HandManager.Cursor = this.cursorView;
            this.kinectManager.AddStateListener(this);

            //MessageBox.Show("Success!");
            //Loaded += OnLoaded;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            SystemCanary.Default.SystemDidRecieveInteraction();

            const float increment = 0.01f;
            if (e.Key == Key.A) //left
            {
                DEBUG_currentKinectHandPos.X -= increment;
                this.kinectManager.HandManager.InjectNormalizedHandLocation(DEBUG_currentKinectHandPos);
            }
            else if (e.Key == Key.D) //right
            {
                DEBUG_currentKinectHandPos.X += increment;
                this.kinectManager.HandManager.InjectNormalizedHandLocation(DEBUG_currentKinectHandPos);
            }
            else if (e.Key == Key.W) //up
            {
                DEBUG_currentKinectHandPos.Y -= increment;
                this.kinectManager.HandManager.InjectNormalizedHandLocation(DEBUG_currentKinectHandPos);
            }
            else if (e.Key == Key.S) //down
            {
                DEBUG_currentKinectHandPos.Y += increment;
                this.kinectManager.HandManager.InjectNormalizedHandLocation(DEBUG_currentKinectHandPos);
            }
            else if (e.Key == Key.Space || e.Key == Key.Q) //toggle open close
            {
                DEBUG_handIsOpen = !DEBUG_handIsOpen;
                HandState state = (DEBUG_handIsOpen ? HandState.Open : HandState.Closed);
                this.kinectManager.HandManager.InjectHandStateChange(state);
            }
            else if (e.Key == Key.M)
            {
                Process currentProc = Process.GetCurrentProcess();
                long memoryUsed = currentProc.PrivateMemorySize64;
                Debug.WriteLine("Memory used: " + memoryUsed);
            }
        }

        public void KinectManagerDidUpdateState(KinectManager aManager, bool aIsKinectActive)
        {
            Dispatcher.InvokeAsync((Action)delegate ()
            {
                if (aIsKinectActive)
                {
                    this.stateMessage.Opacity = 0.0f;
                }
                else
                {
                    //this.stateMessage.Opacity = 1.0f;
                }
            });
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void timerTick(object sender, EventArgs e)
        {
            if (timerCount == 0)
            {
                ProgressText.Foreground = Brushes.Green;
                ProgressText.Text = "Recording!";
                timer.Stop();
            }
            else
            {
                //update progress bar and countdown the timer
                timerCount--;
                setProgressPercentage(100 - 100 * ((double)timerCount / 30.0), (double)1.0 / 30.0);
                // RecordingProgress.Value+= 10;
            }
        }
        void initButtons()
        {
            allButtons = new List<Button> { HomeButton, EnrollButton, LoginGuessButton, LoginIAmButton, UserSlot1, UserSlot2,
            UserSlot3, UserSlot4, ReadyButton};
            welcomeButtons = new List<Button> { EnrollButton, LoginGuessButton, LoginIAmButton };
            selectUserButtons = new List<Button> { HomeButton, UserSlot1, UserSlot2, UserSlot3, UserSlot4 };
        }

        void setProgramState(ScreenStates ss)
        {
            //set current State
            currentState = ss;
            StickMen.Visibility = System.Windows.Visibility.Hidden;

            switch (ss)
            {
                case ScreenStates.HOME:
                    setScreenState(ScreenStates.HOME);
                    break;
                case ScreenStates.TUTORIAL:

                    setScreenState(ScreenStates.TUTORIAL);
                    break;
                case ScreenStates.SELECTUSER_ENROLL:
                    setScreenState(ScreenStates.SELECTUSER_ENROLL);
                    break;
                case ScreenStates.SELECTUSER_LOGIN:
                    setScreenState(ScreenStates.SELECTUSER_LOGIN);
                    break;
                case ScreenStates.RECORDING_ENROLL:
                    StickMen.Visibility = System.Windows.Visibility.Visible;
                    rb.clearBuffer();
                    setScreenState(ScreenStates.RECORDING_ENROLL);
                    startTimer(3);
                    break;
                case ScreenStates.RECORDING_LOGIN_GUESS:
                    StickMen.Visibility = System.Windows.Visibility.Visible;
                    rb.clearBuffer();
                    setScreenState(ScreenStates.RECORDING_LOGIN_GUESS);
                    startTimer(3);
                    break;
                case ScreenStates.RECORDING_LOGIN_GIVEN:
                    StickMen.Visibility = System.Windows.Visibility.Visible;
                    rb.clearBuffer();
                    setScreenState(ScreenStates.RECORDING_LOGIN_GIVEN);
                    startTimer(3);
                    break;
                case ScreenStates.RESULT:
                    setScreenState(ScreenStates.RESULT);
                    break;
                default:
                    //do nothing?! HOW DID IT GET HERE
                    setScreenState(ScreenStates.HOME);
                    break;
            }
        }

        void setScreenState(ScreenStates ss)
        {
            List<Button> VisibleButtons = null;

            //toggle all buttons + widgets to OFF
            RecordingProgress.Visibility = System.Windows.Visibility.Hidden;
            ProgressText.Visibility = System.Windows.Visibility.Hidden;
            TutorialText.Visibility = System.Windows.Visibility.Hidden;

            foreach (Button b in allButtons)
            {
                b.Visibility = System.Windows.Visibility.Hidden;
            }

            switch (ss)
            {
                case ScreenStates.HOME:
                    //have mechanism to stop recording or no.
                    HomeButton.Content = "Home/Back";
                    VisibleButtons = welcomeButtons;
                    break;
                case ScreenStates.TUTORIAL:
                    //VisibleButtons = welcomeButtons;
                    HomeButton.Visibility = System.Windows.Visibility.Visible;
                    //RecordingProgress.Visibility = System.Windows.Visibility.Visible;
                    TutorialText.Visibility = System.Windows.Visibility.Visible;
                    ReadyButton.Visibility = System.Windows.Visibility.Visible;
                    break;
                case ScreenStates.SELECTUSER_ENROLL:
                    TutorialText.Visibility = System.Windows.Visibility.Visible;
                    HomeButton.Visibility = System.Windows.Visibility.Visible;
                    HomeButton.Content = "Currently in Enroll: Home/Back";
                    VisibleButtons = selectUserButtons;
                    break;
                case ScreenStates.SELECTUSER_LOGIN:
                    HomeButton.Visibility = System.Windows.Visibility.Visible;
                    HomeButton.Content = "Currently in Login: Home/Back";
                    VisibleButtons = selectUserButtons;
                    break;
                case ScreenStates.RECORDING_ENROLL:
                    RecordingProgress.Visibility = System.Windows.Visibility.Visible;
                    ProgressText.Visibility = System.Windows.Visibility.Visible;
                    break;
                case ScreenStates.RECORDING_LOGIN_GUESS:
                    RecordingProgress.Visibility = System.Windows.Visibility.Visible;
                    ProgressText.Visibility = System.Windows.Visibility.Visible;
                    break;
                case ScreenStates.RECORDING_LOGIN_GIVEN:
                    RecordingProgress.Visibility = System.Windows.Visibility.Visible;
                    ProgressText.Visibility = System.Windows.Visibility.Visible;
                    break;
                case ScreenStates.RESULT:
                    HomeButton.Content = "Results/Press to Back";
                    HomeButton.Visibility = System.Windows.Visibility.Visible;
                    break;
                default:
                    //do nothing?! HOW DID IT GET HERE
                    VisibleButtons = welcomeButtons;
                    break;
            }

            if (VisibleButtons != null)
            {
                foreach (Button b in VisibleButtons)
                {
                    b.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {

        }


        void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            /*do nothing*/
            //pull frame from skeleton -- display it
            using (BodyFrame bf = e.FrameReference.AcquireFrame())
            {
                if (bf == null)
                    return; //do nothing if nothing found

                bf.GetAndRefreshBodyData(bStorage); //copy given skeleton(s) frames (if multiple skeletons found) into buffer

                //once the closest skeleton is determined draw THAT stickman


                //pointer to closest skeleton frame
                Body closestBody = null;

                // int closestId = -1; 
                double closestDistance = double.MaxValue;
                //out of detected skeletons in frame use the closest one -- determine closest one here
                foreach (var body in bStorage)
                {
                    //if valid skeleton
                    if (body.IsTracked)
                    {
                        if (body.Joints[JointType.SpineShoulder].Position.Z < closestDistance)
                        {
                            //update Id and distance
                            // closestId = 0;
                            closestBody = body; //set this skeleton as closest
                            closestDistance = body.Joints[JointType.SpineShoulder].Position.Z;
                        }
                    }
                }


                //draw the closest skeleton IF found
                if (closestBody != null)
                {
                    //draw next
                    //always clear cavnas
                    StickMen.Children.Clear();
                    DrawStickMan(closestBody, Brushes.Black, 3);
                }
                else
                {
                    return; //nothing found!
                }
                //DrawStickMan(Skeleton skeleton, Brush brush, int thickness)

                //if progress bar has filled
                if (timerCount == 0)
                {

                    //recording logic
                    if (currentState == ScreenStates.RECORDING_ENROLL || currentState == ScreenStates.RECORDING_LOGIN_GIVEN
                        || currentState == ScreenStates.RECORDING_LOGIN_GUESS)
                    {
                        //quickly allocate space -- rely on garbage collection huehuehue
                        float[] currJoints = new float[numJoints * 3];
                        for (int i = 0; i < jointorder.Length; i++)
                        {
                            currJoints[i * 3] = closestBody.Joints[(JointType)jointorder[i]].Position.X;
                            currJoints[i * 3 + 1] = closestBody.Joints[(JointType)jointorder[i]].Position.Y;
                            currJoints[i * 3 + 2] = closestBody.Joints[(JointType)jointorder[i]].Position.Z;
                        }
                        if (rb.getBufferStatus() == true) //recording complete AND/OR buffer-size was reached
                        {
                            if ((currentState == ScreenStates.RECORDING_ENROLL))
                            {
                                float[,] lookupData, queryDataExpanded, queryData;
                                queryDataExpanded = (rb.getBufferData());
                                queryData = new float[rb.getBufferNumberFrames(), numJoints * 3];
                                for (int i = 0; i < rb.getBufferNumberFrames(); i++)
                                {
                                    for (int j = 0; j < numJoints * 3; j++)
                                    {
                                        queryData[i, j] = queryDataExpanded[i, j];
                                    }
                                }
                                queryData = normalizeData(queryData);
                                dataStore[currentUser][0][recordNumber] = new MathNet.Numerics.LinearAlgebra.Single.DenseMatrix(queryData);

                                if ((recordNumber != numRecords - 1))
                                {

                                    recordNumber++;
                                    rb.clearBuffer();
                                    startTimer(3);
                                    setProgramState(ScreenStates.RECORDING_ENROLL);
                                }
                                else
                                {
                                    //store in g1, s1 slot
                                    dataEnrolled = true;

                                    dataStore[currentUser][0][recordNumber] = new MathNet.Numerics.LinearAlgebra.Single.DenseMatrix(queryData);

                                    userHasData[currentUser] = true;


                                    showPopup("Recorded " + rb.getBufferNumberFrames() + " frames for " + userNames[currentUser], Brushes.Blue);


                                    //Code for saving dataStore as .csv file
                                    //For each user

                                    Directory.CreateDirectory(desktopDir + "KinectSamples\\User" + (currentUser + 1).ToString());

                                    //For each Sample
                                    for (int sNum = 0; sNum < numRecords; sNum++)
                                    {
                                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@desktopDir + "KinectSamples\\User" + (currentUser + 1).ToString() + "\\Sample" + sNum.ToString() + ".csv", false))
                                        {
                                            for (int j = 0; j < dataStore[currentUser][0][sNum].RowCount; j++)
                                            {
                                                for (int i = 0; i < numJoints * 3 - 1; i++)
                                                {
                                                    file.Write(Convert.ToString(dataStore[currentUser][0][sNum][j, i]) + ",");
                                                }
                                                file.Write(Convert.ToString(dataStore[currentUser][0][sNum][j, numJoints * 3 - 1]) + "\n");
                                            }

                                        }
                                    }

                                    setProgramState(ScreenStates.HOME);
                                }
                            }
                            else
                            {
                                //bad "formatting"
                                float[,] lookupData, queryDataExpanded, queryData;
                                float bestScore = float.PositiveInfinity;
                                float dtwScore = float.PositiveInfinity;
                                float[][,] saveData = new float[5][,];
                                int userMatchIdx = -1;

                                //hide skeleton while processing
                                StickMen.Visibility = System.Windows.Visibility.Hidden;


                                queryDataExpanded = (rb.getBufferData());
                                queryData = new float[rb.getBufferNumberFrames(), numJoints * 3];
                                for (int i = 0; i < rb.getBufferNumberFrames(); i++)
                                {
                                    for (int j = 0; j < numJoints * 3; j++)
                                    {
                                        queryData[i, j] = queryDataExpanded[i, j];
                                    }
                                }
                                queryData = normalizeData(queryData);


                                //compute distances


                                switch (currentState)
                                {
                                    case ScreenStates.RECORDING_LOGIN_GIVEN:

                                        for (int i = 0; i < numRecords; i++)
                                        {
                                            lookupData = dataStore[currentUser][0][i].ToArray();
                                            dtwScore = gestureDistance(queryData, lookupData, queryData.GetLength(0), lookupData.GetLength(0));
                                            //TODO some analysis here ahahahahah
                                            ResultText.Text = "User: " + (int)(currentUser + 1) + "|Score: " + dtwScore;
                                            if (dtwScore <= acceptThreshold)
                                            {
                                                showPopup("Welcome back, " + userNames[currentUser], Brushes.Green);

                                                break;
                                            }
                                            else if (i == numRecords - 1)
                                            {
                                                showPopup("Sorry, I do not think you are " + userNames[currentUser], Brushes.Red);
                                            }
                                        }
                                        setProgramState(ScreenStates.HOME);
                                        //setProgramState(ScreenStates.RESULT);
                                        break;

                                    case ScreenStates.RECORDING_LOGIN_GUESS:
                                        for (int uIdx = 0; uIdx < userHasData.Length; uIdx++)
                                        {
                                            if (userHasData[uIdx] == false) continue;
                                            for (int i = 0; i < numRecords; i++)
                                            {
                                                lookupData = dataStore[uIdx][0][i].ToArray();
                                                dtwScore = gestureDistance(queryData, lookupData, queryData.GetLength(0), lookupData.GetLength(0));

                                                if (dtwScore < bestScore)
                                                {
                                                    bestScore = dtwScore;
                                                    userMatchIdx = uIdx;
                                                }
                                            }
                                        }
                                        //after ALL comparisons then do some result feeding here

                                        //TODO
                                        ResultText.Text = "User:" + (int)(userMatchIdx + 1) + "|Score:" + bestScore;

                                        if (bestScore <= acceptThreshold)
                                        {
                                            showPopup("Welcome back, " + userNames[userMatchIdx], Brushes.Green);
                                        }
                                        else
                                        {
                                            showPopup("Sorry, I do not think you are anyone I know.", Brushes.Red);
                                        }

                                        setProgramState(ScreenStates.HOME);
                                        //ResultText.Text = 
                                        //setProgramState(ScreenStates.RESULT);
                                        break;

                                    default:
                                        break;
                                }

                            }

                        }
                        else
                        {
                            rb.addFrame(currJoints, recordNumber);

                            StickMen.Children.Clear();
                            //update GUI to reflect recording or status here
                            if (rb.isMoving())
                            {
                                DrawStickMan(closestBody, Brushes.Green, 3);
                            }
                            else
                            {
                                DrawStickMan(closestBody, Brushes.Black, 3);
                            }

                        }
                    }
                }


            }
        }

        public float gestureDistance(float[,] query, float[,] template, int qlength, int tlength)
        {
            //TODO: normalization between gestures
            int minlength = (qlength < tlength) ? qlength : tlength;
            SimpleDTW simpleDTW = new SimpleDTW(query, template, qlength, tlength, numJoints);
            simpleDTW.computeDTW();
            return (float)simpleDTW.getSum() / (float)minlength;
        }

        //normalize Kinect skeleton data from center, and spinal length
        //no rotation/orientation normalization is done at the time
        public float[,] normalizeData(float[,] input)
        {
            int jointIdx;
            float spineLength;
            float[] jointCenter, neckJoint;
            float[,] normData = new float[input.GetLength(0), input.GetLength(1)];

            //assumption that data is of the form, time x joints
            //in theory "TODO", check that getlength(1) is the right size as some catch exception
            for (int t = 0; t < input.GetLength(0); t++)
            {
                for (int i = 0; i < numJoints; i++)
                {
                    jointIdx = 3 * i;
                    jointCenter = new float[3] { input[t, 30], input[t, 31], input[t, 32] };
                    neckJoint = new float[3] { input[t, 3], input[t, 4], input[t, 5] };
                    //spinelength defined from neck to skel bone? (dont remember)
                    spineLength = (float)Math.Sqrt(Math.Pow(jointCenter[0] - neckJoint[0], 2) +
                        Math.Pow(jointCenter[1] - neckJoint[1], 2) +
                        Math.Pow(jointCenter[2] - neckJoint[2], 2));
                    //hack to remove spine normalizatoin
                    //spineLength = 1;
                    //subtract from CENTROID (30->32, this is "Hard-coded" data)
                    //then normalize by spinelength
                    normData[t, jointIdx] = (input[t, jointIdx] - jointCenter[0]) / input[t, jointIdx + 2];// / spineLength;
                    normData[t, jointIdx + 1] = (input[t, jointIdx + 1] - jointCenter[1]) / input[t, jointIdx + 2]; // / spineLength;
                    normData[t, jointIdx + 2] = (input[t, jointIdx + 2] - jointCenter[2]) / input[t, jointIdx + 2]; // / spineLength;
                }
            }

            return normData;
        }


        /*
         *  Starts timer with given input in seconds
         */
        void setProgressPercentage(double percentage, double timespan)
        {
            //smooth filling
            TimeSpan duration = TimeSpan.FromSeconds(timespan);
            DoubleAnimation animation = new DoubleAnimation(percentage, duration);
            RecordingProgress.BeginAnimation(ProgressBar.ValueProperty, animation);
        }

        void startTimer(int seconds)
        {
            timerCount = seconds * (1000 / 100); //timerCount represent TickEvents that must trigger before the program stops
            //though updates every 100 ticks
            RecordingProgress.Value = 0; //set to Start
            //RecordingProgress.Maximum = (timerCount-1) * 10;
            ProgressText.Foreground = Brushes.Red;
            ProgressText.Text = "Progress til recording starts.";//: " + Math.Ceiling((double)timerCount*50/1000);
            timer.Start();
        }

        void enrollPushed(object sender, RoutedEventArgs e)
        {
            startTimer(3);
        }

        private void showPopup(string text, Brush backgroundColor)
        {
            ResultDisplay resultDisplay;
            resultDisplay = new ResultDisplay(text);
            resultDisplay.Background = backgroundColor;
            Grid.SetColumnSpan(resultDisplay, 3);
            Grid.SetRowSpan(resultDisplay, 3);
            this.kinectRegionGrid.Children.Add(resultDisplay);
        }

        //generic callback function for a button being pressed (ALL of them go here)
        private void ButtonPressedEvent(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(HomeButton))
            {
                //MessageBox.Show("Clicked Home!");
                setProgramState(ScreenStates.HOME);
                return;
            }
            else if (sender.Equals(EnrollButton))
            {
                //MessageBox.Show("Clicked Enroll!");
                setProgramState(ScreenStates.SELECTUSER_ENROLL);
                return;
            }
            else if (sender.Equals(ReadyButton))
            {
                //MessageBox.Show("Clicked ReadyButton");
                setProgramState(ScreenStates.SELECTUSER_ENROLL);
                return;
            }
            else if (sender.Equals(LoginGuessButton))
            {
                if (dataEnrolled != true)
                {
                    showPopup("Sorry, no users have been enrolled yet!", Brushes.Red);
                    setProgramState(ScreenStates.HOME);
                    return;
                    //no users enrolled
                }
                //MessageBox.Show("Clicked LoginGuess!");
                setProgramState(ScreenStates.RECORDING_LOGIN_GUESS);
                return;
            }
            else if (sender.Equals(LoginIAmButton))
            {
                if (dataEnrolled != true)
                {
                    showPopup("Sorry, no users have been enrolled yet!", Brushes.Red);
                    setProgramState(ScreenStates.HOME);
                    return;
                    //no users enrolled
                }
                //MessageBox.Show("Clicked LoginIAm!");
                setProgramState(ScreenStates.SELECTUSER_LOGIN);
                return;
            }
            else if (sender.Equals(UserSlot1))
            {
                currentUser = 0;
                if (currentState == ScreenStates.SELECTUSER_ENROLL)
                {
                    recordNumber = 0;

                    setProgramState(ScreenStates.RECORDING_ENROLL);
                }
                else //(currentState == ScreenStates.SELECTUSER_LOGIN)
                {
                    if (userHasData[currentUser] == false)
                    {
                        showPopup("Sorry, the profile for " + userNames[currentUser] + " has not been enrolled yet!", Brushes.Red);
                        setProgramState(ScreenStates.HOME);
                        return;
                    }
                    setProgramState(ScreenStates.RECORDING_LOGIN_GIVEN);
                }
                return;
            }
            else if (sender.Equals(UserSlot2))
            {

                currentUser = 1;
                if (currentState == ScreenStates.SELECTUSER_ENROLL)
                {
                    recordNumber = 0;

                    setProgramState(ScreenStates.RECORDING_ENROLL);
                }
                else //(currentState == ScreenStates.SELECTUSER_LOGIN)
                {
                    if (userHasData[currentUser] == false)
                    {
                        showPopup("Sorry, the profile for " + userNames[currentUser] + " has not been enrolled yet!", Brushes.Red);
                        setProgramState(ScreenStates.HOME);
                        return;
                    }
                    setProgramState(ScreenStates.RECORDING_LOGIN_GIVEN);
                }
                return;
            }
            else if (sender.Equals(UserSlot3))
            {

                currentUser = 2;
                if (currentState == ScreenStates.SELECTUSER_ENROLL)
                {
                    recordNumber = 0;

                    setProgramState(ScreenStates.RECORDING_ENROLL);
                }
                else //(currentState == ScreenStates.SELECTUSER_LOGIN)
                {
                    if (userHasData[currentUser] == false)
                    {
                        showPopup("Sorry, the profile for " + userNames[currentUser] + " has not been enrolled yet!", Brushes.Red);
                        setProgramState(ScreenStates.HOME);
                        return;
                    }
                    setProgramState(ScreenStates.RECORDING_LOGIN_GIVEN);
                }
                return;
            }
            else if (sender.Equals(UserSlot4))
            {

                currentUser = 3;

                if (currentState == ScreenStates.SELECTUSER_ENROLL)
                {
                    recordNumber = 0;

                    setProgramState(ScreenStates.RECORDING_ENROLL);
                }
                else //(currentState == ScreenStates.SELECTUSER_LOGIN)
                {
                    if (userHasData[currentUser] == false)
                    {
                        showPopup("Sorry, the profile for " + userNames[currentUser] + " has not been enrolled yet!", Brushes.Red);
                        setProgramState(ScreenStates.HOME);
                        return;
                    }
                    setProgramState(ScreenStates.RECORDING_LOGIN_GIVEN);
                }
                return;
            }

        }

        private void DrawStickMan(Body body, Brush brush, int thickness)
        {
            //Debug.Assert(skeleton.TrackingState == SkeletonTrackingState.Tracked, "The skeleton is being tracked.");
            MiniStick.Children.Clear();
            foreach (var run in SkeletonSegmentRuns)
            {
                var next1 = this.GetJointPoint(body, run[0], StickMen);
                var next2 = this.GetJointPoint(body, run[0], MiniStick);

                for (var i = 1; i < run.Length; i++)
                {
                    var prev1 = next1;
                    var prev2 = next2;
                    next1 = this.GetJointPoint(body, run[i], StickMen);
                    next2 = this.GetJointPoint(body, run[i], MiniStick);

                    var line = new Line
                    {
                        Stroke = brush,
                        StrokeThickness = thickness,
                        X1 = prev1.X,
                        Y1 = prev1.Y,
                        X2 = next1.X,
                        Y2 = next1.Y,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round
                    };

                    var line2 = new Line
                    {
                        Stroke = brush,
                        StrokeThickness = thickness / 2,
                        X1 = prev2.X,
                        Y1 = prev2.Y,
                        X2 = next2.X,
                        Y2 = next2.Y,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round
                    };
                    StickMen.Children.Add(line);
                    MiniStick.Children.Add(line2);
                }
            }
        }

        private Point GetJointPoint(Body body, JointType jointType, Canvas canvas)
        {
            var joint = body.Joints[jointType];

            // Points are centered on the StickMen canvas and scaled according to its height allowing
            // approximately +/- 1.5m from center line.
            var point = new Point
            {
                X = (canvas.Width / 2) + (canvas.Height * joint.Position.X / 3),
                Y = (canvas.Width / 2) - (canvas.Height * joint.Position.Y / 3)
            };

            return point;
        }

        private void LoadAClick(object sender, RoutedEventArgs e)
        {
            this.SetThreshold(HARD_THRESHOLD);
        }

        private void LoadBClick(object sender, RoutedEventArgs e)
        {
            this.SetThreshold(MEDIUM_THRESHOLD);
        }

        private void LoadCClick(object sender, RoutedEventArgs e)
        {
            this.SetThreshold(EASY_THRESHOLD);
        }

        private void ThreshSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.SetThreshold((float)e.NewValue);
        }

        private void SetThreshold(float value)
        {
            acceptThreshold = value;
            if (ThresholdBlock != null)
                ThresholdBlock.Text = "Threshold: " + acceptThreshold.ToString();
        }
    }
}
