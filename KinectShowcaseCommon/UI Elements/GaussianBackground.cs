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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectShowcaseCommon.UI_Elements
{
    public class GaussianBackground : Canvas
    {
        protected Image _nonGaussian = null;
        protected Image _gaussian = null;
        protected WriteableBitmap _source = null, _gaussianSource = null;
        protected double _scale = 1.0f, _scaleGaussian = 1.0f;
        protected float _percentStart;
        protected float _percentEnd;

        public float PercentStart
        {
            get
            {
                return _percentStart;
            }
            set
            {
                _percentStart = value;
                this.UpdateImageLayouts();
            }
        }
        public float PercentEnd
        {
            get
            {
                return _percentEnd;
            }
            set
            {
                _percentEnd = value;
                this.UpdateImageLayouts();
            }
        }

        public GaussianBackground()
            : base()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.Source = null;
                this.PercentStart = 0.0f;
                this.PercentEnd = 0.25f;

                this.Init();
            }
        }



        public BitmapSource Source
        {
            get { return (BitmapSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(object), typeof(GaussianBackground), new PropertyMetadata(null, new PropertyChangedCallback(SourceChanged)));

        private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as GaussianBackground;
            if (instance != null)
            {
                if (e.NewValue != null && e.NewValue is BitmapSource)
                {
                    WriteableBitmap setAs = null;
                    if (!(e.NewValue is WriteableBitmap))
                    {
                        setAs = new WriteableBitmap(e.NewValue as BitmapSource);
                    }
                    else
                    {
                        setAs = e.NewValue as WriteableBitmap;
                    }

                    instance._source = setAs;
                    if (setAs != null)
                        instance._gaussianSource = setAs.Convolute(WriteableBitmapExtensions.KernelGaussianBlur5x5);
                    instance.Update();
                }
                else
                {
                    instance._source = null;
                }
            }
        }



        private void Init()
        {
            //create the controls that will hold the blurred and non-blurred images
            _nonGaussian = new Image();
            this.Children.Add(_nonGaussian);
            _gaussian = new Image();
            this.Children.Add(_gaussian);

            /*
            //apply the blur affect to the image
            BlurEffect blur = new BlurEffect();
            blur.KernelType = KernelType.Gaussian;
            blur.Radius = _gaussianRadius;
            blur.RenderingBias = RenderingBias.Performance;
            _gaussian.Effect = blur;
             * */
        }

        public void SetPercents(float aStart, float aEnd)
        {
            _percentStart = aStart;
            _percentEnd = aEnd;
            this.UpdateImageLayouts();
        }

        public void Update()
        {
            this.UpdateImageLayouts();
            this.UpdateImageSource();
        }

        protected virtual void UpdateImageLayouts()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                if (_source != null)
                {
                    //figure out where the top left corner of the image controls should be
                    Point topLeftCornerSource = new Point();
                    topLeftCornerSource.X = this.Width / 2 - _source.Width;
                    topLeftCornerSource.Y = this.Height / 2 - _source.Height;
                    //move the controls to the correct locations
                    Canvas.SetLeft(_nonGaussian, topLeftCornerSource.X);
                    Canvas.SetTop(_nonGaussian, topLeftCornerSource.Y);

                    //get best scale
                    double scaleX = this.ActualWidth / _source.Width;
                    double scaleY = this.ActualHeight / _source.Height;
                    if (scaleX > scaleY)
                    {
                        _scale = scaleX;
                    }
                    else
                    {
                        _scale = scaleY;
                    }

                    //apply scale to the source
                    _nonGaussian.RenderTransform = new ScaleTransform(_scale, _scale);  
                }
                if (_gaussianSource != null)
                {
                    Point topLeftCornerGaussian = new Point();
                    topLeftCornerGaussian.X = this.Width / 2 - _gaussian.Width;
                    topLeftCornerGaussian.Y = this.Height / 2 - _gaussian.Height;
                    Canvas.SetLeft(_gaussian, topLeftCornerGaussian.X);
                    Canvas.SetTop(_gaussian, topLeftCornerGaussian.Y);

                    //get best scale
                    double scaleXG = this.ActualWidth / _gaussianSource.Width;
                    double scaleYG = this.ActualHeight / _gaussianSource.Height;
                    if (scaleXG > scaleYG)
                    {
                        _scaleGaussian = scaleXG;
                    }
                    else
                    {
                        _scaleGaussian = scaleYG;
                    }

                    _gaussian.RenderTransform = new ScaleTransform(_scaleGaussian, _scaleGaussian);

                    //figure out the clipping corners
                    Point clipTopLeft = new Point();
                    clipTopLeft.X = 0.0;
                    clipTopLeft.Y = this.ActualHeight * this.PercentStart / _scaleGaussian;
                    Point clipBottomRight = new Point();
                    clipBottomRight.X = this.ActualWidth / _scaleGaussian;
                    clipBottomRight.Y = this.ActualHeight * this.PercentEnd / _scaleGaussian;

                    //apply the clipping mask to the gaussian control
                    Rect rect = new Rect(clipTopLeft, clipBottomRight);
                    var maskGeometry = new RectangleGeometry(rect, 0.0, 0.0);
                    var maskDrawing = new GeometryDrawing(Brushes.Black, null, maskGeometry);
                    var maskBrush = new DrawingBrush
                    {
                        Drawing = maskDrawing,
                        Stretch = Stretch.None,
                        ViewboxUnits = BrushMappingMode.Absolute,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top
                    };
                    _gaussian.OpacityMask = maskBrush;
                }
            }
        }

        protected virtual void UpdateImageSource()
        {
            if (_source != null)
            {
                //set the source
                _nonGaussian.Source = _source;
                _gaussian.Source = _gaussianSource;
            }
        }
    }
}
