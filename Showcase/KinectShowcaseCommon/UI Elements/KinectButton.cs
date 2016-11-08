using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Automation.Provider;
using System.Windows.Media.Animation;
using KinectShowcaseCommon.Kinect_Processing;
using System.Diagnostics;
using System.ComponentModel;

namespace KinectShowcaseCommon.UI_Elements
{
    public class KinectButton : Button, KinectHandManager.HandStateChangeListener, KinectHandManager.HandLocationListener, KinectHandManager.HandControl
    {
        private KinectManager _kinectManager = null;
        private Rect _kinectSpaceBounds = new Rect(-10, -10, 0, 0);
        private Rect _attachBounds;
        private bool _clickBeganInside = false;

        public bool ShouldHandAttach = true;

        public KinectButton()
            : base()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _kinectManager = KinectManager.Default;
                if (_kinectManager != null)
                {
                    _kinectManager.HandManager.AddHandStateChangeListener(this);
                    _kinectManager.HandManager.AddHandLocationListener(this);
                }
            }
        }

        ~KinectButton()
        {
            if (_kinectManager != null)
            {
                _kinectManager.HandManager.RemoveHandStateChangeListener(this);
                _kinectManager.HandManager.RemoveHandLocationListener(this);
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            this.CalculateKinectBounds();

        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            this.CalculateKinectBounds();
        }

        private void CalculateKinectBounds()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                //get the position of the ui element in window coords
                UIElement control = this;
                Point topLeft = new Point(0, 0);
                this.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Point bottomRight = new Point(this.ActualWidth, this.ActualHeight); ;// new Point(this.DesiredSize.Width, this.DesiredSize.Height);

                if (bottomRight.X == 0 || bottomRight.Y == 0)
                    bottomRight = new Point(this.DesiredSize.Width, this.DesiredSize.Height);

                while (control != null)
                {
                    UIElement parent = VisualTreeHelper.GetParent(control) as UIElement;

                    if (parent == null)
                    {
                        break;
                    }
                    else
                    {
                        topLeft = control.TranslatePoint(topLeft, parent);
                        bottomRight = control.TranslatePoint(bottomRight, parent);
                        control = parent;
                    }
                }
                //divide by window dimensions to get from 0.0 -> 1.0f
                topLeft.X /= control.RenderSize.Width;
                topLeft.Y /= control.RenderSize.Height;
                bottomRight.X /= control.RenderSize.Width;
                bottomRight.Y /= control.RenderSize.Height;

                //scale by range of hand manager coords
                topLeft.X *= _kinectManager.HandManager.HandCoordRangeX;
                bottomRight.X *= _kinectManager.HandManager.HandCoordRangeX;
                topLeft.Y *= _kinectManager.HandManager.HandCoordRangeY;
                bottomRight.Y *= _kinectManager.HandManager.HandCoordRangeY;

                //these points now define the bounds of the button in kinect space
                this._kinectSpaceBounds = new Rect(topLeft, bottomRight);
                _attachBounds = new Rect(_kinectSpaceBounds.X + _kinectSpaceBounds.Width / 4, _kinectSpaceBounds.Y + _kinectSpaceBounds.Height / 4, _kinectSpaceBounds.Width / 2, _kinectSpaceBounds.Height / 2);

                if (_kinectSpaceBounds.Height == 0.0 || _kinectSpaceBounds.Width == 0)
                {
                    Debug.WriteLine("Kinect Button - Error! - Weired dimensions");
                }

            }
        }

        public bool KinectHandManagerDidDetectHandStateChange(KinectHandManager aManager, KinectHandManager.HandStateChangeEvent aEvent)
        {
            //holds whether we handled the event
            bool result = false;
            if (this.IsEnabledCore && this.IsVisible)
            {
                if (aEvent.EventType == KinectHandManager.HandStateChangeType.OpenToClose)
                {
                    if (this._kinectSpaceBounds.Contains(aEvent.HandPosition))
                    {
                        _clickBeganInside = true;
                        //event handled
                        result = true;

                        Dispatcher.InvokeAsync((Action)delegate()
                        {
                            //click the button
                            ButtonAutomationPeer peer = new ButtonAutomationPeer(this);
                            IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                            invokeProv.Invoke();
                        });
                    }
                    else
                    {
                        _clickBeganInside = false;
                    }
                }
                else if (aEvent.EventType == KinectHandManager.HandStateChangeType.CloseToOpen)
                {
                    if (_clickBeganInside && this._kinectSpaceBounds.Contains(aEvent.HandPosition))
                    {
                        //event handled
                        result = true;

                        /*
                        Dispatcher.InvokeAsync((Action)delegate()
                        {
                            //click the button
                            ButtonAutomationPeer peer = new ButtonAutomationPeer(this);
                            IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                            invokeProv.Invoke();
                        });
                         * */
                    }
                }
            }

            return result;
        }

        public bool KinectHandManagerDidGetHandLocation(KinectHandManager aManager, KinectHandManager.HandLocationEvent aEvent)
        {
            bool result = false;

            if (this.IsEnabledCore && this.IsVisible)
            {
                if (this._attachBounds.Contains(aEvent.HandPosition))
                {
                    result = true;

                    //TODO - have some sort of mouse over animation
                }
                else
                {

                }
            }

            return result;
        }

        public bool HandShouldAttach()
        {
            return this.ShouldHandAttach && this.IsVisible;
        }

        public Point AttachLocation()
        {
            double centerX = _kinectSpaceBounds.X + _kinectSpaceBounds.Width / 2;
            double centerY = _kinectSpaceBounds.Y + _kinectSpaceBounds.Height / 2;
            return new Point(centerX, centerY);
        }

        public Rect KinectSpaceBounds()
        {
            return this._kinectSpaceBounds;
        }

    }
}
