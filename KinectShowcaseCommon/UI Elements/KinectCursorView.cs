using Microsoft.Kinect;
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
using System.Windows.Media.Animation;
using KinectShowcaseCommon.Helpers;

namespace KinectShowcaseCommon.UI_Elements
{
    public class KinectCursorView : Canvas
    {
        public enum CursorState
        {
            OpenHand,
            ClosedHand,
        }

        private const float ANIMATION_LENGTH = 0.5f;
        private const float PERCENT_CURSOR_SIZE = 0.05f;

        private CursorState _cursorState = CursorState.ClosedHand;
        private Point _cursorPosition;
        private Point _cursorPositionDraw;
        private float _selectionCiclePercent = 0.5f;
        private DrawingGroup _drawingGroup = null;
        private Storyboard _openToCloseHandAnimation, _closeToOpenHandAnimation;
        private Image[] _cursorImages = null;

        public KinectCursorView()
        {
            _drawingGroup = new DrawingGroup();

            this.SetCursorPosition(new Microsoft.Kinect.PointF());
            this.SetCursorState(CursorState.OpenHand);
        }

        public void SetCursorImages(Image[] aImages)
        {
            _cursorImages = new Image[aImages.Length];
            for (int i = 0; i < aImages.Length; i++)
            {
                _cursorImages[i] = aImages[i];
                this.Children.Add(_cursorImages[i]);
                if (i != 0)
                {
                    _cursorImages[i].Opacity = 0.0f;
                }
                else
                {
                    _cursorImages[i].Opacity = 1.0f;
                }
            }

            this.CreateAnimations();
        }

        private void CreateAnimations()
        {
            //create the storyboards
            _openToCloseHandAnimation = new Storyboard();
            float imageDuration = ANIMATION_LENGTH / _cursorImages.Length;
            Duration duration = new Duration(TimeSpan.FromSeconds(imageDuration));
            for (int i = 0; i < _cursorImages.Length; i++)
            {
                Image currentImage = _cursorImages[i];

                DoubleAnimation duringAnimator = new DoubleAnimation();
                duringAnimator.Duration = duration;
                duringAnimator.BeginTime = TimeSpan.FromSeconds(imageDuration * i);
                duringAnimator.From = 1.0f;
                duringAnimator.To = 1.0f;

                Storyboard.SetTarget(duringAnimator, currentImage);
                Storyboard.SetTargetProperty(duringAnimator, new PropertyPath("Opacity"));
                _openToCloseHandAnimation.Children.Add(duringAnimator);


                if (i != _cursorImages.Length - 1)
                {
                    DoubleAnimation afterAnimator = new DoubleAnimation();
                    afterAnimator.Duration = duration;
                    afterAnimator.BeginTime = TimeSpan.FromSeconds(imageDuration * (i + 1));
                    afterAnimator.From = 0.0f;
                    afterAnimator.To = 0.0f;

                    Storyboard.SetTarget(afterAnimator, currentImage);
                    Storyboard.SetTargetProperty(afterAnimator, new PropertyPath("Opacity"));
                    _openToCloseHandAnimation.Children.Add(afterAnimator);
                }
            }

            _closeToOpenHandAnimation = new Storyboard();
            for (int i = 0; i < _cursorImages.Length; i++)
            {
                Image currentImage = _cursorImages[_cursorImages.Length - i - 1];

                DoubleAnimation duringAnimator = new DoubleAnimation();
                duringAnimator.Duration = duration;
                duringAnimator.BeginTime = TimeSpan.FromSeconds(imageDuration * i);
                duringAnimator.From = 1.0f;
                duringAnimator.To = 1.0f;

                Storyboard.SetTarget(duringAnimator, currentImage);
                Storyboard.SetTargetProperty(duringAnimator, new PropertyPath("Opacity"));
                _closeToOpenHandAnimation.Children.Add(duringAnimator);

                if (i != _cursorImages.Length - 1)
                {
                    DoubleAnimation afterAnimator = new DoubleAnimation();
                    afterAnimator.Duration = duration;
                    afterAnimator.BeginTime = TimeSpan.FromSeconds(imageDuration * (i + 1));
                    afterAnimator.From = 0.0f;
                    afterAnimator.To = 0.0f;

                    Storyboard.SetTarget(afterAnimator, currentImage);
                    Storyboard.SetTargetProperty(afterAnimator, new PropertyPath("Opacity"));
                    _closeToOpenHandAnimation.Children.Add(afterAnimator);
                }
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (_cursorImages != null)
            {
                foreach (Image currentImage in _cursorImages)
                {
                    currentImage.Width = this.ActualWidth * PERCENT_CURSOR_SIZE;
                    currentImage.Height = this.ActualHeight * PERCENT_CURSOR_SIZE;
                }

                SetCursorPosition(_cursorPosition);
            }
        }

        public void SetCursorPosition(PointF aLoc)
        {
            this.SetCursorPosition(new Point(aLoc.X, aLoc.Y));
        }

        public void SetCursorPosition(Point aLoc)
        {
            Dispatcher.InvokeAsync((Action)delegate()
            {
                this._cursorPosition.X = aLoc.X;
                this._cursorPosition.Y = aLoc.Y;

                //keeps the cursor on screen
                bool handIsOffscreen = false;
                if (_cursorPosition.X < 0)
                {
                    _cursorPosition.X = 0;
                    handIsOffscreen = true;
                }
                else if (_cursorPosition.X > 1.0)
                {
                    _cursorPosition.X = 1.0f;
                    handIsOffscreen = true;
                }
                if (_cursorPosition.Y < 0)
                {
                    _cursorPosition.Y = 0;
                    handIsOffscreen = true;
                }
                else if (_cursorPosition.Y > 1)
                {
                    _cursorPosition.Y = 1;
                    handIsOffscreen = true;
                }

                if (handIsOffscreen)
                {

                }

                this._cursorPositionDraw.X = this.ActualWidth * this._cursorPosition.X;
                this._cursorPositionDraw.Y = this.ActualHeight * this._cursorPosition.Y;

                Point topLeftCorner = new Point(_cursorPositionDraw.X - _cursorImages[0].Width / 2, _cursorPositionDraw.Y - _cursorImages[0].Height / 2);
                foreach (Image currentImage in _cursorImages)
                {
                    Canvas.SetLeft(currentImage, topLeftCorner.X);
                    Canvas.SetTop(currentImage, topLeftCorner.Y);
                }
            });
        }

        public void SetCursorState(CursorState aState)
        {
            Dispatcher.InvokeAsync((Action)delegate()
            {
                if (_cursorState != aState)
                {
                    if (_cursorState == CursorState.OpenHand)
                    {
                        _openToCloseHandAnimation.Begin();
                    }
                    else
                    {
                        _closeToOpenHandAnimation.Begin();
                    }
                }

                _cursorState = aState;
            });
        }

        public void SetSelectionAmount(float aPercent)
        {
            Dispatcher.InvokeAsync((Action)delegate()
            {
                _selectionCiclePercent = aPercent;

                this.Draw();
            });
        }

        public void RefreshHandedness(bool aIsLeftHand)
        {
            Dispatcher.InvokeAsync((Action)delegate()
            {
                foreach (Image currentImage in this._cursorImages)
                {
                    if (aIsLeftHand)
                    {
                        currentImage.RenderTransform = new ScaleTransform(-1.0, 1.0);
                    }
                    else
                    {
                        currentImage.RenderTransform = new ScaleTransform(1.0, 1.0);
                    }
                    currentImage.RenderTransformOrigin = new Point(0.5f, 0.5f);
                }
            });
        }

        public void Draw()
        {
            using (DrawingContext dc = this._drawingGroup.Open())
            {
                // prevent drawing outside of our render area
                this._drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.ActualWidth, this.ActualHeight));
            }
        }
    }
}
