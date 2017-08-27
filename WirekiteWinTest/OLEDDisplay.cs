using Codecrete.Wirekite.Device;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Codecrete.Wirekite.Test.UI
{
    /// <summary>
    /// OLED display with SH1306 or SH1106 chip and I2C communication
    /// </summary>
    public class OLEDDisplay
    {
        private const byte SetContrast = 0x81;
        private const byte OutputRAMToDisplay = 0xA4;
        private const byte SetDisplayOn = 0xA5;
        private const byte SetNormalDisplay = 0xA6;
        private const byte SetInvertedDisplay = 0xA7;
        private const byte DisplayOff = 0xAE;
        private const byte DisplayOn = 0xAF;
        private const byte SetDisplayOffset = 0xD3;
        private const byte SetComPin = 0xDA;
        private const byte SetVCOMH = 0xDB;
        private const byte SetClockDivideRatio = 0xD5;
        private const byte SetPrecharge = 0xD9;
        private const byte SetMultiplexRatio = 0xA8;
        private const byte SetColumnAddressLow = 0x00;
        private const byte SetColumnAddressHigh = 0x10;
        private const byte SetPageAddress = 0xb0;
        private const byte SetStartLineBase = 0x40;
        private const byte PageAddressingMode = 0x20;
        private const byte ScanDirectionIncreasing = 0xC0;
        private const byte ScanDirectionDecreasing = 0xC8;
        private const byte SegmentRampBase = 0xA0;
        private const byte ChargePump = 0x8D;
        private const byte DeactivateScroll = 0x2E;

        private WirekiteDevice device;
        private UInt16 i2cPort;
        private bool releasePort;
        private bool isInitialized;
        private int offset;

        /// <summary>
        /// I2C slave address
        /// </summary>
        public UInt16 DisplayAddress = 0x3c;

        /// <summary>
        /// Display width in pixels
        /// </summary>
        public int Width = 128;

        /// <summary>
        /// Display height in pixels
        /// </summary>
        public int Height = 64;

        /// <summary>
        /// Horizontal display offset (in pixel).
        /// </summary>
        /// <remarks>
        /// Use 0 for SH1306 chip, 2 for SH1106 chip.
        /// </remarks>
        public int DisplayOffset = 0;

        private Bitmap bitmap;


        public OLEDDisplay(WirekiteDevice device, I2CPins i2cPins)
        {
            this.device = device;
            i2cPort = device.ConfigureI2CMaster(i2cPins, 400000);
            releasePort = true;
        }


        public OLEDDisplay(WirekiteDevice device, UInt16 i2cPort)
        {
            this.device = device;
            this.i2cPort = i2cPort;
            releasePort = false;
        }


        ~OLEDDisplay()
        {
            if (releasePort)
                device.ReleaseI2CPort(i2cPort);
        }


        private void InitSensor()
        {
            // Init sequence
            byte[] initSequence = {
                0x80, DisplayOff,
                0x80, SetClockDivideRatio, 0x80, 0x80,
                0x80, SetMultiplexRatio, 0x80, 0x3f,
                0x80, SetDisplayOffset, 0x80, 0x0,
                0x80, SetStartLineBase + 0,
                0x80, ChargePump, 0x80, 0x14,
                0x80, PageAddressingMode, 0x80, 0x00,
                0x80, SegmentRampBase + 0x1,
                0x80, ScanDirectionDecreasing,
                0x80, SetComPin, 0x80, 0x12,
                0x80, SetContrast, 0x80, 0xcf,
                0x80, SetPrecharge, 0x80, 0xF1,
                0x80, SetVCOMH, 0x80, 0x40,
                0x80, DeactivateScroll,
                0x80, OutputRAMToDisplay,
                0x80, SetNormalDisplay,
                0x80, DisplayOn
            };
            int numBytesSent = device.SendOnI2CPort(i2cPort, initSequence, DisplayAddress);
            if (numBytesSent != initSequence.Length)
                throw new Exception("Initialization of OLED display failed");

            bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppRgb);
            bitmap.SetResolution(72, 72);
        }


        private float stringWidth = 0;


        private void Draw(int offset)
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Black);
                // Might only work in Windows 10
                string text = "\uE007 \uE209 \uE706 \uE774 \uE83D \uE928 \uEA8E \ue007 \uE209";
                Font font = new Font("Segoe MDL2 Assets", 64);
                g.MeasureString(text, font);
                g.DrawString(text, font, new SolidBrush(Color.White), new PointF(-offset, 0));

                if (stringWidth == 0)
                {
                    SizeF size = g.MeasureString("\uE007 \uE209 \uE706 \uE774 \uE83D \uE928 \uEA8E", font);
                    stringWidth = size.Width - 64 / 6.0f - 6;
                }
            }
        }


        public void Update()
        {
            if (!isInitialized)
            {
                InitSensor();
                isInitialized = true;
            }

            Draw(offset);
            offset++;
            if (offset >= stringWidth)
                offset = 0;

            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            int stride = Math.Abs(bitmapData.Stride) / 4;
            int[] pixelData = new int[stride * Height];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelData, 0, stride * Height);
            bitmap.UnlockBits(bitmapData);

            byte[] bwPixels = BurkesDither(pixelData, stride, Width);

            byte[] tile = new byte[Width + 7];
            for (int page = 0; page < Height / 8; page++)
            {
                tile[0] = 0x80;
                tile[1] = (byte)(SetPageAddress + page);
                tile[2] = 0x80;
                tile[3] = (byte)(SetColumnAddressLow | (DisplayOffset & 0x0f));
                tile[4] = 0x80;
                tile[5] = (byte)(SetColumnAddressHigh | ((DisplayOffset >> 4) & 0x0f));
                tile[6] = 0x40;

                int index = page * 8 * stride;
                for (int i = 0; i < Width; i++)
                {
                    byte b = 0;
                    byte bit = 1;
                    int p = index + i;
                    for (int j = 0; j < 8; j++)
                    {
                        if (bwPixels[p] != 0)
                            b |= bit;
                        bit <<= 1;
                        p += stride;
                    }

                    tile[i + 7] = b;
                }

                device.SubmitOnI2CPort(i2cPort, tile, DisplayAddress);
            }

            /*
            // Just for the fun of it: read back some of the data
            let cmd: [UInt8] = [
                0x80, OLEDDisplay.SetPageAddress + UInt8(4),
                0x80, OLEDDisplay.SetColumnAddressLow | UInt8(DisplayOffset & 0x0f),
                0x80, OLEDDisplay.SetColumnAddressHigh | UInt8((DisplayOffset >> 4) & 0x0f),
                0x40
            ]
            let data1 = Data(bytes: cmd)
            let response = device!.sendAndRequest(onI2CPort: i2cPort, data: data1, toSlave: displayAddress, receiveLength: UInt16(Width))!
            let responseBytes = [UInt8](response)
            */

        }


        /// <summary>
        /// Apply Burke's dithering to the specified grayscale pixelmap
        /// </summary>
        /// <param name="pixelData">RGB pixelmap as an array of pixel bytes</param>
        /// <param name="stride">pixel data stride (length of row)</param>
        /// <param name="width">pixel data width</param>
        /// <returns></returns>
        static byte[] BurkesDither(int[] pixelData, int stride, int width)
        {
            int height = pixelData.Length / stride;
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
                    int r = pixel & 0xff;
                    int g = (pixel >> 8) & 0xff;
                    int b = (pixel >> 16) & 0xff;
                    int gs = (int)(r * 0.299 + g * 0.587 + b * 0.114 + 0.5); // target value

                    int bw = gs >= 128 ? 255 : 0; // black/white value
                    int err = gs - bw; // error
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

                srcIndex += stride;
            }

            return result;
        }
        

    }
}
