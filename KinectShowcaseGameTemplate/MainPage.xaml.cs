using KinectShowcaseCommon.Kinect_Processing;
using KinectShowcaseCommon.ProcessHandling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KinectShowcaseGameTemplate
{
    public partial class MainPage : Page, KinectHandManager.HandLocationListener, KinectHandManager.HandStateChangeListener
    {
        #region Important Stuff - Don't mess with these or suffer the consequences!!!

        #region Properties

        // Handles kinect
        private KinectManager _kinectManager;
        // Handles calling the main game loop X times per second
        private DispatcherTimer _gameTimer;
        // The time in between studentWork() calls
        private const int GAME_LOOP_INTERVAL_MILLISECONDS = 33; //translates to about 30 fps
        // Location of player 1's hand
        private Point playerOneHandLocation = new Point();
        // Location of player 2's hand
        private Point playerTwoHandLocation = new Point();
        // Delays the game by a certain number of frames
        private int delayFrames = -1;
        // Holds the state of the grid
        private int[,] gridArray = new int[GAME_GRID_ROWS_COUNT, GAME_GRID_COLUMNS_COUNT];
        // The UI elements for the grid
        private Rectangle[,] rectArray;
        // Holds if the game has finished
        private bool isGameOver = false;
        // Holds if the game is currently waiting for the BOT to move
        private bool waitingOnBot = false;
        // Holds if player 1's hand is closed
        private bool isPlayerOneHandClosed = false, playerOneHandJustClosed = false;
        // Holds if player 2's hand is closed
        private bool isPlayerTwoHandClosed = false;

        private Point lastHoverTile;

        #endregion

        #region Game Init

        public MainPage()
        {
            InitializeComponent();

            //make the background full blur
            ((App.Current as App).MainWindow as MainWindow).SkeletonView.SetPercents(0.0f, 1.0f);
            ((App.Current as App).MainWindow as MainWindow).SkeletonView.SetMode(KinectShowcaseCommon.UI_Elements.LiveBackground.BackgroundMode.Infrared);

            _kinectManager = KinectManager.Default;
            _kinectManager.HandManager.AddHandLocationListener(this);
            _kinectManager.HandManager.AddHandStateChangeListener(this);

            this.InitGrid();
            this.InitTimer();
            this.StudentInit();

            this.byText.Text = AUTHOR;
        }

        private void InitGrid()
        {
            //dynamically add a grid      
            uniGrid.Rows = GAME_GRID_ROWS_COUNT;
            uniGrid.Columns = GAME_GRID_COLUMNS_COUNT;
            //rows left->right
            rectArray = new Rectangle[GAME_GRID_ROWS_COUNT, GAME_GRID_COLUMNS_COUNT];
            for (int rr = 0; rr < GAME_GRID_ROWS_COUNT; rr++)
            {
                for (int cc = 0; cc < GAME_GRID_COLUMNS_COUNT; cc++)
                {
                    rectArray[rr, cc] = new Rectangle();
                    rectArray[rr, cc].Stroke = this.gridCellStrokeColor;
                    rectArray[rr, cc].StrokeThickness = 3.0f;
                    rectArray[rr, cc].Fill = this.gridCellFillColor;
                    uniGrid.Children.Add(rectArray[rr, cc]);
                }
            }

            zeroGridArray();
        }

        private void InitTimer()
        {
            _gameTimer = new System.Windows.Threading.DispatcherTimer();
            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Interval = new TimeSpan(0, 0, 0, 0, GAME_LOOP_INTERVAL_MILLISECONDS);
            _gameTimer.Start();
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            // code goes here
            studentWork();
        }

        #endregion

        #region Kinect Hand Manager Callbacks

        public bool KinectHandManagerDidGetHandLocation(KinectHandManager aManager, KinectHandManager.HandLocationEvent aEvent)
        {
            bool result = false;

            Point pagePoint = new Point(aEvent.HandPosition.X * this.ActualWidth / _kinectManager.HandManager.HandCoordRangeX, aEvent.HandPosition.Y * this.ActualHeight / _kinectManager.HandManager.HandCoordRangeY);
            playerOneHandLocation = pagePoint;

            if (IsLocationInGrid(playerOneHandLocation))
            {
                hoverOverLocation(GetGridLocationForPoint(playerOneHandLocation));
            }
            else
            {
                hoverOverLocation(new Point(-1, -1));
            }

            return result;
        }

        public bool KinectHandManagerDidDetectHandStateChange(KinectHandManager aManager, KinectHandManager.HandStateChangeEvent aEvent)
        {
            bool result = false;

            if (aEvent.EventType == KinectHandManager.HandStateChangeType.CloseToOpen)
            {
                isPlayerOneHandClosed = false;
            }
            else if (aEvent.EventType == KinectHandManager.HandStateChangeType.OpenToClose)
            {
                isPlayerOneHandClosed = true;
                playerOneHandJustClosed = true;
            }

            return result;
        }

        public Point AttachLocation()
        {
            throw new NotImplementedException();
        }

        public bool HandShouldAttach()
        {
            return false;
        }

        #endregion

        #region Mouse Callbacks

        private void Page_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point clickLocation = e.GetPosition(this);
            playerOneHandLocation = clickLocation;
            isPlayerOneHandClosed = true;
            playerOneHandJustClosed = true;
        }

        private void Page_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isPlayerOneHandClosed = false;
        }

        #endregion

        #region UI Callbacks

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            _gameTimer.Stop();
            SystemCanary.Default.AskForKill();
        }

        #endregion

        #endregion

        #region Functions For Students to Use

        private bool IsLocationInGrid(Point aLocation)
        {
            bool result = false;

            //get the grid's rect
            Rect gridRect = uniGrid.RenderTransform.TransformBounds(new Rect(uniGrid.RenderSize));// LayoutInformation.GetLayoutSlot(uniGrid);
            if (gridRect.Contains(aLocation))
            {
                result = true;
            }

            return result;
        }

        private Point GetGridLocationForPoint(Point aLocation)
        {
            //get the grid's rect
            Rect gridRect = uniGrid.RenderTransform.TransformBounds(new Rect(uniGrid.RenderSize)); //LayoutInformation.GetLayoutSlot(uniGrid);

            //calculate the point
            Point result = new Point(aLocation.X, aLocation.Y);
            //translate to origin of grid's rect
            result.X -= gridRect.Left;
            result.Y -= gridRect.Top;
            //divide by column/row size
            result.X /= gridRect.Width / uniGrid.Columns;
            result.Y /= gridRect.Height / uniGrid.Rows;
            //floor
            result.X = (int)result.X;
            result.Y = (int)result.Y;

            return result;
        }

        void clearBoard()
        {
            for (int rr = 0; rr < GAME_GRID_ROWS_COUNT; rr++)
            {
                for (int cc = 0; cc < GAME_GRID_COLUMNS_COUNT; cc++)
                {
                    rectArray[rr, cc].Stroke = this.gridCellStrokeColor;
                    rectArray[rr, cc].Fill = this.gridCellFillColor;
                }
            }
        }

        bool GridCoordinatesWithingBounds(int aRow, int aCol)
        {
            bool result = false;
            if (aRow >= 0 && aRow < this.uniGrid.Rows && aCol >= 0 && aCol < this.uniGrid.Columns)
            {
                result = true;
            }
            return result;
        }

        void clearGridLocation(int row, int col)
        {
            if (this.GridCoordinatesWithingBounds(row, col))
            {
                rectArray[row, col].Stroke = this.gridCellStrokeColor;
                rectArray[row, col].Fill = this.gridCellFillColor;
            }
            else
            {
                Debug.WriteLine("MainPage - WARN - Tried to clear an invalid grid location X: " + row + " Y: " + col);
            }
        }

        void setText(string text, Brush brush)
        {
            textHere.Text = text;
            textHere.Foreground = brush;
        }

        private void hoverOverLocation(Point aLocation)
        {
            if (GridCoordinatesWithingBounds((int)lastHoverTile.X, (int)lastHoverTile.Y))
            {
                rectArray[(int)lastHoverTile.Y, (int)lastHoverTile.X].Stroke = gridCellStrokeColor;
            }

            lastHoverTile = aLocation;
            if (GridCoordinatesWithingBounds((int)aLocation.X, (int)aLocation.Y))
            {
                rectArray[(int)lastHoverTile.Y, (int)lastHoverTile.X].Stroke = gridCellHoverColor;
            }

            InvalidateVisual();
        }

        void highlightGridLocationWithColor(int row, int col, Brush brush)
        {
            if (this.GridCoordinatesWithingBounds(row, col))
            {
                rectArray[row, col].Fill = brush;
            }
            else
            {
                Debug.WriteLine("MainPage - WARN - Tried to color an invalid grid location X: " + row + " Y: " + col);
            }
        }

        void highlightGridLocation(int row, int col, int playerNumber)
        {
            if (this.GridCoordinatesWithingBounds(row, col))
            {
                if (playerNumber == 1)
                    rectArray[row, col].Fill = playerOneBrush;
                if (playerNumber == 2)
                    rectArray[row, col].Fill = playerTwoBrush;
                InvalidateVisual();
            }
            else
            {
                Debug.WriteLine("MainPage - WARN - Tried to color an invalid grid location X: " + row + " Y: " + col + " for player: " + playerNumber);
            }
        }

        private void updateGridColors()
        {
            for (int i = 0; i < GAME_GRID_ROWS_COUNT; i++)
            {
                for (int j = 0; j < GAME_GRID_COLUMNS_COUNT; j++)
                {
                    int playerNumber = gridArray[i, j];
                    if (playerNumber == 1)
                        rectArray[i, j].Fill = playerOneBrush;
                    if (playerNumber == 2)
                        rectArray[i, j].Fill = playerTwoBrush;
                }
            }
            InvalidateVisual();
        }

        void setPlayerColor(int playerNumber, Brush brush)
        {
            if (playerNumber == 1)
                playerOneBrush = brush;
            if (playerNumber == 2)
                playerTwoBrush = brush;
        }

        bool isPlayerHandClosed(int playerNumber)
        {
            if (playerNumber == 1 && playerOneHandJustClosed)
            {
                playerOneHandJustClosed = false;
                return isPlayerOneHandClosed;
            }
            if (playerNumber == 2)
                return isPlayerTwoHandClosed;
            return false;
        }

        //sets Grid Array to zero
        void zeroGridArray()
        {
            for (int rr = 0; rr < GAME_GRID_ROWS_COUNT; rr++)
            {
                for (int cc = 0; cc < GAME_GRID_COLUMNS_COUNT; cc++)
                {
                    gridArray[rr, cc] = 0;
                }
            }
        }

        void setInstructionText(string text, Brush brush, double fontSize)
        {
            instructionText.Text = text;
            instructionText.Foreground = brush;
            instructionText.FontSize = fontSize;
        }


        void setButtonText(string text, int buttNom)
        {
            switch (buttNom)
            {
                case 1:
                    Button1.Content = text;
                    break;
            }
        }

        private void DisplayButton(int aButtonNumber, bool aShouldDisplay)
        {
            switch (aButtonNumber)
            {
                case 1:
                    {
                        Button1.IsEnabled = aShouldDisplay;
                        Button1.Opacity = (aShouldDisplay ? 1.0 : 0.0f);
                        break;
                    }
            }
        }

        //returns 0 no winner, returns 1 or 2 if that player has won respectively
        int checkWinner()
        {
            //assuming gridArray is 3 x 3

            //check diagonals
            if (gridArray[0, 0] != 0 && gridArray[0, 0] == gridArray[1, 1] && gridArray[1, 1] == gridArray[2, 2])
            {
                return gridArray[0, 0];
            }

            if (gridArray[2, 0] != 0 && gridArray[2, 0] == gridArray[1, 1] && gridArray[1, 1] == gridArray[0, 2])
            {
                return gridArray[2, 0];
            }

            //check rows
            if (gridArray[0, 0] != 0 && gridArray[0, 0] == gridArray[0, 1] && gridArray[0, 1] == gridArray[0, 2])
            {
                return gridArray[0, 0];
            }

            if (gridArray[1, 0] != 0 && gridArray[1, 0] == gridArray[1, 1] && gridArray[1, 1] == gridArray[1, 2])
            {
                return gridArray[1, 0];
            }

            if (gridArray[2, 0] != 0 && gridArray[2, 0] == gridArray[2, 1] && gridArray[2, 1] == gridArray[2, 2])
            {
                return gridArray[2, 0];
            }

            //check columns
            //check rows
            if (gridArray[0, 0] != 0 && gridArray[0, 0] == gridArray[1, 0] && gridArray[1, 0] == gridArray[2, 0])
            {
                return gridArray[0, 0];
            }

            if (gridArray[0, 1] != 0 && gridArray[0, 1] == gridArray[1, 1] && gridArray[1, 1] == gridArray[2, 1])
            {
                return gridArray[0, 1];
            }

            if (gridArray[0, 2] != 0 && gridArray[0, 2] == gridArray[1, 2] && gridArray[1, 2] == gridArray[2, 2])
            {
                return gridArray[0, 2];
            }

            return 0;
        }

        #endregion

        #region Properties For Students To Customize

        // Title of the game
        private const string GAME_TITLE = "Tic Tac Toe";

        // Authors of Game
        private const string AUTHOR = "";

        // Number of rows in the game grid
        private const int GAME_GRID_ROWS_COUNT = 3;
        // Number of columns in the game grid
        private const int GAME_GRID_COLUMNS_COUNT = 3;

        // Color to outline the cells of the grid with
        private Brush gridCellStrokeColor = Brushes.Gray;
        private Brush gridCellHoverColor = Brushes.Red;
        // Color to fill the empty cells of the grid with
        private Brush gridCellFillColor = Brushes.White;

        // Color to fill player 1's cells
        private Brush playerOneBrush = Brushes.Green;
        // Color to fill player 2's cells
        private Brush playerTwoBrush = Brushes.Red;

        #endregion

        #region Functions for Student Customization

        // This is called when the game first starts. By this time the grid has already
        // been initialized to the size set in the above properties (GAME_GRID_ROWS_COUNT, etc..)
        // Some things you could do in this function are:
        //      - Set the text of buttons
        //      - Set the colors of the grid
        private void StudentInit()
        {
            // If you want to write something to the console
            // (the text output on your computer), you can do it like this:
            Debug.WriteLine("StudentInit - Initializing student stuff!");

            // Set the title of the game
            setText(GAME_TITLE, Brushes.White);

            // Here's how you can set instruction text to be displayed on screen
            // Newline => \r\n (Windows Format)
            setInstructionText("How to play: \r\n " +
                               "1. Connect 3 in a row \r\n" +
                               "2. Beat the Bot \r\n" +
                               "3. Game repeats! \r\n ", Brushes.White, 48);

            // Here's how you can set the text of a button
            setButtonText("New Game", 1);
        }

        // This is the main game loop, which means that the code in this function is called
        // many times per second. This is where you control the game, handle player moves,
        // tell the computer to move, and reset the game.
        private void studentWork()
        {
            //We use the gridArray as follows: 0 means the spot is BLANK, 1 means OCCUPIED by player 1, 2 means OCCUPIED by player 2
            //for the purposes of this bot (Computer AI), the user is always player 1

            //delayFrames can be set to a certain number in order to "stall" the program
            //REMEMBER: Camera operates at 30 frames per second. That means, if delayFrames == 90 then the program waits 3 seconds until it moves to the "else if" statement
            if (delayFrames > 0)
            {
                //Post-decrement delayFrames
                delayFrames--;
            }
            else if (delayFrames == 0)
            {
                // delay is over
                // Put what you want to execute AFTER the delay WITHIN this section

                //Post - decrement delayFrames
                delayFrames--;

                //Checks if game is finished (ie. 3 in a row)
                if (!isGameOver)
                {
                    //Function that has the Bot make a move
                    //You can edit the botMove function to make it more smarterer
                    botMove();
                    //Bot has moved, it's the players' turn
                    waitingOnBot = false;

                    //checkWinner() returns 1 or 2 if there is a winner or 0 if there is no winner. 
                    if (checkWinner() == 2) //Winner is player 2
                    {
                        setText("BOT Wins! (Close Hand to Reset)", Brushes.Red); //Changes the text on the screen
                        isGameOver = true; //Changes this global variable so that the game ends
                    }
                }
            }
            else
            {
                //see if the hand is closed
                if (isPlayerHandClosed(1))
                {
                    // GAME RESET LOGIC

                    //Check if the game is over
                    if (isGameOver)
                    {
                        resetGame();
                        setText(GAME_TITLE, Brushes.White);
                    }
                    else
                    {
                        //Check if the player is allowed to move (it's not the bot's turn)
                        if (!waitingOnBot)
                        {
                            //Check if there was a move somewhere (hand hovers over button AND hand is closed)
                            if (this.IsLocationInGrid(playerOneHandLocation))
                            {
                                //get the location of the hand in the grid
                                Point gridHandLoc = this.GetGridLocationForPoint(playerOneHandLocation);
                                int row = (int)gridHandLoc.Y;
                                int col = (int)gridHandLoc.X;

                                //check if the grid is available
                                if (gridArray[row, col] == 0)
                                {
                                    //process PlayerOne move            
                                    gridArray[row, col] = 1; //Internal grid (or "Board) is set to 1 at player hand location
                                    //Highlight the grid location on screen
                                    highlightGridLocation(row, col, 1);

                                    //Check if we have a winnner
                                    if (checkWinner() == 1)
                                    {
                                        setText("P1 Wins! (Close Hand to Reset)", Brushes.Green);
                                        isGameOver = true;
                                        delayFrames = 30;
                                    }
                                    else
                                    {
                                        //Sets delay to 15 which means the Bot will not make a move until 15 frames or 0.5 seconds
                                        delayFrames = 15;
                                        waitingOnBot = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        private const int PLAYER_VALUE = 1;
        private const int BOT_VALUE = 2;

        // Call this function when you want the computer to make a move in the game
        private void botMove()
        {
            //Tic-Tac-Toe strategy (From Wikipedia)
            //1. Win: If the player has two in a row, they can place a third to get three in a row.
            //2. Block: If the opponent has two in a row, the player must play the third themselves to block the opponent.
            //3. Fork: Create an opportunity where the player has two threats to win (two non-blocked lines of 2).
            //4. Blocking an opponent's fork:
            //    Option 1: The player should create two in a row to force the opponent into defending, 
            //          as long as it doesn't result in them creating a fork. For example, if "X" has a /corner, 
            //          "O" has the center, and "X" has the opposite corner as well, "O" must not play a corner 
            //          in order to win. (Playing a corner in this scenario creates a fork for "X" to win.)
            //   Option 2: If there is a configuration where the opponent can fork, the player should block that fork.
            //5. Center: A player marks the center. (If it is the first move of the game, playing on a corner gives "O" 
            //      more opportunities to make a mistake and may therefore be the better choice; however, 
            //      it makes no difference between perfect players.)
            //6. Opposite corner: If the opponent is in the corner, the player plays the opposite corner.
            //7. Empty corner: The player plays in a corner square.
            //8. Empty side: The player plays in a middle square on any of the 4 sides.

            bool moved = false;

            //eval board
            int[] rowBotCount = new int[3], rowPlayerCount = new int[3];
            int[] colBotCount = new int[3], colPlayerCount = new int[3];
            int[] rowEmpty = { -1, -1, -1 }, colEmpty = { -1, -1, -1 }, diagEmpty = { -1, -1 };
            int[] diagBotCount = new int[2], diagPlayerCount = new int[2];
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int value = gridArray[row, col];
                    rowBotCount[row] += (value == BOT_VALUE ? 1 : 0);
                    colBotCount[col] += (value == BOT_VALUE ? 1 : 0);
                    rowPlayerCount[row] += (value == PLAYER_VALUE ? 1 : 0);
                    colPlayerCount[col] += (value == PLAYER_VALUE ? 1 : 0);
                    rowEmpty[row] = (value == 0 ? col : (rowEmpty[row] != -1 ? rowEmpty[row] : -1));
                    colEmpty[col] = (value == 0 ? row : (colEmpty[col] != -1 ? colEmpty[col] : -1));

                    if (row == col)
                    {
                        diagBotCount[0] += (value == BOT_VALUE ? 1 : 0);
                        diagPlayerCount[0] += (value == PLAYER_VALUE ? 1 : 0);
                        diagEmpty[0] = (value == 0 ? col : (diagEmpty[0] != -1 ? diagEmpty[0] : -1));
                    }
                    if (row == 2 - col)
                    {
                        diagBotCount[1] += (value == BOT_VALUE ? 1 : 0);
                        diagPlayerCount[1] += (value == PLAYER_VALUE ? 1 : 0);
                        diagEmpty[1] = (value == 0 ? col : (diagEmpty[1] != -1 ? diagEmpty[1] : -1));
                    }
                }
            }

            //1. Win
            //check for 2 in a row
            for (int i = 0; i < 3; i++)
            {
                // Check for 2 in a row
                if (rowBotCount[i] == 2 && rowEmpty[i] != -1)
                {
                    //move here
                    gridArray[i, rowEmpty[i]] = BOT_VALUE;
                    moved = true;
                }
                else if (colBotCount[i] == 2 && colEmpty[i] != -1)
                {
                    //move here
                    gridArray[colEmpty[i], i] = BOT_VALUE;
                    moved = true;
                }
                if (moved)
                    break;
            }
            //1. Win cont.
            if (!moved)
            {
                //check diag
                if (diagBotCount[0] == 2 && diagEmpty[0] != -1)
                {
                    //move here
                    gridArray[diagEmpty[0], diagEmpty[0]] = BOT_VALUE;
                    moved = true;
                }
                else if (diagBotCount[1] == 2 && diagEmpty[1] != -1)
                {
                    //move here
                    gridArray[2 - diagEmpty[1], diagEmpty[1]] = BOT_VALUE;
                    moved = true;
                }
            }


            //2. Block
            if (!moved)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (rowPlayerCount[i] == 2 && rowEmpty[i] != -1)
                    {
                        //move here
                        gridArray[i, rowEmpty[i]] = BOT_VALUE;
                        moved = true;
                    }
                    else if (colPlayerCount[i] == 2 && colEmpty[i] != -1)
                    {
                        //move here
                        gridArray[colEmpty[i], i] = BOT_VALUE;
                        moved = true;
                    }
                    if (moved)
                        break;
                }

                if (!moved)
                {
                    //check diag
                    if (diagPlayerCount[0] == 2 && diagEmpty[0] != -1)
                    {
                        //move here
                        gridArray[diagEmpty[0], diagEmpty[0]] = BOT_VALUE;
                        moved = true;
                    }
                    else if (diagPlayerCount[1] == 2 && diagEmpty[1] != -1)
                    {
                        //move here
                        gridArray[2 - diagEmpty[1], diagEmpty[1]] = BOT_VALUE;
                        moved = true;
                    }
                }
            }

            if (!moved)
            {
                //3. fork
                //4. block fork
            }

            //5. play center
            if (!moved && gridArray[1, 1] == 0)
            {
                gridArray[1, 1] = BOT_VALUE;
                moved = true;
            }

            //6. play opposite corner
            if (!moved)
            {
                if (diagPlayerCount[0] == 1 && diagEmpty[0] != -1)
                {
                    //move here
                    gridArray[diagEmpty[0], diagEmpty[0]] = BOT_VALUE;
                    moved = true;
                }
                else if (diagPlayerCount[1] == 1 && diagEmpty[1] != -1)
                {
                    //move here
                    gridArray[2 - diagEmpty[1], diagEmpty[1]] = BOT_VALUE;
                    moved = true;
                }
            }

            //7. play any corner
            if (!moved)
            {
                if (diagEmpty[0] != -1)
                {
                    //move here
                    gridArray[diagEmpty[0], diagEmpty[0]] = BOT_VALUE;
                    moved = true;
                }
                else if (diagEmpty[1] != -1)
                {
                    //move here
                    gridArray[2 - diagEmpty[1], diagEmpty[1]] = BOT_VALUE;
                    moved = true;
                }
            }

            //8. play empty side
            if (!moved)
            {
                if (gridArray[0, 1] == 0)
                {
                    gridArray[0, 1] = BOT_VALUE;
                    moved = true;
                }
                else if (gridArray[2, 1] == 0)
                {
                    gridArray[2, 1] = BOT_VALUE;
                    moved = true;
                }
                else if (gridArray[1, 0] == 0)
                {
                    gridArray[1, 0] = BOT_VALUE;
                    moved = true;
                }
                else if (gridArray[1, 2] == 0)
                {
                    gridArray[1, 2] = BOT_VALUE;
                    moved = true;
                }
            }

            //9. tie
            if (!moved)
            {
                //if a bot has reached here the game is a TIE
                isGameOver = true;
                setText("TIE Game! (Close Hand to Reset)", Brushes.White);
            }

            updateGridColors();
        }

        // Call this function when you want to reset the game
        private void resetGame()
        {
            //reset game
            waitingOnBot = false;
            isGameOver = false;
            clearBoard();
            zeroGridArray();
        }


        // These functions are called when the reset button is clicked by the user.
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Since we use the title to display when someone wins the game
            // we need to reset the title of the game
            setText(GAME_TITLE, Brushes.White);

            //This is the reset button, so reset the game
            resetGame();
        }

        #endregion
    }
}
