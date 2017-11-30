/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Codecrete.Wirekite.Device;
using System;


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
        private int i2cPort;
        private bool releasePort;
        private bool isInitialized;
        private GraphicsBuffer graphics;


        /// <summary>
        /// I2C slave address
        /// </summary>
        public int DisplayAddress = 0x3c;

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


        public OLEDDisplay(WirekiteDevice device, I2CPins i2cPins)
        {
            this.device = device;
            i2cPort = device.ConfigureI2CMaster(i2cPins, 400000);
            releasePort = true;
        }


        public OLEDDisplay(WirekiteDevice device, int i2cPort)
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

            graphics = new GraphicsBuffer(Width, Height, false);
        }


        public void ShowFrame(GraphicsBuffer.DrawCallback callback)
        {
            if (!isInitialized)
            {
                InitSensor();
                isInitialized = true;
            }

            byte[] pixelData = graphics.Draw(callback, GraphicsFormat.BlackAndWhiteDithered);

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

                int index = page * 8 * Width;
                for (int i = 0; i < Width; i++)
                {
                    byte b = 0;
                    byte bit = 1;
                    int p = index + i;
                    for (int j = 0; j < 8; j++)
                    {
                        if (pixelData[p] != 0)
                            b |= bit;
                        bit <<= 1;
                        p += Width;
                    }

                    tile[i + 7] = b;
                }

                device.SubmitOnI2CPort(i2cPort, tile, DisplayAddress);
            }

            /*
            // Just for the fun of it: read back some of the data
            byte[] cmd = new byte[] {
                0x80, SetPageAddress + 4,
                0x80, (byte)(SetColumnAddressLow | (DisplayOffset & 0x0f)),
                0x80, (byte)(SetColumnAddressHigh | ((DisplayOffset >> 4) & 0x0f)),
                0x40
            };
            byte[] pageData = device.SendAndRequestOnI2CPort(i2cPort, cmd, DisplayAddress, Width);
            */
        }       

    }
}
