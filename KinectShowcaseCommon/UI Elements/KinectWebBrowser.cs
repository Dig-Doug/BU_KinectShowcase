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
using CefSharp.Wpf;
using CefSharp;

namespace KinectShowcaseCommon.UI_Elements
{
    public class KinectWebBrowser : ChromiumWebBrowser, KinectHandManager.HandStateChangeListener, KinectHandManager.HandLocationListener
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
            this.Address = "http://bu.edu";
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

                //scale by range of hand manager coords
                topLeft.X *= _kinectManager.HandManager.HandCoordRangeX;
                bottomRight.X *= _kinectManager.HandManager.HandCoordRangeX;
                topLeft.Y *= _kinectManager.HandManager.HandCoordRangeY;
                bottomRight.Y *= _kinectManager.HandManager.HandCoordRangeY;

                //these points now define the bounds of the button in kinect space
                this._kinectSpaceBounds = new Rect(topLeft, bottomRight);
            }
        }

        #endregion

        public void Click(int x, int y)
        {
            var browser = GetBrowser();
            if (browser != null)
            {
                browser.GetHost().SendMouseClickEvent(x, y, CefSharp.MouseButtonType.Left, false, 1, CefSharp.CefEventFlags.LeftMouseButton);
                browser.GetHost().SendMouseClickEvent(x, y, CefSharp.MouseButtonType.Left, true, 1, CefSharp.CefEventFlags.None);
            }
        }

        public void MouseMoved(int x, int y)
        {
            var browser = GetBrowser();
            if (browser != null)
            {
                browser.GetHost().SendMouseMoveEvent(x, y, false, CefSharp.CefEventFlags.LeftMouseButton);
            }
        }

        public void MouseWheel(int aPosX, int aPosY, int aX, int aY)
        {
            var browser = GetBrowser();

            if (browser != null)
            {
                browser.GetHost().SendMouseWheelEvent(aPosX, aPosY, deltaX: aX, deltaY: aY, modifiers: CefSharp.CefEventFlags.None);
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
                        MouseMoved(mouseX, mouseY);
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

                        int mouseX = (int)(System.Windows.SystemParameters.PrimaryScreenWidth * aEvent.HandPosition.X);
                        int mouseY = (int)(System.Windows.SystemParameters.PrimaryScreenHeight * aEvent.HandPosition.Y);

                        Dispatcher.InvokeAsync((Action)delegate()
                        {
                            MouseWheel(mouseX, mouseY, wheelX, wheelY);
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
