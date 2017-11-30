/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */



using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Codecrete.Wirekite.Test.UI
{
    public enum GraphicsFormat
    {
        Grayscale,
        BlackAndWhiteDithered,
        RGB565,
        RGB565Rotated180
    };


    public class GraphicsBuffer : IDisposable
    {
        private Bitmap bitmap;
        private int Width { get; }
        private int Height { get; }

        public GraphicsBuffer(int width, int height, bool isColor)
        {
            Width = width;
            Height = height;
            bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);
            bitmap.SetResolution(72, 72);
        }


        public delegate void DrawCallback(Graphics g);


        public byte[] Draw(DrawCallback callback, GraphicsFormat format)
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                callback(g);
            }

            switch (format)
            {
                case GraphicsFormat.Grayscale:
                    return ConvertToGrayscale();
                case GraphicsFormat.BlackAndWhiteDithered:
                    return ConvertToBlackAndWhiteDithered();
                case GraphicsFormat.RGB565:
                    return ConvertToRGB565();
                default:
                    return ConvertToRGB565Rotated180();
            }
        }


        private byte[] ConvertToGrayscale()
        {
            using (Bitmap clone = new Bitmap(Width, Height, PixelFormat.Format32bppRgb))
            {
                using (Graphics g = Graphics.FromImage(clone))
                {
                    ColorMatrix colorMatrix = new ColorMatrix(
                        new float[][]
                        {
                            new float[] {.3f, .3f, .3f, 0, 0},
                            new float[] {.59f, .59f, .59f, 0, 0},
                            new float[] {.11f, .11f, .11f, 0, 0},
                            new float[] {0, 0, 0, 1, 0},
                            new float[] {0, 0, 0, 0, 1}
                        }
                    );
                    ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(colorMatrix);

                    g.DrawImage(bitmap, new Rectangle(0, 0, Width, Height), 0, 0, Width, Height, GraphicsUnit.Pixel, attributes);
                }

                return ConvertToCompactGrayscale(GetIntPixelData(clone));
            }
        }


        private byte[] ConvertToBlackAndWhiteDithered()
        {
            return BurkesDither(ConvertToGrayscale(), Width);
        }


        private byte[] ConvertToRGB565()
        {
            using (Bitmap clone = new Bitmap(Width, Height, PixelFormat.Format16bppRgb565))
            {
                using (Graphics g = Graphics.FromImage(clone))
                {
                    g.DrawImage(bitmap, new Rectangle(0, 0, Width, Height));
                }

                return GetBytePixelData(clone);
            }
        }


        private byte[] ConvertToRGB565Rotated180()
        {
            using (Bitmap clone = new Bitmap(Width, Height, PixelFormat.Format16bppRgb565))
            {
                using (Graphics g = Graphics.FromImage(clone))
                {
                    using (Image rotated = Rotate180(bitmap))
                    {
                        g.DrawImage(rotated, new Rectangle(0, 0, Width, Height));
                    }
                }

                return GetBytePixelData(clone);
            }
        }


        private static int[] GetIntPixelData(Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int stride = Math.Abs(bitmapData.Stride) / 4;
            int[] pixelData = new int[stride * bitmap.Height];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelData, 0, stride * bitmap.Height);
            bitmap.UnlockBits(bitmapData);
            return pixelData;
        }


        private static short[] GetWordPixelData(Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int stride = Math.Abs(bitmapData.Stride) / 2;
            short[] pixelData = new short[stride * bitmap.Height];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelData, 0, stride * bitmap.Height);
            bitmap.UnlockBits(bitmapData);
            return pixelData;
        }


        private static byte[] GetBytePixelData(Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int stride = Math.Abs(bitmapData.Stride);
            byte[] pixelData = new byte[stride * bitmap.Height];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelData, 0, stride * bitmap.Height);
            bitmap.UnlockBits(bitmapData);
            return pixelData;
        }


        private static byte[] ConvertToCompactGrayscale(int[] grayscaleData)
        {
            byte[] pixelData = new byte[grayscaleData.Length];
            int i = 0;

            foreach (int pixel in grayscaleData)
            {
                pixelData[i] = (byte)(pixel & 0xff);
                i++;
            }

            return pixelData;
        }


        private static Image Rotate180(Image image)
        {
            Bitmap clone = new Bitmap(image.Width, image.Height, image.PixelFormat);
            using (Graphics g = Graphics.FromImage(clone))
            {
                g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));
            }

            clone.RotateFlip(RotateFlipType.Rotate180FlipNone);
            return clone;
        }


        /// <summary>
        /// Apply Burke's dithering to the specified grayscale pixelmap
        /// </summary>
        /// <param name="pixelData">RGB pixelmap as an array of pixel bytes</param>
        /// <param name="width">pixel data width</param>
        /// <returns></returns>
        static public byte[] BurkesDither(byte[] pixelData, int width)
        {
            int height = pixelData.Length / width;
            byte[] result = new byte[width * height];
            int[] currLine;
            int[] nextLine = new int[width];

            int p = 0;
            int srcIndex = 0;
            for (int y = 0; y < height; y++)
            {
                int sp = srcIndex;
                currLine = nextLine;
                nextLine = new int[width];

                for (int x = 0; x < width; x++)
                {
                    int pixel = pixelData[sp];
                    int bw = pixel >= 128 ? 255 : 0; // black/white value
                    int err = pixel - bw; // error
                    result[p] = (byte)bw;

                    // distribute error
                    nextLine[x] += err >> 2;
                    if (x > 0)
                        nextLine[x - 1] += err >> 3;
                    if (x > 1)
                        nextLine[x - 2] += err >> 4;
                    if (x < width - 1)
                    {
                        currLine[x + 1] += err >> 2;
                        nextLine[x + 1] += err >> 3;
                    }
                    if (x < width - 2)
                    {
                        currLine[x + 2] += err >> 3;
                        nextLine[x + 2] += err >> 4;
                    }

                    p++;
                    sp++;
                }

                srcIndex += width;
            }

            return result;
        }


        #region IDisposable Support
        private bool _isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (bitmap != null)
                        bitmap.Dispose();
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
