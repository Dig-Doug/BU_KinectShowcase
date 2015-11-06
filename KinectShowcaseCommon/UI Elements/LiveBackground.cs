using KinectShowcaseCommon.Helpers;
using KinectShowcaseCommon.Kinect_Processing;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectShowcaseCommon.UI_Elements
{
    public class LiveBackground : GaussianBackground
    {
        public enum BackgroundMode
        {
            Color = 0,
            Infrared = 1,
            BodyIndex = 2,
        }
        public static int BACKGROUND_MODE_COUNT = 3;
        private const float BACKGROUND_SWITCH_TIME = 1000000000;//1.5f * 60.0f;

        private KinectManager _kinectManager = null;

        private BackgroundMode _currentMode = BackgroundMode.Color;
        private ColorFrameReader _colorFrameReader = null;
        private InfraredFrameReader _infraredFrameReader = null;
        private BodyIndexFrameReader _bodyIndexFrameReader = null;
        private bool _needColor, _needInfrared, _needBodyIndex;
        private long _frameCount = 0;
        private DateTime _lastBackgroundSwitchTime = DateTime.Now;

        private const int BytesPerPixel = 4;
        private static readonly uint[] BodyColor =
        {
            0x0000FF00,
            0x00FF0000,
            0xFFFF4000,
            0x40FFFF00,
            0xFF40FF00,
            0xFF808000,
        };
        private uint[] bodyIndexPixels = null;
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;
        private const float InfraredSourceScale = 0.75f;
        private const float InfraredOutputValueMinimum = 0.01f;
        private const float InfraredOutputValueMaximum = 1.0f;

        public bool ChangeRandomly { get; set; }

        #region Lifecycle

        public LiveBackground()
            : base()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _kinectManager = KinectManager.Default;

                // open the reader for the color frames
                this._colorFrameReader = _kinectManager.KinectSensor.ColorFrameSource.OpenReader();
                this._infraredFrameReader = _kinectManager.KinectSensor.InfraredFrameSource.OpenReader();
                this._bodyIndexFrameReader = _kinectManager.KinectSensor.BodyIndexFrameSource.OpenReader();
                FrameDescription bodyDescription = this.GetFrameDescriptionForMode(BackgroundMode.BodyIndex);
                this.bodyIndexPixels = new uint[bodyDescription.Width * bodyDescription.Height];

                // wire handler for frame arrival
                this._colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
                this._infraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;
                this._bodyIndexFrameReader.FrameArrived += this.Reader_BodyIndexFrameArrived;

                this._needColor = true;
            }
        }

        ~LiveBackground()
        {
            if (this._colorFrameReader != null)
            {
                this._colorFrameReader.Dispose();
                this._colorFrameReader = null;
            }

            if (this._infraredFrameReader != null)
            {
                this._infraredFrameReader.Dispose();
                this._infraredFrameReader = null;
            }

            if (this._bodyIndexFrameReader != null)
            {
                this._bodyIndexFrameReader.Dispose();
                this._bodyIndexFrameReader = null;
            }
        }

        #endregion

        #region Mode Changes

        public void ChangeModeRandom()
        {
                    //choose a random one
            int toChooseFrom = UI_Elements.LiveBackground.BACKGROUND_MODE_COUNT;
            Random rand = new Random();
            int nextType = rand.Next(toChooseFrom);
            //switch the type
            this.SetMode((BackgroundMode)nextType);
    }

        public void SetMode(BackgroundMode aMode)
        {
            if (aMode != _currentMode)
            {
                //detach 
                if (_currentMode == BackgroundMode.Color)
                {
                    _needColor = false;
                }
                else if (_currentMode == BackgroundMode.Infrared)
                {
                    _needInfrared = false;
                }
                else if (_currentMode == BackgroundMode.BodyIndex)
                {
                    _needBodyIndex = false;
                }
                else
                {
                    Debug.WriteLine("LiveBackground - ERROR - Unknown background mode");
                }

                _currentMode = aMode;
                if (_currentMode == BackgroundMode.Color)
                {
                    _needColor = true;
                }
                else if (_currentMode == BackgroundMode.Infrared)
                {
                    _needInfrared = true;
                }
                else if (_currentMode == BackgroundMode.BodyIndex)
                {
                    _needBodyIndex = true;
                }
                else
                {
                    Debug.WriteLine("LiveBackground - ERROR - Unknown background mode");
                }
            }
        }

        private FrameDescription GetFrameDescriptionForMode(BackgroundMode aMode)
        {
            FrameDescription result = null;

            if (aMode == BackgroundMode.Color)
            {
                result = _kinectManager.KinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            }
            else if (aMode == BackgroundMode.Infrared)
            {
                result = _kinectManager.KinectSensor.InfraredFrameSource.FrameDescription;
            }
            else if (aMode == BackgroundMode.BodyIndex)
            {
                result = _kinectManager.KinectSensor.BodyIndexFrameSource.FrameDescription;
            }
            else
            {
                Debug.WriteLine("LiveBackground - ERROR - Unknown background mode for get frame description");
            }

            return result;
        }

        #endregion

        #region Frame Arrived Callbacks

        private void Frame_Update()
        {
            //check if we've passed the timeout limit
            if ((DateTime.Now - _lastBackgroundSwitchTime).TotalSeconds >= BACKGROUND_SWITCH_TIME)
            {
                //reset to the home screen
                Debug.WriteLine("LiveBackground - LOG - Switching background");
                this.ChangeModeRandom();
                _lastBackgroundSwitchTime = DateTime.Now;
            }
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            Frame_Update();

            if (_needColor)
            {
                //lowers fps to improve performance
                if (_frameCount % 2 == 0)
                {
                    // ColorFrame is IDisposable
                    using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
                    {
                        if (colorFrame != null)
                        {
                            FrameDescription frameDescription = this.GetFrameDescriptionForMode(BackgroundMode.Color);

                            using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                            {
                                // verify data and write the new color frame data to the display bitmap
                                if (this._source == null || (frameDescription.Width != this._source.PixelWidth) || (frameDescription.Height != this._source.PixelHeight))
                                {
                                    this._source = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                                }

                                this._source.Lock();

                                colorFrame.CopyConvertedFrameDataToIntPtr(this._source.BackBuffer, (uint)(frameDescription.Width * frameDescription.Height * 4), ColorImageFormat.Bgra);
                                this._source.AddDirtyRect(new Int32Rect(0, 0, this._source.PixelWidth, this._source.PixelHeight));

                                _gaussianSource = this._source.Resize(frameDescription.Width / 4, frameDescription.Height / 4, WriteableBitmapExtensions.Interpolation.Bilinear);
                                _gaussianSource = _gaussianSource.Convolute(WriteableBitmapExtensions.KernelGaussianBlur5x5);

                                this.Update();

                                this._source.Unlock();

                            }
                        }
                    }
                }
                _frameCount++;
            }
        }

        private void Reader_BodyIndexFrameArrived(object sender, BodyIndexFrameArrivedEventArgs e)
        {
            if (_needBodyIndex)
            {
                //lowers fps to improve performance
                if (_frameCount % 2 == 0)
                {
                    // ColorFrame is IDisposable
                    using (BodyIndexFrame frame = e.FrameReference.AcquireFrame())
                    {
                        if (frame != null)
                        {
                            FrameDescription frameDescription = this.GetFrameDescriptionForMode(BackgroundMode.BodyIndex);

                            using (KinectBuffer buffer = frame.LockImageBuffer())
                            {
                                // verify data and write the new color frame data to the display bitmap
                                if (this._source == null || (frameDescription.Width != this._source.PixelWidth) || (frameDescription.Height != this._source.PixelHeight))
                                {
                                    this._source = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                                }

                                this._source.Lock();

                                this.ProcessBodyIndexFrameData(buffer.UnderlyingBuffer, buffer.Size);

                                this._source.WritePixels(new Int32Rect(0, 0, this._source.PixelWidth, this._source.PixelHeight), this.bodyIndexPixels, this._source.PixelWidth * (int)BytesPerPixel, 0);
                                _gaussianSource = this._source;

                                this.Update();

                                this._source.Unlock();
                            }
                        }
                    }
                }
                _frameCount++;
            }
        }

        private unsafe void ProcessBodyIndexFrameData(IntPtr bodyIndexFrameData, uint bodyIndexFrameDataSize)
        {
            byte* frameData = (byte*)bodyIndexFrameData;

            // convert body index to a visual representation
            for (int i = 0; i < (int)bodyIndexFrameDataSize; ++i)
            {
                // the BodyColor array has been sized to match
                // BodyFrameSource.BodyCount
                if (frameData[i] < BodyColor.Length)
                {
                    // this pixel is part of a player,
                    // display the appropriate color
                    this.bodyIndexPixels[i] = BodyColor[frameData[i]];
                }
                else
                {
                    // this pixel is not part of a player
                    // display black
                    this.bodyIndexPixels[i] = 0x00000000;
                }
            }
        }

        private void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            if (_needInfrared)
            {
                //lowers fps to improve performance
                if (_frameCount % 2 == 0)
                {
                    // ColorFrame is IDisposable
                    using (InfraredFrame frame = e.FrameReference.AcquireFrame())
                    {
                        if (frame != null)
                        {
                            FrameDescription frameDescription = this.GetFrameDescriptionForMode(BackgroundMode.Infrared);

                            using (KinectBuffer buffer = frame.LockImageBuffer())
                            {
                                // verify data and write the new color frame data to the display bitmap
                                if (this._source == null || (frameDescription.Width != this._source.PixelWidth) || (frameDescription.Height != this._source.PixelHeight) || this._source.Format != PixelFormats.Gray32Float)
                                {
                                    this._source = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Gray32Float, null);
                                }

                                this._source.Lock();

                                this.ProcessInfraredFrameData(buffer.UnderlyingBuffer, buffer.Size);

                                _gaussianSource = this._source;

                                this.Update();

                                this._source.Unlock();

                            }
                        }
                    }
                }
                _frameCount++;
            }
        }

        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            FrameDescription frameDescription = this.GetFrameDescriptionForMode(BackgroundMode.Infrared);

            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // lock the target bitmap
            this._source.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)this._source.BackBuffer;

            // process the infrared data
            for (int i = 0; i < (int)(infraredFrameDataSize / frameDescription.BytesPerPixel); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            // mark the entire bitmap as needing to be drawn
            this._source.AddDirtyRect(new Int32Rect(0, 0, this._source.PixelWidth, this._source.PixelHeight));

            // unlock the bitmap
            this._source.Unlock();
        }

        #endregion

    }
}
