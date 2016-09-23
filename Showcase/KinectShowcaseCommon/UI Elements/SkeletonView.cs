using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using KinectShowcaseCommon.Kinect_Processing;
using System.ComponentModel;
using KinectEx.Smoothing;

namespace KinectShowcaseCommon.UI_Elements
{
    //Adapted from the BodyBasics-WPF Demo for Kinect v2
    public class SkeletonView : LiveBackground, KinectManager.SmoothBodyDataListener
    {
        // Radius of drawn hand circles
        private const double HandSize = 30;
        // Thickness of drawn joint lines
        private const double JointThickness = 3;
        // Thickness of clip edge rectangles
        private const double ClipBoundsThickness = 10;
        // Constant for clamping Z values of camera space points from being negative
        private const float InferredZPositionClamp = 0.1f;
        // Brush used for drawing hands that are currently tracked as closed
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        // Brush used for drawing hands that are currently tracked as opened
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        // Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        // Brush used for drawing joints that are currently tracked
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        // Brush used for drawing joints that are currently inferred
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        // Pen used for drawing bones that are currently inferred
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        // Drawing group for body rendering output
        private DrawingGroup drawingGroup;
        // definition of bones
        private List<Tuple<JointType, JointType>> bones;
        // Width of display (depth space)
        private int displayWidth;
        // Height of display (depth space)
        private int displayHeight;
        // List of colors for each body tracked
        private List<Pen> bodyColors;
        // whether or not to draw the clipping edges
        private bool showClipping = false;

        // Active Kinect sensor
        private KinectManager kinectManager = null;
        // Coordinate mapper to map one type of point to another
        private CoordinateMapper coordinateMapper = null;

        private Image _skeletonImage = null;

        public bool ShouldDrawHeadJoint { get; set; }
        public bool ShouldDrawHandRect { get; set; }

        // Initializes a new instance of the MainWindow class.
        public SkeletonView()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.ShouldDrawHeadJoint = false;
                this.ShouldDrawHandRect = false; //not quite right

                // Create the drawing group we'll use for drawing
                this.drawingGroup = new DrawingGroup();

                _skeletonImage = new Image();

                // Create an image source that we can use in our image control
                DrawingImage image = new DrawingImage(this.drawingGroup);
                _skeletonImage.Source = image;
                this.Children.Add(_skeletonImage);
            }
        }

        public void SetKinectManager(KinectManager aManager)
        {
            // one sensor is currently supported
            this.kinectManager = aManager;

            if (this.kinectManager != null)
            {
                // get the coordinate mapper
                this.coordinateMapper = this.kinectManager.KinectSensor.CoordinateMapper;


                // a bone defined as a line between two joints
                this.bones = new List<Tuple<JointType, JointType>>();

                // Torso
                this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

                // Right Arm
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

                // Left Arm
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

                // Right Leg
                this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

                // Left Leg
                this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

                // populate body colors, one for each BodyIndex
                this.bodyColors = new List<Pen>();

                this.bodyColors.Add(new Pen(Brushes.Red, 6));
                this.bodyColors.Add(new Pen(Brushes.Gray, 6));
                //this.bodyColors.Add(new Pen(Brushes.Green, 6 * _skeletonScaleX));
                //this.bodyColors.Add(new Pen(Brushes.Blue, 6 * _skeletonScaleX));
                //this.bodyColors.Add(new Pen(Brushes.Indigo, 6 * _skeletonScaleX));
                //this.bodyColors.Add(new Pen(Brushes.Violet, 6 * _skeletonScaleX));

                this.kinectManager.AddSmoothBodyDataListener(this);
            }
        }

        ~SkeletonView()
        {
            if (this.kinectManager != null)
            {
                this.kinectManager.RemoveSmoothBodyDataListener(this);
                this.kinectManager = null;
            }
        }

        protected override void UpdateImageLayouts()
        {
            base.UpdateImageLayouts();

            if (this.kinectManager != null)
            {
                // get the depth (display) extents
                FrameDescription colorFrameDescription = this.kinectManager.KinectSensor.ColorFrameSource.FrameDescription;
                FrameDescription depthFrameDescription = this.kinectManager.KinectSensor.DepthFrameSource.FrameDescription;

                // get size of joint space
                this.displayWidth = (int)(colorFrameDescription.Width * this._scale);
                this.displayHeight = (int)(colorFrameDescription.Height * this._scale);

                Point topLeftCorner = new Point();
                topLeftCorner.X = 0.0;// this.Width / 2 - this.displayWidth;
                topLeftCorner.Y = 0.0;//this.Height / 2 - this.displayHeight;

                //move the controls to the correct locations
                Canvas.SetLeft(_skeletonImage, topLeftCorner.X);
                Canvas.SetTop(_skeletonImage, topLeftCorner.Y);
            }
        }

        public void KinectManagerDidGetUpdatedBodyData(KinectManager aManager, SmoothedBody<KalmanSmoother>[] aBodies)
        {
            //update on UI thread
            Dispatcher.InvokeAsync((Action)delegate()
            {
                DrawBodies(aBodies);
            });
        }

        private void DrawBodies(SmoothedBody<KalmanSmoother>[] aBodies)
        {
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                int penIndex = 0;
                foreach (SmoothedBody<KalmanSmoother> body in aBodies)
                {
                    if (body.TrackingId == this.kinectManager.CurrentlyTrackingId)
                        penIndex = 0;
                    else
                        penIndex = 1;
                    Pen drawPen = this.bodyColors[penIndex];

                    if (body.IsTracked)
                    {
                        this.DrawClippedEdges(body, dc);

                        var joints = body.Joints;

                        // convert the joint points to depth (display) space
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                        foreach (JointType jointType in joints.Keys)
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = InferredZPositionClamp;
                            }

                            ColorSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(position);
                            jointPoints[jointType] = new Point(depthSpacePoint.X * this._scale, depthSpacePoint.Y * this._scale);
                        }

                        this.DrawBody(joints, jointPoints, dc, drawPen);

                        if (body != null && KinectManager.Default.CurrentlyTrackingBody != null && body.TrackingId == KinectManager.Default.CurrentlyTrackingBody.TrackingId)
                        {
                            if (KinectManager.Default.HandManager.TrackingLeftHand)
                                this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            else
                                this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }

                        //draw hand rect
                        if (penIndex == 0 && this.ShouldDrawHandRect)
                        {
                            Rect handRect = this.kinectManager.HandManager.HandRect;
                            handRect.X *= this.displayWidth;
                            handRect.Y *= this.displayHeight;
                            handRect.Width *= this.displayWidth;
                            handRect.Height *= this.displayHeight;
                            dc.DrawRectangle(this.trackedJointBrush, drawPen, handRect);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
            }
        }

        /// Draws a body
        private void DrawBody(IReadOnlyDictionary<JointType, KinectEx.IJoint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                //don't draw head if we don't want to
                if (!(!this.ShouldDrawHeadJoint && bone.Item1 == JointType.Head))
                {
                    this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
                }
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                //don't draw head if we don't want to
                if (!(!this.ShouldDrawHeadJoint && jointType == JointType.Head))
                {
                    Brush drawBrush = null;

                    TrackingState trackingState = joints[jointType].TrackingState;

                    if (trackingState == TrackingState.Tracked)
                    {
                        drawBrush = this.trackedJointBrush;
                    }
                    else if (trackingState == TrackingState.Inferred)
                    {
                        drawBrush = this.inferredJointBrush;
                    }

                    if (drawBrush != null)
                    {
                        drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                    }
                }
            }
        }

        /// Draws one bone of a body (joint to joint)
        private void DrawBone(IReadOnlyDictionary<JointType, KinectEx.IJoint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            var joint0 = joints[jointType0];
            var joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// Draws indicators to show which edges are clipping body data
        private void DrawClippedEdges(SmoothedBody<KalmanSmoother> body, DrawingContext drawingContext)
        {
            if (this.showClipping)
            {
                FrameEdges clippedEdges = body.ClippedEdges;

                if (clippedEdges.HasFlag(FrameEdges.Bottom))
                {
                    drawingContext.DrawRectangle(
                        Brushes.Red,
                        null,
                        new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
                }

                if (clippedEdges.HasFlag(FrameEdges.Top))
                {
                    drawingContext.DrawRectangle(
                        Brushes.Red,
                        null,
                        new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
                }

                if (clippedEdges.HasFlag(FrameEdges.Left))
                {
                    drawingContext.DrawRectangle(
                        Brushes.Red,
                        null,
                        new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
                }

                if (clippedEdges.HasFlag(FrameEdges.Right))
                {
                    drawingContext.DrawRectangle(
                        Brushes.Red,
                        null,
                        new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
                }
            }
        }

    }
}
