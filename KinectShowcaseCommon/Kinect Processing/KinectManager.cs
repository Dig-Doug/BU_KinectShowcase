using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using KinectShowcaseCommon.UI_Elements;
using KinectShowcaseCommon.ProcessHandling;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Drawing;
using Nito.KitchenSink;
using KinectEx.Smoothing;

namespace KinectShowcaseCommon.Kinect_Processing
{
    public sealed class KinectManager
    {

        #region RawBodyData

        public interface RawBodyDataListener
        {
            void KinectManagerDidGetUpdatedBodyData(KinectManager aManager, Body[] aBodies);
        }

        #endregion

        #region SmoothBodyData

        public interface SmoothBodyDataListener
        {
            void KinectManagerDidGetUpdatedBodyData(KinectManager aManager, SmoothedBody<KalmanSmoother>[] aBodies);
        }

        #endregion

        #region ColorData

        public interface ColorDataListener
        {
            void KinectManagerDidGetUpdatedColorData(KinectManager aManager, ColorFrame aColorFrame);
        }

        #endregion

        #region State

        public interface StateListener
        {
            void KinectManagerDidUpdateState(KinectManager aManager, bool aIsKinectActive);
        }

        #endregion

        #region ActiveTracking

        public interface ActivelyTrackingListener
        {
             void KinectManagerDidBeginTracking(KinectManager aManager);
             void KinectManagerDidFinishTracking(KinectManager aManager);
        }

        #endregion

        #region Properties

        //plus or minus this amount from the center
        private const float BODY_X_REGION = 1.0f;
        //plus or minus this amount from the center
        private const float BODY_Y_REGION = 1.0f;
        //plus this amount from the camera
        private const float BODY_Z_REGION = 6.0f;

        private static volatile KinectManager instance;
        private static object syncRoot = new Object();

        private CoordinateMapper coordinateMapper = null;
        private BodyFrameReader bodyFrameReader = null;
        private ColorFrameReader colorFrameReader = null;
        private Body[] bodies = null;
        private int bodyCount = 0;
        private bool _kinectState = false;
        private WeakCollection<RawBodyDataListener> rawBodyDataListeners = new WeakCollection<RawBodyDataListener>();
        private WeakCollection<SmoothBodyDataListener> _smoothBodyDataListeners = new WeakCollection<SmoothBodyDataListener>();
        private WeakCollection<ColorDataListener> colorDataListeners = new WeakCollection<ColorDataListener>();
        private WeakCollection<StateListener> stateListeners = new WeakCollection<StateListener>();
        private WeakCollection<ActivelyTrackingListener> trackingListeners = new WeakCollection<ActivelyTrackingListener>();

        private KalmanSmoother _smoother = new KalmanSmoother();
        private SmoothedBody<KalmanSmoother>[] _smoothedBodies;

        public KinectSensor KinectSensor { get; private set; }
        public ulong CurrentlyTrackingId { get; private set; }
        public Body CurrentlyTrackingBody { get; private set; }
        public KinectHandManager HandManager { get; private set; }
        public KinectGestureManager GestureManager { get; private set; }
        public bool ShouldSendEvents { get; set; }
        public ISystemInteractionListener InteractionListener { get; private set; }

        #endregion

        #region Lifecycle

        public static KinectManager Default
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new KinectManager();
                    }
                }

                return instance;
            }
        }

        private KinectManager()
        {

        }

        public void Init(ISystemInteractionListener aListener)
        {
            this.InteractionListener = aListener;
            this.ShouldSendEvents = true;
            this.CurrentlyTrackingId = 0;

            // get the sensor
            this.KinectSensor = KinectSensor.GetDefault();

            this.InitBody();

            this.InitColor();

            this.HandManager = new KinectHandManager(this);
            //this.GestureManager = new KinectGestureManager(this);

            // set IsAvailableChanged event notifier
            this.KinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.KinectSensor.Open();
        }

        private void InitBody()
        {
            KalmanSmoothingParameters smoothingParam = new KalmanSmoothingParameters();
            smoothingParam.JitterRadius = 0.1f;
            smoothingParam.MeasurementUncertainty = 0.0001f;

            _smoothedBodies = new SmoothedBody<KalmanSmoother>[this.KinectSensor.BodyFrameSource.BodyCount];
            for (int i = 0; i < this.KinectSensor.BodyFrameSource.BodyCount; i++)
            {
                _smoothedBodies[i] = new SmoothedBody<KalmanSmoother>(smoothingParam);
            }

            // get the coordinate mapper
            this.coordinateMapper = this.KinectSensor.CoordinateMapper;

            // get total number of bodies from BodyFrameSource
            this.bodies = new Body[this.KinectSensor.BodyFrameSource.BodyCount];

            // sets total number of possible tracked bodies
            this.bodyCount = this.KinectSensor.BodyFrameSource.BodyCount;

            // open the reader for the body frames
            this.bodyFrameReader = this.KinectSensor.BodyFrameSource.OpenReader();

            // wire handler for frame arrival
            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;
        }

        private void InitColor()
        {
            // open the reader for the color frames
            this.colorFrameReader = this.KinectSensor.ColorFrameSource.OpenReader();

            // wire handler for frame arrival
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
        }

        public void Destroy()
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.bodies != null)
            {
                foreach (Body body in this.bodies)
                {
                    if (body != null)
                    {
                        //body.Dispose(); not needed in WPF?
                    }
                }
            }

            if (this.KinectSensor != null)
            {
                this.KinectSensor.Close();
                this.KinectSensor = null;
            }
        }

        #endregion

        #region Kinect Sensor Callbacks

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            if (!this.KinectSensor.IsAvailable)
            {
                _kinectState = false;
            }
            else
            {
                _kinectState = true;
            }

            this.NotifyStateListenersOfUpdate(_kinectState);
        }

        #endregion

        #region Body Data

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                //update the smooth bodies

                for (int i = 0; i < _smoothedBodies.Length; i++)
                {
                    Body currentBody = bodies[i];
                    //update the smoothed version
                    _smoothedBodies[i].Update(currentBody);
                }

                //get the closest valid body if we're not already tracking someone that is in the control region
                if (this.CurrentlyTrackingId == 0 || this.CurrentlyTrackingBody == null || !IsBodyInControlRegion(this.CurrentlyTrackingBody))
                {
                    Body closestValid = null;
                    float depth = 9999999;
                    //enumerate though, find the closest one in the control region
                    foreach (Body currentBody in bodies)
                    {
                        if (currentBody != null && currentBody.IsTracked && IsBodyInControlRegion(currentBody))
                        {
                            CameraSpacePoint spineLoc = currentBody.Joints[JointType.SpineBase].Position;
                            if (depth > spineLoc.Z)
                            {
                                closestValid = currentBody;
                                depth = spineLoc.Z;
                            }
                        }
                    }

                    //see if we got someone
                    if (closestValid != null)
                    {
                        //if so, store info on them
                        this.CurrentlyTrackingBody = closestValid;
                        this.CurrentlyTrackingId = closestValid.TrackingId;

                        this.NotifyStateListenersOfBeginTracking();
                    }
                    else
                    {
                        this.CurrentlyTrackingId = 0;

                        this.NotifyStateListenersOfFinishedTracking();
                    }
                }

                //if we're tracking someone, get the body
                if (this.CurrentlyTrackingId != 0)
                {
                    IEnumerable<Body> res = this.bodies.Where(b => b.TrackingId == this.CurrentlyTrackingId);
                    if (res.Count() > 0)
                        this.CurrentlyTrackingBody = res.First();
                    else
                        this.CurrentlyTrackingId = 0;
                }

                //notify listeners
                this.NotifyRawBodyDataListenersOfUpdate();
                this.NotifySmoothBodyDataListenersOfUpdate();
            }
        }

        bool IsBodyInControlRegion(Body aBody)
        {
            bool result = true;
            CameraSpacePoint spineLoc = aBody.Joints[JointType.SpineBase].Position;
            if (spineLoc.X < -BODY_X_REGION || spineLoc.X > BODY_X_REGION)
            {
                result = false;
            }

            if (spineLoc.X < -BODY_Y_REGION || spineLoc.X > BODY_Y_REGION)
            {
                result = false;
            }

            if (spineLoc.Z > BODY_Z_REGION)
            {
                result = false;
            }

            return result;
        }

        #endregion

        #region Color Data

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    /*
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    if (this.ColorBitmap == null)
                    {
                        // create the bitmap to display
                        this.ColorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                    }

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.ColorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.ColorBitmap.PixelWidth) && (colorFrameDescription.Height == this.ColorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.ColorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.ColorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.ColorBitmap.PixelWidth, this.ColorBitmap.PixelHeight));
                        }

                        this.ColorBitmap.Unlock();
                    }
                     * */
                    //this.ColorBitmap = ToBitmap(colorFrame);

                    this.NotifyColorDataListenersOfUpdate(colorFrame);
                }
            }
        }

        #endregion

        #region Listener Handling

        public void AddRawBodyDataListener(RawBodyDataListener aListener)
        {
            if (!this.rawBodyDataListeners.Contains(aListener))
            {
                this.rawBodyDataListeners.Add(aListener);
            }
        }

        private void NotifyRawBodyDataListenersOfUpdate()
        {
            if (this.ShouldSendEvents)
            {
                foreach (RawBodyDataListener currentListener in this.rawBodyDataListeners)
                {
                    currentListener.KinectManagerDidGetUpdatedBodyData(this, this.bodies);
                }
            }
        }

        public void RemoveRawBodyDataListener(RawBodyDataListener aListener)
        {
            if (this.rawBodyDataListeners.Contains(aListener))
            {
                this.rawBodyDataListeners.Remove(aListener);
            }
        }

        public void AddSmoothBodyDataListener(SmoothBodyDataListener aListener)
        {
            if (!this._smoothBodyDataListeners.Contains(aListener))
            {
                this._smoothBodyDataListeners.Add(aListener);
            }
        }

        private void NotifySmoothBodyDataListenersOfUpdate()
        {
            if (this.ShouldSendEvents)
            {
                foreach (SmoothBodyDataListener currentListener in this._smoothBodyDataListeners)
                {
                    currentListener.KinectManagerDidGetUpdatedBodyData(this, _smoothedBodies);
                }
            }
        }

        public void RemoveSmoothBodyDataListener(SmoothBodyDataListener aListener)
        {
            if (this._smoothBodyDataListeners.Contains(aListener))
            {
                this._smoothBodyDataListeners.Remove(aListener);
            }
        }

        public void AddColorDataListener(ColorDataListener aListener)
        {
            if (!this.colorDataListeners.Contains(aListener))
            {
                this.colorDataListeners.Add(aListener);
            }
        }

        private void NotifyColorDataListenersOfUpdate(ColorFrame aColorFrame)
        {
            if (this.ShouldSendEvents)
            {
                foreach (ColorDataListener currentListener in this.colorDataListeners)
                {
                    currentListener.KinectManagerDidGetUpdatedColorData(this, aColorFrame);
                }
            }
        }

        public void RemoveColorDataListener(ColorDataListener aListener)
        {
            if (this.colorDataListeners.Contains(aListener))
            {
                this.colorDataListeners.Remove(aListener);
            }
        }

        public void AddStateListener(StateListener aListener)
        {
            if (!this.stateListeners.Contains(aListener))
            {
                this.stateListeners.Add(aListener);
                aListener.KinectManagerDidUpdateState(this, _kinectState);
            }
        }

        private void NotifyStateListenersOfUpdate(bool aState)
        {
            if (this.ShouldSendEvents)
            {
                foreach (StateListener currentListener in this.stateListeners)
                {
                    currentListener.KinectManagerDidUpdateState(this, aState);
                }
            }
        }

        public void RemoveStateListener(StateListener aListener)
        {
            if (this.stateListeners.Contains(aListener))
            {
                this.stateListeners.Remove(aListener);
            }
        }

        public void AddTrackingListener(ActivelyTrackingListener aListener)
        {
            if (!this.trackingListeners.Contains(aListener))
            {
                this.trackingListeners.Add(aListener);
            }
        }

        private void NotifyStateListenersOfBeginTracking()
        {
            if (this.ShouldSendEvents)
            {
                foreach (ActivelyTrackingListener currentListener in this.trackingListeners)
                {
                    currentListener.KinectManagerDidBeginTracking(this);
                }
            }
        }

        private void NotifyStateListenersOfFinishedTracking()
        {
            if (this.ShouldSendEvents)
            {
                foreach (ActivelyTrackingListener currentListener in this.trackingListeners)
                {
                    currentListener.KinectManagerDidFinishTracking(this);
                }
            }
        }

        public void RemoveTrackingListener(ActivelyTrackingListener aListener)
        {
            if (this.trackingListeners.Contains(aListener))
            {
                this.trackingListeners.Remove(aListener);
            }
        }

        #endregion
    }
}
