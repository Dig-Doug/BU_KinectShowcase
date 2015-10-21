using KinectShowcaseCommon.Helpers;
using KinectShowcaseCommon.Kinect_Processing;
using KinectShowcaseCommon.ProcessHandling;
using KinectShowcaseCommon.UI_Elements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace KinectShowcaseGameTemplate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, KinectManager.StateListener
    {
        public SkeletonView SkeletonView { get; private set; }
        private KinectManager kinectManager;

        public MainWindow()
        {
            InitializeComponent();

            this.kinectManager = KinectManager.Default;

            //setup the cursor with images
            Image[] cursorImages = new Image[3];
            Image cursorImage0 = new Image();
            cursorImage0.Source = ImageExtensions.ToBitmapSource(KinectShowcaseGameTemplate.Properties.Resources.hand1);
            cursorImages[0] = cursorImage0;
            Image cursorImage1 = new Image();
            cursorImage1.Source = ImageExtensions.ToBitmapSource(KinectShowcaseGameTemplate.Properties.Resources.hand2);
            cursorImages[1] = cursorImage1;
            Image cursorImage2 = new Image();
            cursorImage2.Source = ImageExtensions.ToBitmapSource(KinectShowcaseGameTemplate.Properties.Resources.hand3);
            cursorImages[2] = cursorImage2;

            this.cursorView.SetCursorImages(cursorImages);

            this.kinectManager.HandManager.Cursor = this.cursorView;
            this.kinectManager.AddStateListener(this);
            //this.kinectRegion.KinectSensor = this.kinectManager.KinectSensor;

            Point center = new Point(0.5f, 0.5f);
            this.cursorView.SetCursorPosition(center);

            this.SkeletonView = skeletonView;
            this.SkeletonView.ShouldDrawHeadJoint = false;

            _mainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            _mainFrame.Navigate(new MainPage());
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (!this.kinectManager.KinectSensor.IsAvailable)
            {
                BitmapSource source = ImageExtensions.ToBitmapSource(KinectShowcaseGameTemplate.Properties.Resources.background);
                // Calculate stride of source
                int stride = source.PixelWidth * (source.Format.BitsPerPixel / 8);
                // Create data array to hold source pixel data
                byte[] data = new byte[stride * source.PixelHeight];
                // Copy source image pixels to the data array
                source.CopyPixels(data, stride, 0);
                // Create WriteableBitmap to copy the pixel data to.      
                WriteableBitmap target = new WriteableBitmap(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, source.Format, null);
                // Write the pixel data to the WriteableBitmap.
                target.WritePixels(new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight), data, stride, 0);

                skeletonView.Source = target;
                skeletonView.Update();
            }
            else
            {
                skeletonView.Update();
            }
            this.SkeletonView.SetKinectManager(this.kinectManager);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            SystemCanary.Default.SystemDidRecieveInteraction();
        }

        public void KinectManagerDidUpdateState(KinectManager aManager, bool aIsKinectActive)
        {
            Dispatcher.InvokeAsync((Action)delegate()
            {
                if (aIsKinectActive)
                {
                    this.stateMessage.Opacity = 0.0f;
                }
                else
                {
                    //this.stateMessage.Opacity = 1.0f;
                }
            });
        }
    }
}
