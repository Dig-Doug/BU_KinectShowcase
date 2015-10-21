using KinectEx.Smoothing;
using KinectShowcaseCommon.Filters;
using KinectShowcaseCommon.UI_Elements;
using Microsoft.Kinect;
using Nito.KitchenSink;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KinectShowcaseCommon.Kinect_Processing
{
    public class KinectHandManager : KinectManager.SmoothBodyDataListener
    {
        #region HandStateChange

        public enum HandStateChangeType
        {
            BeganTracking,
            TrackingEnded,
            OpenToClose,
            CloseToOpen,
        }

        public class HandStateChangeEvent : EventArgs
        {
            public HandStateChangeType EventType { get; private set; }
            public Point HandPosition { get; private set; }

            public HandStateChangeEvent(HandStateChangeType aType, Point aLocation)
            {
                this.EventType = aType;
                this.HandPosition = aLocation;
            }
        }

        public interface HandStateChangeListener
        {
            bool KinectHandManagerDidDetectHandStateChange(KinectHandManager aManager, HandStateChangeEvent aEvent);
        }

        #endregion

        #region HandLocation

        public class HandLocationEvent
        {
            public Point HandPosition { get; private set; }

            public HandLocationEvent(Point aLocation)
            {
                this.HandPosition = aLocation;
            }
        }

        public interface HandLocationListener
        {
            bool KinectHandManagerDidGetHandLocation(KinectHandManager aManager, HandLocationEvent aEvent);
            bool HandShouldAttach();
            Point AttachLocation();
        }

        #endregion

        #region Properties

        private const float STABLE_HAND_POSTION_THRESHOLD = 0.05f;
        private const float INFERRED_Z_POSITION_CLAMP = 0.1f;
        //the size of the hand location filter
        public const int DEFAULT_HAND_LOCATION_BUFFER_SIZE = 3;
        //the size of the hand state buffer
        public const int DEFAULT_HAND_STATE_BUFFER_SIZE = 10;
        //the minimum number of closed states we need to see after an open to confirm that the hand is closed
        public const int DEFAULT_MINIMUM_CLOSED_STATES_AFTER_OPEN = 1;
        //the minimum number of open state we need to see after a close to confirm the hand is open
        public const int DEFAULT_MINIMUM_OPEN_STATES_AFTER_CLOSE = 7;
        public const int DEFAULT_QUANTIZATION_LEVELS = 100;

        private bool _trackingLeftHand = false;
        public bool TrackingLeftHand
        {
            get
            {
                return _trackingLeftHand;
            }
            set
            {
                _trackingLeftHand = value;
                if (this.Cursor != null)
                {
                    this.Cursor.RefreshHandedness(TrackingLeftHand);
                }
            }
        }
        public bool ShouldMoveCursor { get; set; }
        public bool ShouldSetCursorState { get; set; }
        public bool ShouldQuantize { get; set; }
        public int QuantizationLevels { get; set; }
        public bool ShouldFilterHandLocation { get; set; }
        public bool ShouldFilterHandState { get; set; }
        public int MinimumClosedStatesAfterOpen { get; set; }
        public int MinimumOpenStatesAfterClose { get; set; }
        public bool ShouldAttachToControls { get; set; }

        private PointFilter _pointFilter = new AveragePointFilter(3);
        public PointFilter PointFilter
        {
            get
            {
                return _pointFilter;
            }
            set
            {
                if (_pointFilter != value)
                {
                    _pointFilter = value;
                    if (value == null)
                    {
                        _pointFilter = new PointFilter();
                    }
                }
            }
        }

        private AverageValueFilter _shoulderLengthFilter = new AverageValueFilter(10);
        private AveragePointFilter _shoulderLocationFilter = new AveragePointFilter(10);

        private int _handLocationBufferSize;
        private int HandLocationBufferSize
        {
            get
            {
                return _handLocationBufferSize;
            }
            set
            {
                _handLocationBufferSize = value;
                _handPointsBuffer = new Point[_handLocationBufferSize];
                _handPointsBufferIndex = 0;
            }
        }

        private KinectManager _kinectManager;
        private CoordinateMapper _coordinateMapper;
        private WeakCollection<HandStateChangeListener> _handStateListeners = new WeakCollection<HandStateChangeListener>();
        private WeakCollection<HandLocationListener> _handLocationListeners = new WeakCollection<HandLocationListener>();
        private Point _currentHandLocation, _originHandPos = new Point(0.5f, 0.5f);
        private Point _lastStableHandPosition;
        private Point[] _handPointsBuffer;
        private int _handPointsBufferIndex = 0;
        private Rect _handTrackingRect;

        // Hand State
        public int HandStateBufferSize
        {
            get
            {
                return _handStateBufferSize;
            }
            set
            {
                _handStateBufferSize = value;
                _handStateBuffer = new HandState[_handStateBufferSize];
                _handStateBufferIndex = 0;
            }
        }
        private int _handStateBufferSize;
        private HandState[] _handStateBuffer;
        private int _handStateBufferIndex = 0;
        private int _handStateRunningCount = 0;
        private HandState _lastConfirmedHandState = HandState.Open;


        public KinectCursorView Cursor { get; set; }
        private float _depthFrameWidth, _depthFrameHeight;




        private Point _handRectCenter = new Point(0.5, -0.5);
        private Size _handRectSize = new Size(1.0, 1.0);

        #endregion

        #region Lifecycle

        public KinectHandManager(KinectManager aManager)
        {
            _kinectManager = aManager;
            _coordinateMapper = this._kinectManager.KinectSensor.CoordinateMapper;
            this.TrackingLeftHand = false;
            this.ShouldMoveCursor = true;
            this.ShouldSetCursorState = true;
            this.ShouldQuantize = false;
            this.QuantizationLevels = DEFAULT_QUANTIZATION_LEVELS;
            this.ShouldFilterHandLocation = true;
            this.ShouldFilterHandState = true;
            this.HandStateBufferSize = DEFAULT_HAND_STATE_BUFFER_SIZE;
            this.HandLocationBufferSize = DEFAULT_HAND_LOCATION_BUFFER_SIZE;
            this.MinimumClosedStatesAfterOpen = DEFAULT_MINIMUM_CLOSED_STATES_AFTER_OPEN;
            this.MinimumOpenStatesAfterClose = DEFAULT_MINIMUM_OPEN_STATES_AFTER_CLOSE;
            this.ShouldAttachToControls = true;

            FrameDescription frameDescription = _kinectManager.KinectSensor.DepthFrameSource.FrameDescription;
            _depthFrameWidth = frameDescription.Width;
            _depthFrameHeight = frameDescription.Height;

            this._kinectManager.AddSmoothBodyDataListener(this);
        }

        ~KinectHandManager()
        {
            if (this._kinectManager != null)
            {
                this._kinectManager.RemoveSmoothBodyDataListener(this);
            }
        }

        #endregion

        #region KinectManager.RawBodyDataListener Methods

        public void KinectManagerDidGetUpdatedBodyData(KinectManager aManager, SmoothedBody<ExponentialSmoother>[] aBodies)
        {
            SmoothedBody<ExponentialSmoother> tracked = aBodies.Where(b => b.TrackingId == this._kinectManager.CurrentlyTrackingId).FirstOrDefault();

            if (tracked != null && tracked.TrackingId != 0)
            {
                Dictionary<JointType, Point> jointPoints = this._kinectManager.HandManager.ConvertJointsToDepthSpace(tracked);


                double shoulderLengthScale;
                Point rectCenter;
                //only use the shoulder scale if it is actually tracked
                if (tracked.Joints[JointType.ShoulderLeft].TrackingState == TrackingState.Tracked && tracked.Joints[JointType.ShoulderRight].TrackingState == TrackingState.Tracked)
                {
                    double newLength = DistanceBetweenPoint(jointPoints[JointType.ShoulderLeft], jointPoints[JointType.ShoulderRight]) / 2;
                    shoulderLengthScale = _shoulderLengthFilter.Next(newLength);

                    rectCenter = _shoulderLocationFilter.Next(jointPoints[JointType.ShoulderRight]);
                }
                else
                {
                    shoulderLengthScale = _shoulderLengthFilter.Last;
                    rectCenter = _shoulderLocationFilter.Last;
                }

                rectCenter.X += _handRectCenter.X * shoulderLengthScale;
                rectCenter.Y += _handRectCenter.Y * shoulderLengthScale;

                Rect handRect = new Rect(rectCenter.X - _handRectSize.Width * shoulderLengthScale / 2, rectCenter.Y - _handRectSize.Height * shoulderLengthScale / 2,
                                    _handRectSize.Width * shoulderLengthScale, _handRectSize.Height * shoulderLengthScale);

                Point handLocation = jointPoints[JointType.HandRight];
                double scaledX = (handLocation.X - handRect.X) / handRect.Width;
                double scaledY = (handLocation.Y - handRect.Y) / handRect.Height;

                Point nextHandLoc = new Point(scaledX, scaledY);// _pointFilter.Next(new Point(scaledX, scaledY));

                Debug.WriteLine("X: " + nextHandLoc.X + " Y: " + nextHandLoc.Y);

                HandLocationEvent movedEvent = new HandLocationEvent(nextHandLoc);
                this.NotifyHandLocationListenersOfEvent(movedEvent);
            }


            /*

            if (tracked != null && tracked.TrackingId != 0)
            {
                Dictionary<JointType, Point> jointPoints = this._kinectManager.HandManager.ConvertJointsToDepthSpace(tracked);

                //check if we are already tracking a hand
                if (this._lastConfirmedHandState == HandState.NotTracked || !HandIsInCorrectPosition(jointPoints, this.TrackingLeftHand))
                {
                    if (this.ChooseHand(jointPoints))
                    {
                        HandStateChangeEvent beganTrackingEvent = new HandStateChangeEvent(HandStateChangeType.BeganTracking, this._currentHandLocation);
                        this.NotifyHandStateChangeListenersOfEvent(beganTrackingEvent);

                        _lastConfirmedHandState = HandState.Open;
                    }
                }

                MapCurrentHandPosition(jointPoints);
                if (!double.IsNaN(_currentHandLocation.X) && !double.IsNaN(_currentHandLocation.Y))
                {
                    HandLocationEvent movedEvent = new HandLocationEvent(_currentHandLocation);
                    this.NotifyHandLocationListenersOfEvent(movedEvent);
                }

                //Process Hand State
                ProcessHandState(tracked);
            }
            else
            {
                _lastConfirmedHandState = HandState.NotTracked;
                this._handStateBuffer[this._handStateBufferIndex] = _lastConfirmedHandState;
                this._handStateBufferIndex++;
                if (this._handStateBufferIndex >= _handStateBufferSize)
                    this._handStateBufferIndex = 0;
            }

            */
        }

        #endregion












        #region Hand Processing

        private bool ChooseHand(Dictionary<JointType, Point> aJointPoints)
        {
            //holds if we were successful in choosing a hand
            bool result = false;

            //see if the hands are good
            bool leftHandGood = this.HandIsInCorrectPosition(aJointPoints, true);
            bool rightHandGood = this.HandIsInCorrectPosition(aJointPoints, false);

            //prefer the right hand :P
            if (rightHandGood)
            {
                this.TrackingLeftHand = false;
                result = true;
            }
            else if (leftHandGood)
            {
                this.TrackingLeftHand = true;
                result = true;
            }

            return result;
        }

        private bool HandIsInCorrectPosition(Dictionary<JointType, Point> aJointPoints, bool aShouldTestLeftHand)
        {
            bool result = false;

            Rect handRect = KinectHandManager.CalculateHandRectForJointPointsInDepthSpace(aJointPoints, aShouldTestLeftHand);
            Point handPos = GetRawHandPosition(aJointPoints, aShouldTestLeftHand);
            //(aShouldTestLeftHand ? aJointPoints[JointType.WristLeft] : aJointPoints[JointType.WristRight]);//KinectHandManager.MapHandPositionInHandRectWithJointPointsInDepthSpace(aJointPoints, handRect, new Point(0.5, 0.5), aShouldTestLeftHand);

            //if (aShouldTestLeftHand)
            //Debug.WriteLine("X: " + handPos.X + " Y: " + handPos.Y);

            if (handRect.Contains(handPos))
            {
                result = true;
            }

            return result;
        }

        private void ProcessHandState(Body aTracked)
        {
            HandState trackBodyHandState = (TrackingLeftHand ? aTracked.HandLeftState : aTracked.HandRightState);

            //save the current state in the filter
            this._handStateBuffer[this._handStateBufferIndex] = trackBodyHandState;
            if (this._handStateBufferIndex > 0)
            {
                if (this._handStateBuffer[this._handStateBufferIndex] == this._handStateBuffer[this._handStateBufferIndex - 1])
                {
                    _handStateRunningCount++;
                }
                else
                {
                    _handStateRunningCount = 1;
                }
            }
            else
            {
                if (this._handStateBuffer[this._handStateBufferIndex] == this._handStateBuffer[_handStateBufferSize - 1])
                {
                    _handStateRunningCount++;
                }
                else
                {
                    _handStateRunningCount = 1;
                }
            }
            this._handStateBufferIndex++;
            if (this._handStateBufferIndex >= _handStateBufferSize)
                this._handStateBufferIndex = 0;

            //check if the hand just closed
            if (_lastConfirmedHandState == HandState.Open && trackBodyHandState == HandState.Closed)
            {
                if (this.ShouldFilterHandState)
                {
                    //see if the hand is been closed for long enough
                    if (this._handStateRunningCount >= this.MinimumClosedStatesAfterOpen)
                    {
                        //if so, send out event message
                        DidDetectHandStateChange(_lastConfirmedHandState, trackBodyHandState);
                        //set last confirmed state
                        _lastConfirmedHandState = HandState.Closed;
                    }
                }
                else
                {
                    //if so, send out event message
                    DidDetectHandStateChange(_lastConfirmedHandState, trackBodyHandState);
                    //set last confirmed state
                    _lastConfirmedHandState = HandState.Closed;
                }
            }
            //check if the hand is been open for long enough
            else if (_lastConfirmedHandState == HandState.Closed && trackBodyHandState == HandState.Open)
            {
                if (this.ShouldFilterHandState)
                {
                    //see if there is enough data for a hand open message
                    if (this._handStateRunningCount >= this.MinimumOpenStatesAfterClose)
                    {
                        //if so, send out event message
                        DidDetectHandStateChange(_lastConfirmedHandState, trackBodyHandState);
                        //set last confirmed state
                        _lastConfirmedHandState = HandState.Open;
                    }
                }
                else
                {
                    //if so, send out event message
                    DidDetectHandStateChange(_lastConfirmedHandState, trackBodyHandState);
                    //set last confirmed state
                    _lastConfirmedHandState = HandState.Open;
                }
            }
            else if (_lastConfirmedHandState == HandState.NotTracked)
            {

            }
        }

        private void MapCurrentHandPosition(Dictionary<JointType, Point> aJointPoints)
        {
            // convert the joint points to depth (display) space
            _handTrackingRect = CalculateHandRectForJointPointsInDepthSpace(aJointPoints, this.TrackingLeftHand);
            Point handLocation = MapHandPositionInHandRectWithJointPointsInDepthSpace(aJointPoints, _handTrackingRect, _originHandPos, this.TrackingLeftHand);

            //save the location in the filter
            this._handPointsBuffer[_handPointsBufferIndex] = handLocation;
            _handPointsBufferIndex++;
            if (_handPointsBufferIndex >= _handLocationBufferSize)
            {
                _handPointsBufferIndex = 0;
            }

            if (this.ShouldFilterHandLocation)
            {
                //--Average Filter--
                //take the average
                Point average = new Point();
                for (int i = 0; i < _handLocationBufferSize; i++)
                {
                    average.X += this._handPointsBuffer[i].X;
                    average.Y += this._handPointsBuffer[i].Y;
                }
                average.X /= _handLocationBufferSize;
                average.Y /= _handLocationBufferSize;
                _currentHandLocation = average;

                if (this.ShouldQuantize)
                {
                    double QUANTIZATION_STEP = 1.0f / this.QuantizationLevels;
                    int quantizedXCoord = (int)(_currentHandLocation.X / QUANTIZATION_STEP);
                    int quantizedYCoord = (int)(_currentHandLocation.Y / QUANTIZATION_STEP);
                    _currentHandLocation = new Point(quantizedXCoord * QUANTIZATION_STEP, quantizedYCoord * QUANTIZATION_STEP);
                }


                bool isStable = true;
                for (int i = 0; i < _handLocationBufferSize; i++)
                {
                    int filteredIndex = _handPointsBufferIndex - i;
                    if (filteredIndex < 0)
                    {
                        filteredIndex += _handLocationBufferSize;
                    }
                    Point oldHandLoc = _handPointsBuffer[filteredIndex];
                    if (DistanceBetweenPoint(handLocation, oldHandLoc) > STABLE_HAND_POSTION_THRESHOLD)
                    {
                        isStable = false;
                        break;
                    }
                }

                if (isStable)
                {
                    _lastStableHandPosition = handLocation;
                }
            }
            else
            {
                _currentHandLocation = handLocation;
                _lastStableHandPosition = handLocation;
            }

            //Debug.WriteLine("X: " + _currentHandLocation.X + " Y: " + _currentHandLocation.Y);

            //--Median Filter--
            //this is really inefficient, but it doesn't really matter
            //find the median
            /*
            double [] xCoords = new double[HAND_LOCATION_FILTER_SIZE];
            double [] yCoords = new double[HAND_LOCATION_FILTER_SIZE];
            for (int i = 0; i < HAND_LOCATION_FILTER_SIZE; i++)
            {
                xCoords[i] = this._handPoints[i].X;
                yCoords[i] = this._handPoints[i].Y;
            }

            double xMedian = GetMedian(xCoords);
            double yMedian = GetMedian(yCoords);
            _currentHandLocation = new Point(xMedian, yMedian);
             * */

            //--Recursive Filter--
            /*
            Point lastFilter = this._handPoints[0];
            if (lastFilter == null)
                lastFilter = new Point(0.5, 0.5);

            const double alpha = 0.05f;
            double xCoord = (1 - alpha) * handLocation.X + alpha * lastFilter.X;
            double yCoord = (1 - alpha) * handLocation.Y + alpha * lastFilter.Y;
            _currentHandLocation = new Point(xCoord, yCoord);
            this._handPoints[0] = _currentHandLocation;
             * */



        }

        public static double GetMedian(double[] sourceNumbers)
        {
            //Framework 2.0 version of this method. there is an easier way in F4        
            if (sourceNumbers == null || sourceNumbers.Length == 0)
                return 0D;

            //make sure the list is sorted, but use a new array
            double[] sortedPNumbers = (double[])sourceNumbers.Clone();
            sourceNumbers.CopyTo(sortedPNumbers, 0);
            Array.Sort(sortedPNumbers);

            //get the median
            int size = sortedPNumbers.Length;
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)sortedPNumbers[mid] : ((double)sortedPNumbers[mid] + (double)sortedPNumbers[mid - 1]) / 2;
            return median;
        }

        private void DidDetectHandStateChange(HandState aFromState, HandState aToState)
        {
            HandStateChangeEvent handEvent = null;
            Point handLocationAtChange = _currentHandLocation;

            if (this.ShouldFilterHandLocation && this.ShouldFilterHandState)
            {
                int posIndex = _handPointsBufferIndex;
                int stateIndex = _handStateBufferIndex;
                int count = 0;
                while (_handStateBuffer[stateIndex] != HandState.Open && count < _handStateBufferSize)
                {
                    posIndex--;
                    stateIndex--;
                    if (posIndex < 0)
                    {
                        posIndex = this._handLocationBufferSize - 1;
                    }
                    if (stateIndex < 0)
                    {
                        stateIndex = this._handStateBufferSize - 1;
                    }
                    count++;
                }

                if (count < _handStateBufferSize)
                {
                    handLocationAtChange = _handPointsBuffer[posIndex];
                }
            }

            if (_lastStableHandPosition != null)
                handLocationAtChange = _lastStableHandPosition;

            if (aToState == HandState.Open && aFromState != HandState.Open)
            {
                handEvent = new HandStateChangeEvent(HandStateChangeType.CloseToOpen, handLocationAtChange);
            }
            else if (aToState == HandState.Closed && aFromState != HandState.Closed)
            {
                handEvent = new HandStateChangeEvent(HandStateChangeType.OpenToClose, handLocationAtChange);
            }

            if (handEvent != null)
            {
                this.NotifyHandStateChangeListenersOfEvent(handEvent);
            }
        }

        #endregion

        #region Listener Handling

        public void AddHandStateChangeListener(HandStateChangeListener aListener)
        {
            if (!this._handStateListeners.Contains(aListener))
                this._handStateListeners.Add(aListener);
        }

        private void NotifyHandStateChangeListenersOfEvent(HandStateChangeEvent aEvent)
        {
            //notify the system listener of an interaction
            this._kinectManager.InteractionListener.SystemDidRecieveInteraction();

            foreach (HandStateChangeListener currentListener in this._handStateListeners)
            {
                bool result = currentListener.KinectHandManagerDidDetectHandStateChange(this, aEvent);
                //skip if the event was handled
                if (result)
                {
                    break;
                }
            }

            if (this.ShouldSetCursorState && this.Cursor != null)
            {
                KinectCursorView.CursorState state = (aEvent.EventType == HandStateChangeType.OpenToClose ? KinectCursorView.CursorState.ClosedHand : KinectCursorView.CursorState.OpenHand);
                this.Cursor.SetCursorState(state);
            }
        }

        public void RemoveHandStateChangeListener(HandStateChangeListener aListener)
        {
            if (this._handStateListeners.Contains(aListener))
                this._handStateListeners.Remove(aListener);
        }

        public void AddHandLocationListener(HandLocationListener aListener)
        {
            if (!this._handLocationListeners.Contains(aListener))
                this._handLocationListeners.Add(aListener);
        }

        private void NotifyHandLocationListenersOfEvent(HandLocationEvent aEvent)
        {
            //notify the system listener of an interaction
            this._kinectManager.InteractionListener.SystemDidRecieveInteraction();
            Point attachPoint = new Point();
            bool isAttaching = false;
            foreach (HandLocationListener currentListener in this._handLocationListeners)
            {
                bool result = currentListener.KinectHandManagerDidGetHandLocation(this, aEvent);
                if (this.ShouldAttachToControls && result)
                {
                    if (currentListener.HandShouldAttach())
                    {
                        attachPoint = currentListener.AttachLocation();
                        isAttaching = true;
                    }
                }
            }

            if (this.ShouldMoveCursor && this.Cursor != null)
            {
                Point handPos;
                if (isAttaching)
                {
                    handPos = attachPoint;
                }
                else
                {
                    handPos = aEvent.HandPosition;
                }
                this.Cursor.SetCursorPosition(handPos);
            }
        }

        public void RemoveHandLocationListener(HandLocationListener aListener)
        {
            if (this._handLocationListeners.Contains(aListener))
                this._handLocationListeners.Remove(aListener);
        }

        #endregion

        #region Helper Methods

        public Dictionary<JointType, Point> ConvertJointsToDepthSpace(SmoothedBody<ExponentialSmoother> aBody)
        {
            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

            foreach (JointType jointType in aBody.Joints.Keys)
            {
                // sometimes the depth(Z) of an inferred joint may show as negative
                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                CameraSpacePoint position = aBody.Joints[jointType].Position;
                if (position.Z < 0)
                {
                    position.Z = INFERRED_Z_POSITION_CLAMP;
                }

                DepthSpacePoint depthSpacePoint = this._coordinateMapper.MapCameraPointToDepthSpace(position);
                jointPoints[jointType] = new Point(depthSpacePoint.X / _depthFrameWidth, depthSpacePoint.Y / _depthFrameHeight);
            }

            return jointPoints;
        }

        public static Rect CalculateHandRectForJointPointsInDepthSpace(Dictionary<JointType, Point> aJointPoints, bool aShouldDoLeftHand)
        {
            Rect result;
            double shoulderLengthScale = DistanceBetweenPoint(aJointPoints[JointType.ShoulderLeft], aJointPoints[JointType.ShoulderRight]);
            if (aShouldDoLeftHand)
            {
                result = new Rect(aJointPoints[JointType.ShoulderLeft].X - (1.5 * shoulderLengthScale),// -(1.3 * shoulderLengthScale),
                                            aJointPoints[JointType.ShoulderLeft].Y - (0.5 * shoulderLengthScale),// -(1.0 * shoulderLengthScale),
                                            shoulderLengthScale * 1.25,//2 * shoulderLengthScale,
                                            shoulderLengthScale * 2);//2 * shoulderLengthScale);
            }
            else
            {
                //using right hand
                result = new Rect(aJointPoints[JointType.ShoulderRight].X + (0.25 * shoulderLengthScale), // - (.7 * shoulderLengthScale),
                                             aJointPoints[JointType.ShoulderRight].Y - (0.5 * shoulderLengthScale),// -(1.0 * shoulderLengthScale),
                                             shoulderLengthScale * 1.0,//2 * shoulderLengthScale,
                                             shoulderLengthScale * 1.5);//2 * shoulderLengthScale);
            }

            return result;
        }

        public static Point GetRawHandPosition(Dictionary<JointType, Point> aJointPoints, bool aShouldDoLeftHand)
        {
            Point result = new Point();
            const double divide = 2.0f;
            if (aShouldDoLeftHand)
            {
                result.X = aJointPoints[JointType.WristLeft].X + aJointPoints[JointType.HandLeft].X;// +aJointPoints[JointType.HandTipLeft].X;
                result.Y = aJointPoints[JointType.WristLeft].Y + aJointPoints[JointType.HandLeft].Y;// +aJointPoints[JointType.HandTipLeft].Y;
            }
            else
            {
                result.X = aJointPoints[JointType.WristRight].X + aJointPoints[JointType.HandRight].X;// +aJointPoints[JointType.HandTipRight].X;
                result.Y = aJointPoints[JointType.WristRight].Y + aJointPoints[JointType.HandRight].Y;// +aJointPoints[JointType.HandTipRight].Y;
            }
            result.X /= divide;
            result.Y /= divide;

            return result;
        }

        public static Point MapHandPositionInHandRectWithJointPointsInDepthSpace(Dictionary<JointType, Point> aJointPoints, Rect aHandRect, Point aOrigin, bool aShouldDoLeftHand)
        {
            Point result = new Point();

            double shoulderLengthScale = DistanceBetweenPoint(aJointPoints[JointType.ShoulderLeft], aJointPoints[JointType.ShoulderRight]);
            Point handPos = GetRawHandPosition(aJointPoints, aShouldDoLeftHand);
            if (aShouldDoLeftHand)
            {
                result.X = (float)((handPos.X - aHandRect.X) / (aHandRect.Width));
                result.Y = (float)((handPos.Y - aHandRect.Y) / (aHandRect.Height));
            }
            else
            {
                //using right hand
                result.X = (float)((handPos.X - aHandRect.X) / (aHandRect.Width));
                result.Y = (float)((handPos.Y - aHandRect.Y) / (aHandRect.Height));
            }
            result.X = result.X - aOrigin.X + 0.5f;
            result.Y = result.Y - aOrigin.Y + 0.5f;

            return result;
        }

        public static float DistanceBetweenPoint(Point aPointA, Point aPointB)
        {
            return (float)Math.Sqrt(Math.Pow(aPointB.X - aPointA.X, 2) + Math.Pow(aPointB.Y - aPointA.Y, 2));
        }

        #endregion

        #region Debug Methods

        public void InjectHandLocation(Point aLocation)
        {
            _currentHandLocation = aLocation;
            _currentHandLocation.X = _currentHandLocation.X - _originHandPos.X + 0.5f;
            _currentHandLocation.Y = _currentHandLocation.Y - _originHandPos.Y + 0.5f;
            _lastStableHandPosition = _currentHandLocation;
            HandLocationEvent movedEvent = new HandLocationEvent(_currentHandLocation);
            this.NotifyHandLocationListenersOfEvent(movedEvent);
        }

        public void InjectHandStateChange(HandState aState)
        {
            //_currentHandState = aState;
            DidDetectHandStateChange(_lastConfirmedHandState, aState);
            _lastConfirmedHandState = aState;
        }

        #endregion
    }
}
