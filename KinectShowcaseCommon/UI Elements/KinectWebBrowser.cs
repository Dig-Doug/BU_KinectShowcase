using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Microsoft.Kinect.Input;
using System.Diagnostics;
using KinectShowcaseCommon.Kinect_Processing;
using System.Runtime.InteropServices;

namespace KinectShowcaseCommon.UI_Elements
{
    public class KinectWebBrowser : UserControl, KinectHandManager.HandStateChangeListener, KinectHandManager.HandLocationListener
    {
        private const double SCROLL_SCALAR = 10000;
        private const double MAX_CLICK_DELTA = 0.05;

        private static bool hasInitUserAgent = false;

        private KinectManager _kinectManager = null;
        private Rect _kinectSpaceBounds;
        private bool _clickBeganInside = false;
        private Point _clickBeganPoint;
        private bool _hasScrolled = false;
        private Point _lastScrollPoint;

        #region Lifecycle

        public KinectWebBrowser()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _kinectManager = KinectManager.Default;
                if (_kinectManager != null)
                {
                    _kinectManager.HandManager.AddHandStateChangeListener(this);
                    _kinectManager.HandManager.AddHandLocationListener(this);
                }

                if (!hasInitUserAgent)
                {
                    hasInitUserAgent = true;
                    //WebConfig config = new WebConfig();
                    //config.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 5_1 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9B176 Safari/7534.48.3";
                    //WebCore.Initialize(config);
                }
            }
        }

        ~KinectWebBrowser()
        {
            if (_kinectManager != null)
            {
                _kinectManager.HandManager.RemoveHandStateChangeListener(this);
                _kinectManager.HandManager.RemoveHandLocationListener(this);
            }
        }

        #endregion

        #region Kinect Region Stuff

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            this.CalculateKinectBounds();

        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            //this.CalculateKinectBounds(); // makes not display
        }

        private void CalculateKinectBounds()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                //get the position of the ui element in window coords
                UIElement control = this;
                Point topLeft = new Point(0, 0);
                this.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Point bottomRight = new Point(control.RenderSize.Width, control.RenderSize.Height);
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
                //these points now define the bounds of the button in kinect space
                this._kinectSpaceBounds = new Rect(topLeft, bottomRight);
            }
        }

        #endregion

        private void SetPosition(int a, int b)
        {
            SetCursorPos(a, b);
        }

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        //This simulates a left mouse click
        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        public void Click(int x, int y)
        {
           // this.InjectMouseMove(x, y);
            //this.InjectMouseDown(Awesomium.Core.MouseButton.Left);
            //this.InjectMouseUp(Awesomium.Core.MouseButton.Left);
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
                        _hasScrolled = false;
                        _clickBeganPoint = aEvent.HandPosition;
                        //event handled
                        result = true;
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

                        if (!_hasScrolled && Point.Subtract(aEvent.HandPosition, _clickBeganPoint).Length < MAX_CLICK_DELTA)
                        {
                            int mouseX = (int)(System.Windows.SystemParameters.PrimaryScreenWidth * aEvent.HandPosition.X);
                            int mouseY = (int)(System.Windows.SystemParameters.PrimaryScreenHeight * aEvent.HandPosition.Y);
                            Dispatcher.InvokeAsync((Action)delegate()
                            {
                                this.Click(mouseX, mouseY);
                            });
                        }
                    }

                    _clickBeganInside = false;
                    _hasScrolled = false;
                }
            }

            return result;
        }

        public bool KinectHandManagerDidGetHandLocation(KinectHandManager aManager, KinectHandManager.HandLocationEvent aEvent)
        {
            bool result = false;

            if (this.IsEnabledCore && this.IsVisible)
            {
                if (this._kinectSpaceBounds.Contains(aEvent.HandPosition))
                {
                    //always move mouse so is under hand
                    int mouseX = (int)(System.Windows.SystemParameters.PrimaryScreenWidth * aEvent.HandPosition.X);
                    int mouseY = (int)(System.Windows.SystemParameters.PrimaryScreenHeight * aEvent.HandPosition.Y);
                    Dispatcher.InvokeAsync((Action)delegate()
                    {
                        //this.InjectMouseMove(mouseX, mouseY);
                    });
                }

                if (_clickBeganInside)
                {
                    result = true;

                    if (!_hasScrolled && Point.Subtract(aEvent.HandPosition, _clickBeganPoint).Length >= MAX_CLICK_DELTA)
                    {
                        _hasScrolled = true;
                        _lastScrollPoint = aEvent.HandPosition;

                        Debug.WriteLine("Began scrolling");
                    }

                    if (_hasScrolled)
                    {
                        double xDistance = aEvent.HandPosition.X - _lastScrollPoint.X;
                        double yDistance = aEvent.HandPosition.Y - _lastScrollPoint.Y;

                        int wheelX = (int)(xDistance * SCROLL_SCALAR);
                        int wheelY = -(int)(yDistance * SCROLL_SCALAR);

                        Dispatcher.InvokeAsync((Action)delegate()
                        {
                            //this.InjectMouseWheel(wheelY, wheelX);
                        });
                        _lastScrollPoint = aEvent.HandPosition;

                        Debug.WriteLine("X: " + wheelX + " Y: " + wheelY);
                    }

                }
            }

            return result;
        }

        public bool HandShouldAttach()
        {
            return false;
        }

        public Point AttachLocation()
        {
            return new Point(0.5,0.5);
        }
    }
}
