using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace KinectShowcaseCommon.Helpers
{
    /// <summary>
    /// WriteableBitmap extensions to make the image darker.
    /// </summary>
    public static class WriteableBitmapDarkenExtension
    {
        /// <summary>
        /// Darkens the specified bitmap.
        /// </summary>
        /// <param name="target">The target bitmap.</param>
        /// <param name="amount">The 0..1 range amount to darken by where 0 makes the bitmap completely black and 1 does nothing.</param>
        /// <returns></returns>
        public static WriteableBitmap Darken(this WriteableBitmap target, double amount)
        {
            target.Lock();

            IntPtr ptr = target.BackBuffer;

            unsafe
            {
                uint* pixels = (uint*)ptr.ToPointer();
                for (int x = 0; x < target.Width; x++)
                {
                    for (int y = 0; y < target.Height; y++)
                    {
                        long index = y * (long)target.Width + x;
                        uint pixel = pixels[index];
                        uint b = (pixel & 0x000000ff);
                        uint g = (pixel & 0x0000ff00) >> 8;
                        uint r = (pixel & 0x00ff0000) >> 16;
                        uint a = (pixel & 0xff000000) >> 24;

                        uint newPixel = (uint)(b * amount);
                        newPixel += (uint)(g * amount) << 8;
                        newPixel += (uint)(r * amount) << 16;
                        newPixel += 0xff000000;
                        pixels[index] = newPixel;
                    }
                }
            }

            target.Unlock();
            return target;
        }
    }
}
