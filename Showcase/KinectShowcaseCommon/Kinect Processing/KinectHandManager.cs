﻿using KinectEx.Smoothing;
using KinectShowcaseCommon.Filters;
using KinectShowcaseCommon.UI_Elements;
using log4net;
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
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public enum FilterType
        {
            RecursiveFilter,
            AverageFilter,
            None
        }

        public static FilterType StringToFilterType(String aString)
        {
            FilterType result = FilterType.None;
            if (aString == "recursive")
            {
                result = FilterType.RecursiveFilter;
            }
            else if (aString == "average")
            {
                result = FilterType.AverageFilter;
            }
            else if (aString == "none")
            {
                result = FilterType.None;
            }
            else
            {
                Debug.WriteLine("KinectHandManager - LOG - invalid string filter type");
            }

            return result;
        }


        public class Config
        {
            public FilterType XFilter = FilterType.RecursiveFilter, YFilter = FilterType.RecursiveFilter;
            public float[] XFilterParams = {0.3f}, YFilterParams = {0.3f};
            public Point HandRectCenter  = new Point(2.0, -3);
            public Size HandRectSize = new Size(2.0, 2.0);
        }


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

        public interface HandControl
        {
            Rect KinectSpaceBounds();
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
        private KinectCursorView _cursor;
        public KinectCursorView Cursor
        {
            get
            {
                return _cursor;
            }
            set
            {
                _cursor = value;
                if (_cursor != null)
                {
                    _cursor.SetCursorPosition(_scaledHandLocationFilter.Last);
                }
            }
        }
        public Rect HandRect { get; private set; }
        public float HandCoordRangeX { get; private set; }
        public float HandCoordRangeY { get; private set; }
        public Point HandPosition
        {
            get
            {
                return _scaledHandLocationFilter.Last;
            }
        }

        private KinectManager _kinectManager;
        private CoordinateMapper _coordinateMapper;
        private WeakCollection<HandStateChangeListener> _handStateListeners = new WeakCollection<HandStateChangeListener>();
        private WeakCollection<HandLocationListener> _handLocationListeners = new WeakCollection<HandLocationListener>();
        private Object _handStateListenerLock = new object();
        private Object _handLocationListenerLock = new object();
        private PointFilter _scaledHandLocationFilter;
        private HandState _lastConfirmedHandState = HandState.Open;
        private float _depthFrameWidth, _depthFrameHeight;
        private HandStateCounter _handStateCounter = new HandStateCounter();
        private Point _handRectCenter;
        private Size _handRectSize;

        #endregion

        #region Lifecycle

        public KinectHandManager(KinectManager aManager, Config aConfig)
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

            this.LoadConfig(aConfig);

            this._kinectManager.AddSmoothBodyDataListener(this);
        }

        private void LoadConfig(Config aConfig)
        {
            ValueFilter xFilter = FilterFromeParams(aConfig.XFilter, aConfig.XFilterParams);
            ValueFilter yFilter = FilterFromeParams(aConfig.YFilter, aConfig.YFilterParams);
            _scaledHandLocationFilter = new PointFilter(xFilter, yFilter);
            _handRectCenter = aConfig.HandRectCenter;
            _handRectSize = aConfig.HandRectSize;
        }

        private ValueFilter FilterFromeParams(FilterType aType, float[] aParams)
        {
            ValueFilter result;
            if (aType == FilterType.None)
            {
                result = new ValueFilter();
            }
            else if (aType == FilterType.RecursiveFilter)
            {
                if (aParams.Length >= 1)
                {
                    result = new RecursiveValueFilter(aParams[0]);
                }
                else
                {
                    result = new RecursiveValueFilter(0.1);
                }
            }
            else if (aType == FilterType.AverageFilter)
            {
                if (aParams.Length >= 1)
                {
                    result = new AverageValueFilter((int)aParams[0]);
                }
                else
                {
                    result = new AverageValueFilter(3);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            return result;
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
                _scaledHandLocationFilter.Set(new Point(this.HandCoordRangeX / 2, this.HandCoordRangeY / 2));
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

            Point filteredScaledHandLocation = scaledHandLocation;
            //check for valid location
            if (!double.IsNaN(scaledHandLocation.X) && !double.IsNaN(scaledHandLocation.Y))
            {
                //filter
                filteredScaledHandLocation = _scaledHandLocationFilter.Next(scaledHandLocation);

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
            lock (_handStateListenerLock)
            {
                if (!this._handStateListeners.Contains(aListener))
                    this._handStateListeners.Add(aListener);
            }
        }

        private void NotifyHandStateChangeListenersOfEvent(HandStateChangeEvent aEvent)
        {
            //notify the system listener of an interaction
            this._kinectManager.InteractionListener.SystemDidRecieveInteraction();

            lock (_handStateListenerLock)
            {
                foreach (HandStateChangeListener currentListener in this._handStateListeners)
            {
                bool result = currentListener.KinectHandManagerDidDetectHandStateChange(this, aEvent);
                //skip if the event was handled
                if (result)
                {
                    break;
                }
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
            lock (_handStateListenerLock)
            {
                if (this._handStateListeners.Contains(aListener))
                    this._handStateListeners.Remove(aListener);
            }
        }

        public void AddHandLocationListener(HandLocationListener aListener)
        {
            lock (_handLocationListenerLock)
            {
                if (!this._handLocationListeners.Contains(aListener))
                {
                    this._handLocationListeners.Add(aListener);
                    aListener.KinectHandManagerDidGetHandLocation(this, new HandLocationEvent(this.HandPosition));
                }
            }
        }

        private void NotifyHandLocationListenersOfEvent(HandLocationEvent aEvent)
        {
            //notify the system listener of an interaction
            this._kinectManager.InteractionListener.SystemDidRecieveInteraction();
            Point attachPoint = new Point();
            bool isAttaching = false;
            lock (_handLocationListenerLock)
            {
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
            lock (_handLocationListenerLock)
            {
                if (this._handLocationListeners.Contains(aListener))
                    this._handLocationListeners.Remove(aListener);
            }
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
            double shoulderLengthScale = Point.Subtract(aJointPoints[JointType.SpineMid], aJointPoints[JointType.SpineBase]).Length / 2;

            //choose the center of the rect (left shoulder for left hand, etc.)
            Point rectCenter = GetBodyOrigin(aJointPoints, aShouldDoLeftHand);
            //(aShouldDoLeftHand ? aJointPoints[JointType.SpineShoulder] : aJointPoints[JointType.SpineShoulder]);
            //(aShouldDoLeftHand ? aJointPoints[JointType.ShoulderLeft] : aJointPoints[JointType.ShoulderRight]);

            //increment the center by the offset (subtract for left side)
            rectCenter.X += _handRectCenter.X * shoulderLengthScale * (aShouldDoLeftHand ? -1 : 1);
            rectCenter.Y += _handRectCenter.Y * shoulderLengthScale;

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

        private Point GetBodyOrigin(Dictionary<JointType, Point> aJointPoints, bool aShouldDoLeftHand)
        {
            double[] xValues = { aJointPoints[JointType.Neck].X, 
                                 aJointPoints[JointType.SpineShoulder].X,
                                 aJointPoints[JointType.SpineMid].X,
                                 aJointPoints[JointType.SpineBase].X };
            double x = GetAverage(xValues);

            double[] yValues = { aJointPoints[JointType.SpineShoulder].Y,
                                 aJointPoints[JointType.SpineMid].Y,
                                 aJointPoints[JointType.SpineBase].Y,
                                 aJointPoints[JointType.HipLeft].Y,
                                 aJointPoints[JointType.HipRight].Y};
            double y = GetAverage(yValues);

            Point result = new Point(x, y);
            return result;
        }

        private static double GetMedian(double[] sourceNumbers)
        {
            //Framework 2.0 version of this method. there is an easier way in F4        
            if (sourceNumbers == null || sourceNumbers.Length == 0)
                throw new System.Exception("Median of empty array not defined.");

            //make sure the list is sorted, but use a new array
            double[] sortedPNumbers = (double[])sourceNumbers.Clone();
            Array.Sort(sortedPNumbers);

            //get the median
            int size = sortedPNumbers.Length;
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)sortedPNumbers[mid] : ((double)sortedPNumbers[mid] + (double)sortedPNumbers[mid - 1]) / 2;
            return median;
        }

        private static double GetAverage(double[] aValues)
        {
            if (aValues == null || aValues.Length == 0)
                return 0;

            double sum = 0;
            foreach (double cur in aValues)
            {
                sum += cur;
            }

            return sum / aValues.Length;
        }

        #endregion

        public void SetNormalizedHandLocation(Point aLocation)
        {
            //scaled up hand location
            Point scaledHandLocation = new Point(aLocation.X * this.HandCoordRangeX, aLocation.Y * this.HandCoordRangeY);
            SetScaledHandLocation(scaledHandLocation);
        }

        public void SetScaledHandLocation(Point aLocation)
        {
            _scaledHandLocationFilter.Set(aLocation);
            this.InjectScaledHandLocation(aLocation);
        }

        #region Debug Methods

        public void InjectNormalizedHandLocation(Point aLocation)
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

        public void InjectScaledHandLocation(Point aLocation)
        {
            //filter
            Point filteredScaledHandLocation = _scaledHandLocationFilter.Next(aLocation);

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
