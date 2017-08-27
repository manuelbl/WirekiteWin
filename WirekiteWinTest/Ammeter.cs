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
    public class Ammeter
    {
        public const int InvalidValue = 0x7fffffff;

        private WirekiteDevice device;
        private UInt16 i2cPort;
        private bool releasePort;

        public UInt16 AmmeterAddress = 0x40;


        public Ammeter(WirekiteDevice device, I2CPins i2cPins)
        {
            this.device = device;
            i2cPort = device.ConfigureI2CMaster(i2cPins, 100000);
            releasePort = true;
            InitSensor();
        }


        public Ammeter(WirekiteDevice device, UInt16 i2cPort)
        {
            this.device = device;
            this.i2cPort = i2cPort;
            releasePort = false;
            InitSensor();
        }


        ~Ammeter()
        {
            if (releasePort)
                device.ReleaseI2CPort(i2cPort);
        }


        public double ReadAmps()
        {
            int value = ReadRegister(4, 2);
            if (value == InvalidValue)
                return double.NaN;

            return value / 10.0;
        }


        private void InitSensor()
        {
            WriteRegister(5, 4096);
            WriteRegister(0, 0x2000 | 0x1800 | 0x04000 | 0x0018 | 0x0007);
        }


        private void WriteRegister(int register, int value)
        {
            byte[] data = new byte[] { (byte)register, (byte)(value >> 8), (byte)value };
            device.SendOnI2CPort(i2cPort, data, AmmeterAddress);
        }


        private int ReadRegister(int register, int length)
        {
            byte[] txData = new byte[] { (byte)register };
            byte[] rxData = device.SendAndRequestOnI2CPort(i2cPort, txData, AmmeterAddress, (UInt16)length);
            if (rxData != null && rxData.Length == length)
            {
                return (Int16)((rxData[0] << 8) | rxData[1]);
            }

            return InvalidValue;
        }

    }    
}
