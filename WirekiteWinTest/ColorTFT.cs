/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Codecrete.Wirekite.Device;
using System;
using System.Threading;


namespace Codecrete.Wirekite.Test.UI
{
    /// <summary>
    /// Color TFT display using the ST7735 chip and SPI communication
    /// </summary>
    public class ColorTFT
    {
        private const byte NOP = 0x00;
        private const byte SWRESET = 0x01;
        private const byte RDDID = 0x04;
        private const byte RDDST = 0x09;

        private const byte SLPIN = 0x10;
        private const byte SLPOUT = 0x11;
        private const byte PTLON = 0x12;
        private const byte NORON = 0x13;

        private const byte INVOFF = 0x20;
        private const byte INVON = 0x21;
        private const byte DISPOFF = 0x28;
        private const byte DISPON = 0x29;
        private const byte CASET = 0x2A;
        private const byte RASET = 0x2B;
        private const byte RAMWR = 0x2C;
        private const byte RAMRD = 0x2E;

        private const byte PTLAR = 0x30;
        private const byte COLMOD = 0x3A;
        private const byte MADCTL = 0x36;

        private const byte FRMCTR1 = 0xB1;
        private const byte FRMCTR2 = 0xB2;
        private const byte FRMCTR3 = 0xB3;
        private const byte INVCTR = 0xB4;
        private const byte DISSET5 = 0xB6;

        private const byte PWCTR1 = 0xC0;
        private const byte PWCTR2 = 0xC1;
        private const byte PWCTR3 = 0xC2;
        private const byte PWCTR4 = 0xC3;
        private const byte PWCTR5 = 0xC4;
        private const byte VMCTR1 = 0xC5;

        private const byte RDID1 = 0xDA;
        private const byte RDID2 = 0xDB;
        private const byte RDID3 = 0xDC;
        private const byte RDID4 = 0xDD;

        private const byte PWCTR6 = 0xFC;

        private const byte GMCTRP1 = 0xE0;
        private const byte GMCTRN1 = 0xE1;

        public int Width = 128;
        public int Height = 160;

        private WirekiteDevice device;
        private int spi;

        private int csPort;
        private int dcPort;
        private int resetPort;

        private bool isInitialized;
        private GraphicsBuffer graphics;


        public ColorTFT(WirekiteDevice device, int spiPort, int csPin, int dcPin, int resetPin)
        {
            this.device = device;
            spi = spiPort;
            csPort = device.ConfigureDigitalOutputPin(csPin, DigitalOutputPinAttributes.Default, true);
            dcPort = device.ConfigureDigitalOutputPin(dcPin, DigitalOutputPinAttributes.Default, true);
            resetPort = device.ConfigureDigitalOutputPin(resetPin, DigitalOutputPinAttributes.Default, true);
        }

        ~ColorTFT()
        {
            device.ReleaseDigitalPin(csPort);
            device.ReleaseDigitalPin(dcPort);
            device.ReleaseDigitalPin(resetPort);
        }


        public void DrawFrame(GraphicsBuffer.DrawCallback callback)
        {
            if (graphics == null)
                graphics = new GraphicsBuffer(Width, Height, true);

            byte[] pixelData = graphics.Draw(callback, GraphicsFormat.RGB565);
            Draw(pixelData, Width, 0, 0);
        }


        public void Draw(byte[] data, int rowLength, int tileX, int tileY, int tileWidth, int tileHeight, int x, int y)
        {
            byte[] tileData = new byte[tileWidth * 2 * tileHeight];
            int sourcePtr = tileY * rowLength * 2 + tileX * 2;
            int destPtr = 0;
            for (int i = 0; i < tileHeight; i++)
            {
                Array.Copy(data, sourcePtr, tileData, destPtr, tileWidth * 2);
                sourcePtr += rowLength * 2;
                destPtr += tileWidth * 2;
            }

            Draw(tileData, tileWidth, x, y);
        }


        public void Draw(byte[] data, int rowLength, int x, int y)
        {
            if (!isInitialized)
            {
                InitDevice();
                isInitialized = true;
            }

            SetAddressWindow(x, y, rowLength, data.Length / rowLength / 2);
            SendCommand(RAMWR, SwapPairsOfBytes(data));
        }


        private void InitDevice()
        {
            Reset();

            SendCommand(SWRESET, null);
            Thread.Sleep(150);
            SendCommand(SLPOUT, null);
            Thread.Sleep(500);

            SendCommand(FRMCTR1, new byte[] { 0x01, 0x2C, 0x2D });
            SendCommand(FRMCTR2, new byte[] { 0x01, 0x2C, 0x2D });
            SendCommand(FRMCTR3, new byte[] { 0x01, 0x2C, 0x2D, 0x01, 0x2C, 0x2D });
            SendCommand(INVCTR, new byte[] { 0x07 });
            SendCommand(PWCTR1, new byte[] { 0xA2, 0x02, 0x84 });
            SendCommand(PWCTR2, new byte[] { 0xC5 });
            SendCommand(PWCTR3, new byte[] { 0x0A, 0x00 });
            SendCommand(PWCTR4, new byte[] { 0x8A, 0x2A });
            SendCommand(PWCTR5, new byte[] { 0x8A, 0xEE });
            SendCommand(VMCTR1, new byte[] { 0x0E });
            SendCommand(INVOFF, null);
            SendCommand(MADCTL, new byte[] { 0xC8 });
            SendCommand(COLMOD, new byte[] { 0x05 });

            SendCommand(CASET, new byte[] { 0x00, 0x00, 0x00, 0x7F });
            SendCommand(RASET, new byte[] { 0x00, 0x00, 0x00, 0x9F });

            SendCommand(GMCTRP1, new byte[] { 0x00, 0x00, 0x00, 0x9F });
            SendCommand(GMCTRN1, new byte[] { 0x00, 0x00, 0x00, 0x9F });
            SendCommand(NORON, null);
            Thread.Sleep(10);
            SendCommand(DISPON, null);
            Thread.Sleep(100);

            SendCommand(MADCTL, new byte[] { 0xC0 });
        }


        private void SendCommand(byte command, byte[] data)
        {
            if (device.IsClosed)
                return;

            // select command mode
            device.WriteDigitalPinSynchronizedWithSPI(dcPort, false, spi);

            byte[] commandData = new byte[] { command };
            device.SubmitOnSPIPort(spi, commandData, csPort);

            // select data mode
            device.WriteDigitalPinSynchronizedWithSPI(dcPort, true, spi);

            if (data == null || data.Length == 0)
                return;

            int offset = 0;
            while (offset < data.Length)
            {
                int end = Math.Min(offset + 1024, data.Length);
                byte[] slice = new byte[end - offset];
                Array.Copy(data, offset, slice, 0, end - offset);
                device.SubmitOnSPIPort(spi, slice, csPort);
                offset = end;
            }
        }


        private void Reset()
        {
            device.WriteDigitalPin(resetPort, true);
            device.WriteDigitalPin(csPort, false);
            Thread.Sleep(500);
            device.WriteDigitalPin(resetPort, false);
            Thread.Sleep(500);
            device.WriteDigitalPin(resetPort, true);
            Thread.Sleep(500);
            device.WriteDigitalPin(csPort, true);
        }


        private void SetAddressWindow(int x, int y, int w, int h)
        {
            SendCommand(CASET, new byte[] { 0x00, (byte)x, 0x00, (byte)(x + w - 1) });
            SendCommand(RASET, new byte[] { 0x00, (byte)y, 0x00, (byte)(y + h - 1) });
        }


        public static byte[] SwapPairsOfBytes(byte[] pixelData)
        {
            int len = pixelData.Length;
            byte[] result = new byte[len];

            for (int i = 0; i < len; i += 2)
            {
                result[i] = pixelData[i + 1];
                result[i + 1] = pixelData[i];
            }

            return result;
        }
    }

}
