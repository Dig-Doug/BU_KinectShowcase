using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Diagnostics;

namespace KinectShowcaseCommon.Kinect_Processing
{
    public class KinectGestureManager
    {
        #region GestureEvents

        public enum GestureEventType
        {
            Unknown,
            SwipeLeftToRight,
            SwipeRightToLeft,
            SwipeBottomToTop,
            SwipeTopToBottom,
        }

        public enum GestureEventState
        {
            GestureBegan,
            GestureChanged,
            GestureEnded,
        }

        public class GestureEventData
        {
            public readonly float Progess = 0.0f;
            public readonly GestureEventState State;

            public GestureEventData(float aProgess, GestureEventState aState)
            {
                this.Progess = aProgess;
                this.State = aState;
            }
        }

        public interface GestureEventListener
        {
            void KinectGestureManagerDidRecognizeGesture(KinectGestureManager aManager, GestureEventType aEventType, GestureEventData aData);
        }

        #endregion

        /*
        private static readonly float CONFIDENCE_MINIMUM = 0.5f;

        private static readonly string GESTURE_DATABASE_PATH = @"Database\KinectShowcaseGestures.gbd";

        private KinectManager _kinectManager;
        private VisualGestureBuilderFrameSource gestureSource = null;
        private VisualGestureBuilderFrameReader gestureReader = null;
        private List<GestureEventListener> gestureEventListeners = new List<GestureEventListener>();
        private Dictionary<string, Gesture> gestures = new Dictionary<string, Gesture>();

        private Gesture currentActiveGesture = null;
        private float currentActiveGestureProgress = -1;

        public KinectGestureManager(KinectManager aManager)
        {
            this._kinectManager = aManager;
            this.gestureSource = new VisualGestureBuilderFrameSource(_kinectManager.KinectSensor, 0);

            VisualGestureBuilderDatabase db = new VisualGestureBuilderDatabase(GESTURE_DATABASE_PATH);

            //add all of our gestures
            foreach (Gesture currentGesture in db.AvailableGestures)
            {
                this.gestures.Add(currentGesture.Name, currentGesture);
                this.gestureSource.AddGesture(currentGesture);
            }

            this.gestureSource.TrackingIdLost += OnTrackingIdLost;
            this.gestureReader = this.gestureSource.OpenReader();
            this.gestureReader.IsPaused = true;
            this.gestureReader.FrameArrived += OnGestureFrameArrived;
        }

        public void Destroy()
        {
            if (this.gestureReader != null)
            {
                this.gestureReader.FrameArrived -= this.OnGestureFrameArrived;
                this.gestureReader.Dispose();
                this.gestureReader = null;
            }
            if (this.gestureSource != null)
            {
                this.gestureSource.TrackingIdLost -= this.OnTrackingIdLost;
                this.gestureSource.Dispose();
            }
        }

        #region Gesture Recognition

        private void OnGestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    //check if we're tracking a particular gesture
                    if (currentActiveGesture == null)
                    {
                        //get the discrete results
                        var discreteResults = frame.DiscreteGestureResults;

                        if (discreteResults != null)
                        {

                            //hold the gesture & results we're most confident
                            Gesture mostConfidentGesture = null;
                            DiscreteGestureResult mostConfidentGestureResult = null;

                            foreach (Gesture currentGesture in discreteResults.Keys)
                            {
                                var result = discreteResults[currentGesture];

                                //check if we're confident enough
                                if (result.Confidence >= CONFIDENCE_MINIMUM)
                                {
                                    //keep track of the MOST confident
                                    if (mostConfidentGestureResult == null || mostConfidentGestureResult.Confidence < result.Confidence)
                                    {
                                        mostConfidentGesture = currentGesture;
                                        mostConfidentGestureResult = result;
                                    }
                                }
                            }

                            //check if we actually recognized a gesture and we're confident about it
                            if (mostConfidentGesture != null)
                            {
                                //process the recognized gesture
                                this.DescreteGestureRecognized(mostConfidentGesture, mostConfidentGestureResult);
                            }
                        }
                    }
                    else
                    {
                        //get the continuous results
                        var continuousResults = frame.ContinuousGestureResults;
                        if (continuousResults != null)
                        {
                            if (continuousResults.ContainsKey(currentActiveGesture))
                            {
                                //get the results for the gesture we're tracking
                                var result = continuousResults[currentActiveGesture];

                                //process the gesture
                                ContinousGestureRecognized(currentActiveGesture, result);
                            }
                        }
                    }
                }
            }
        }

        private void DescreteGestureRecognized(Gesture aRecognizedGesture, DiscreteGestureResult aResult)
        {
            //print to log
            Debug.WriteLine("Recognized DISCRETE gesture: " + aRecognizedGesture.Name + " confidence: " + aResult.Confidence);

            GestureEventType eventType = GestureEventType.Unknown;
            GestureEventData eventData = null;
            if (aRecognizedGesture.Name.Equals("Swipe_LeftToRight"))
            {
                eventType = GestureEventType.SwipeLeftToRight;
                eventData = new GestureEventData(aResult.Confidence, GestureEventState.GestureBegan);
            }
            else if (aRecognizedGesture.Name.Equals("Swipe_RightToLeft"))
            {
                eventType = GestureEventType.SwipeRightToLeft;
                eventData = new GestureEventData(aResult.Confidence, GestureEventState.GestureBegan);
            }

            if (eventType != GestureEventType.Unknown)
            {
                this.NotifyGestureEventListenersOfEvent(eventType, eventData);
            }

            string progressName = aRecognizedGesture.Name + "Progress";
            if (gestures.ContainsKey(progressName))
            {
                currentActiveGesture = gestures[progressName];
                currentActiveGestureProgress = -1f;
            }
            else
            {
                currentActiveGesture = aRecognizedGesture;
            }
        }

        private void ContinousGestureRecognized(Gesture aRecognizedGesture, ContinuousGestureResult aResult)
        {
            //print to log
            GestureEventType eventType = GestureEventType.Unknown;
            GestureEventData eventData = null;
            if (aRecognizedGesture.Name.Equals("Swipe_LeftToRightProgress"))
            {
                eventType = GestureEventType.SwipeLeftToRight;
            }
            else if (aRecognizedGesture.Name.Equals("Swipe_RightToLeftProgress"))
            {
                eventType = GestureEventType.SwipeRightToLeft;
            }

            if (aResult.Progress > -0.1 && (currentActiveGestureProgress == -1f || Math.Abs(aResult.Progress - currentActiveGestureProgress) < 0.1f))
            {
                Debug.WriteLine("CONTINOUS gesture changed: " + aRecognizedGesture.Name + " progress: " + aResult.Progress);

                eventData = new GestureEventData(aResult.Progress, GestureEventState.GestureChanged);
                currentActiveGestureProgress = aResult.Progress;
            }
            else
            {
                Debug.WriteLine("CONTINOUS gesture ended: " + aRecognizedGesture.Name + " progress: " + currentActiveGestureProgress);

                eventData = new GestureEventData(currentActiveGestureProgress, GestureEventState.GestureEnded);
                currentActiveGesture = null;
                currentActiveGestureProgress = -1f;
            }

            if (eventType != GestureEventType.Unknown)
            {
                this.NotifyGestureEventListenersOfEvent(eventType, eventData);
            }
        }

        void OnTrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            if (this.gestureReader != null)
            {
                this.gestureReader.IsPaused = true;
            }
        }

        #endregion

        #region Listener Handling

        public void AddGestureEventListener(GestureEventListener aListener)
        {
            if (!this.gestureEventListeners.Contains(aListener))
            {
                this.gestureEventListeners.Add(aListener);
            }
        }

        private void NotifyGestureEventListenersOfEvent(GestureEventType aEvent, GestureEventData aData)
        {
            foreach (GestureEventListener currentListener in this.gestureEventListeners)
            {
                currentListener.KinectGestureManagerDidRecognizeGesture(this, aEvent, aData);
            }
        }

        public void RemoveGestureEventListener(GestureEventListener aListener)
        {
            if (this.gestureEventListeners.Contains(aListener))
            {
                this.gestureEventListeners.Remove(aListener);
            }
        }

        #endregion
         * */
    }
}
