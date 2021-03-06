﻿using KinectShowcase.ViewModel;
using KinectShowcaseCommon.Helpers;
using KinectShowcaseCommon.Kinect_Processing;
using KinectShowcaseCommon.UI_Elements;
using Microsoft.Kinect;
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

namespace KinectShowcase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, KinectManager.StateListener
    {
        private KinectManager kinectManager;
        private Point DEBUG_currentKinectHandPos = new Point(0.5f, 0.5f);
        private bool DEBUG_handIsOpen = true;
        private bool backgroundInitialized = false;

        public MainWindow()
        {
            InitializeComponent();


            this.kinectManager = KinectManager.Default;

            //setup the cursor with images
            Image[] cursorImages = new Image[3];
            Image cursorImage0 = new Image();
            cursorImage0.Source = ImageExtensions.ToBitmapSource(KinectShowcase.Properties.Resources.hand1);
            cursorImages[0] = cursorImage0;
            Image cursorImage1 = new Image();
            cursorImage1.Source = ImageExtensions.ToBitmapSource(KinectShowcase.Properties.Resources.hand2);
            cursorImages[1] = cursorImage1;
            Image cursorImage2 = new Image();
            cursorImage2.Source = ImageExtensions.ToBitmapSource(KinectShowcase.Properties.Resources.hand3);
            cursorImages[2] = cursorImage2;

            this.cursorView.SetCursorImages(cursorImages);

            this.kinectManager.HandManager.Cursor = this.cursorView;
            this.kinectManager.AddStateListener(this);
            //this.kinectRegion.KinectSensor = this.kinectManager.KinectSensor;

            ViewModelLocator.Locator().ApplicationViewModel.SkeletonView = this.skeletonView;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (!this.kinectManager.KinectSensor.IsAvailable)
            {
                if (!backgroundInitialized)
                {
                    /*
                    backgroundInitialized = true;
                    BitmapSource source = ImageExtensions.ToBitmapSource(KinectShowcase.Properties.Resources.background);
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
                     * */
                }
            }
            else
            {
                skeletonView.Update();
            }
            ViewModelLocator.Locator().ApplicationViewModel.SkeletonView.SetKinectManager(this.kinectManager);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            const float increment = 0.01f;
            if (e.Key == Key.A) //left
            {
                DEBUG_currentKinectHandPos.X -= increment;
                this.kinectManager.HandManager.InjectNormalizedHandLocation(DEBUG_currentKinectHandPos);
            }
            else if (e.Key == Key.D) //right
            {
                DEBUG_currentKinectHandPos.X += increment;
                this.kinectManager.HandManager.InjectNormalizedHandLocation(DEBUG_currentKinectHandPos);
            }
            else if (e.Key == Key.W) //up
            {
                DEBUG_currentKinectHandPos.Y -= increment;
                this.kinectManager.HandManager.InjectNormalizedHandLocation(DEBUG_currentKinectHandPos);
            }
            else if (e.Key == Key.S) //down
            {
                DEBUG_currentKinectHandPos.Y += increment;
                this.kinectManager.HandManager.InjectNormalizedHandLocation(DEBUG_currentKinectHandPos);
            }
            else if (e.Key == Key.Space || e.Key == Key.Q) //toggle open close
            {
                DEBUG_handIsOpen = !DEBUG_handIsOpen;
                HandState state = (DEBUG_handIsOpen ? HandState.Open : HandState.Closed);
                this.kinectManager.HandManager.InjectHandStateChange(state);
            }
            else if (e.Key == Key.M)
            {
                Process currentProc = Process.GetCurrentProcess();
                long memoryUsed = currentProc.PrivateMemorySize64;
                Debug.WriteLine("Memory used: " + memoryUsed);
            }
        }

        public void KinectManagerDidUpdateState(KinectManager aManager, bool aIsKinectActive)
        {
            if (!aIsKinectActive)
            {
                //make stuff visibile
                ViewModelLocator.Locator().HomeViewModel.KinectManagerDidBeginTracking(null);
            }
        }
    }
}
