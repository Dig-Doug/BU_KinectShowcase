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
        //the minimum number of closed states we need to see after an open to confirm that the hand is closed
        public const int DEFAULT_MINIMUM_CLOSED_STATES_AFTER_OPEN = 1;
        //the minimum number of open state we need to see after a close to confirm the hand is open
        public const int DEFAULT_MINIMUM_OPEN_STATES_AFTER_CLOSE = 7;

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
        public int MinimumClosedStatesAfterOpen { get; set; }
        public int MinimumOpenStatesAfterClose { get; set; }
        public bool ShouldAttachToControls { get; set; }
        public KinectCursorView Cursor { get; set; }
        public Rect HandRect { get; private set; }

        private KinectManager _kinectManager;
        private CoordinateMapper _coordinateMapper;
        private WeakCollection<HandStateChangeListener> _handStateListeners = new WeakCollection<HandStateChangeListener>();
        private WeakCollection<HandLocationListener> _handLocationListeners = new WeakCollection<HandLocationListener>();
        //private Point _normalizedHandLocation;
        //private Point _rawScaledHandLocation, _filteredScaledHandLocation;
        private PointFilter _scaledHandLocationFilter = new PointFilter(new RegressionValueFilter(0.1), new ValueFilter());
        private HandState _lastConfirmedHandState = HandState.Open;
        private float _depthFrameWidth, _depthFrameHeight;
        private HandStateCounter _handStateCounter = new HandStateCounter();

        private const float ASPECT_RATIO = 1920 / 1080.0f;
        private Point _handRectCenter = new Point(1.0, -1.0/ ASPECT_RATIO);
        private Size _handRectSize = new Size(2.0, 2.0 / ASPECT_RATIO);


        public float HandCoordRangeX { get; private set; }
        public float HandCoordRangeY { get; private set; }

        #endregion

        #region Lifecycle

        public KinectHandManager(KinectManager aManager)
        {
            _kinectManager = aManager;
            _coordinateMapper = this._kinectManager.KinectSensor.CoordinateMapper;
            this.TrackingLeftHand = false;
            this.MinimumClosedStatesAfterOpen = DEFAULT_MINIMUM_CLOSED_STATES_AFTER_OPEN;
            this.MinimumOpenStatesAfterClose = DEFAULT_MINIMUM_OPEN_STATES_AFTER_CLOSE;
            this.ShouldAttachToControls = true;

            FrameDescription frameDescription = _kinectManager.KinectSensor.DepthFrameSource.FrameDescription;
            _depthFrameWidth = frameDescription.Width;
            _depthFrameHeight = frameDescription.Height;

            this.HandCoordRangeX = 1920;
            this.HandCoordRangeY = 1080;

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

        public void KinectManagerDidGetUpdatedBodyData(KinectManager aManager, SmoothedBody<KalmanSmoother>[] aBodies)
        {
            SmoothedBody<KalmanSmoother> tracked = aBodies.Where(b => b.TrackingId == this._kinectManager.CurrentlyTrackingId).FirstOrDefault();

            if (tracked != null && tracked.TrackingId != 0)
            {
                Dictionary<JointType, Point> jointPoints = this._kinectManager.HandManager.ConvertJointsToDepthSpace(tracked);

                //check if we are already tracking a hand
                bool beganTracking = false;
                if (this._lastConfirmedHandState == HandState.NotTracked || !HandIsInCorrectPosition(jointPoints, this.TrackingLeftHand))
                {
                    if (this.ChooseHand(jointPoints))
                    {
                        beganTracking = true;
                        _lastConfirmedHandState = HandState.Open;
                    }
                }

                //Process hand loc
                Point currentHandLoc = ProcessHandLocation(jointPoints);

                //if began tracking, send out event
                if (beganTracking)
                {
                    HandStateChangeEvent beganTrackingEvent = new HandStateChangeEvent(HandStateChangeType.BeganTracking, currentHandLoc);
                    this.NotifyHandStateChangeListenersOfEvent(beganTrackingEvent);
                }

                //Process Hand State
                ProcessHandState(tracked, currentHandLoc);
            }
            else
            {
                _lastConfirmedHandState = HandState.NotTracked;
                _handStateCounter.Reset();
            }
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

            Rect handRect = CalculateHandRect(aJointPoints, aShouldTestLeftHand);
            Point handPos = GetRawHandPosition(aJointPoints, aShouldTestLeftHand);

            //if (aShouldTestLeftHand)
            //Debug.WriteLine("X: " + handPos.X + " Y: " + handPos.Y);

            if (handRect.Contains(handPos))
            {
                result = true;
            }

            return result;
        }

        private Point NormalizeHandPosition(Dictionary<JointType, Point> aJointPoints, bool aShouldDoLeftHand)
        {
            //get the raw hand pos
            Point handPos = GetRawHandPosition(aJointPoints, aShouldDoLeftHand);
            //get the hand rect
            Rect handRect = CalculateHandRect(aJointPoints, aShouldDoLeftHand);
            //map the hand coord to the rect
            double scaledX = (handPos.X - handRect.X) / handRect.Width;
            double scaledY = (handPos.Y - handRect.Y) / handRect.Height;
            Point result = new Point(scaledX, scaledY);

            //set hand rect
            this.HandRect = handRect;

            return result;
        }

        private Point ProcessHandLocation(Dictionary<JointType, Point> aJoints)
        {
            //get normalized hand
            Point normalizedHandLocation = NormalizeHandPosition(aJoints, this.TrackingLeftHand);

            //scaled up hand location
            Point scaledHandLocation = new Point(normalizedHandLocation.X * this.HandCoordRangeX, normalizedHandLocation.Y * this.HandCoordRangeY);

            //filter
            Point filteredScaledHandLocation = _scaledHandLocationFilter.Next(scaledHandLocation);

            //check for valid location
            if (!double.IsNaN(filteredScaledHandLocation.X) && !double.IsNaN(filteredScaledHandLocation.Y))
            {
                //send event
                HandLocationEvent movedEvent = new HandLocationEvent(filteredScaledHandLocation);
                this.NotifyHandLocationListenersOfEvent(movedEvent);
            }

            return filteredScaledHandLocation;
        }

        private void ProcessHandState(SmoothedBody<KalmanSmoother> aTracked, Point aHandLocation)
        {
            HandState trackBodyHandState = (TrackingLeftHand ? aTracked.HandLeftState : aTracked.HandRightState);

            //save the current state in the counter
            _handStateCounter.Add(trackBodyHandState);

            //check if the hand just closed
            if (_lastConfirmedHandState == HandState.Open && trackBodyHandState == HandState.Closed)
            {
                //see if the hand is been closed for long enough
                if (_handStateCounter.Count >= this.MinimumClosedStatesAfterOpen)
                {
                    //if so, send out event message
                    DidDetectHandStateChange(_lastConfirmedHandState, trackBodyHandState, aHandLocation);
                    //set last confirmed state
                    _lastConfirmedHandState = HandState.Closed;
                }
            }
            //check if the hand is been open for long enough
            else if (_lastConfirmedHandState == HandState.Closed && trackBodyHandState == HandState.Open)
            {
                //see if there is enough data for a hand open message
                if (_handStateCounter.Count >= this.MinimumOpenStatesAfterClose)
                {
                    //if so, send out event message
                    DidDetectHandStateChange(_lastConfirmedHandState, trackBodyHandState, aHandLocation);
                    //set last confirmed state
                    _lastConfirmedHandState = HandState.Open;
                }
            }
            else if (_lastConfirmedHandState == HandState.NotTracked)
            {

            }
        }

        private void DidDetectHandStateChange(HandState aFromState, HandState aToState, Point aHandLocation)
        {
            HandStateChangeEvent handEvent = null;

            if (aToState == HandState.Open && aFromState != HandState.Open)
            {
                handEvent = new HandStateChangeEvent(HandStateChangeType.CloseToOpen, aHandLocation);
            }
            else if (aToState == HandState.Closed && aFromState != HandState.Closed)
            {
                handEvent = new HandStateChangeEvent(HandStateChangeType.OpenToClose, aHandLocation);
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

            if (this.Cursor != null)
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

            if (this.Cursor != null)
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

        private Dictionary<JointType, Point> ConvertJointsToDepthSpace(SmoothedBody<KalmanSmoother> aBody)
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

        private Rect CalculateHandRect(Dictionary<JointType, Point> aJointPoints, bool aShouldDoLeftHand)
        {
            //calculate the shoulder length (from neck to shoulder)
            double shoulderLengthScale = Point.Subtract(aJointPoints[JointType.ShoulderLeft], aJointPoints[JointType.ShoulderRight]).Length / 2;

            //choose the center of the rect (left shoulder for left hand, etc.)
            Point rectCenter = (aShouldDoLeftHand ? aJointPoints[JointType.ShoulderLeft] : aJointPoints[JointType.ShoulderRight]);

            //increment the center by the offset (subtract for left side)
            rectCenter.X += _handRectCenter.X * shoulderLengthScale * (aShouldDoLeftHand ? -1 : 1);
            rectCenter.Y += _handRectCenter.Y * shoulderLengthScale * (aShouldDoLeftHand ? -1 : 1);

            //calculate rect dimensions
            double rectX = rectCenter.X - _handRectSize.Width * shoulderLengthScale / 2;
            double rectY = rectCenter.Y - _handRectSize.Height * shoulderLengthScale / 2;
            double rectWidth = _handRectSize.Width * shoulderLengthScale;
            double rectHeight = _handRectSize.Height * shoulderLengthScale;
            Rect result = new Rect(rectX, rectY, rectWidth, rectHeight);

            return result;
        }

        private Point GetRawHandPosition(Dictionary<JointType, Point> aJointPoints, bool aShouldDoLeftHand)
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

        #endregion

        #region Debug Methods

        public void InjectHandLocation(Point aLocation)
        {
            //scaled up hand location
            Point scaledHandLocation = new Point(aLocation.X * this.HandCoordRangeX, aLocation.Y * this.HandCoordRangeY);

            //filter
            Point filteredScaledHandLocation = _scaledHandLocationFilter.Next(scaledHandLocation);

            //check for valid location
            if (!double.IsNaN(filteredScaledHandLocation.X) && !double.IsNaN(filteredScaledHandLocation.Y))
            {
                //send event
                HandLocationEvent movedEvent = new HandLocationEvent(filteredScaledHandLocation);
                this.NotifyHandLocationListenersOfEvent(movedEvent);
            }
        }

        public void InjectHandStateChange(HandState aState)
        {
            //_currentHandState = aState;
            DidDetectHandStateChange(_lastConfirmedHandState, aState, _scaledHandLocationFilter.Last);
            _lastConfirmedHandState = aState;
        }

        #endregion
    }
}
