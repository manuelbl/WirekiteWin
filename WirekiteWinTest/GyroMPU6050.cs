/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Codecrete.Wirekite.Device;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Codecrete.Wirekite.Test.UI
{
    public class GyroMPU6050
    {

        private WirekiteDevice device;
        private UInt16 i2cPort;
        private bool releasePort;

        public UInt16 GyroAddress = 0x68;

        private int gyroXOffset = 0;
        private int gyroYOffset = 0;
        private int gyroZOffset = 0;

        public int GyroX = 0;
        public int GyroY = 0;
        public int GyroZ = 0;

        public double Temperature = 0;

        public int AccelX = 0;
        public int AccelY = 0;
        public int AccelZ = 0;

        public bool IsCalibrating { get; private set; }


        public GyroMPU6050(WirekiteDevice device, I2CPins i2cPins)
        {
            this.device = device;
            i2cPort = device.ConfigureI2CMaster(i2cPins, 100000);
            releasePort = true;
            InitSensor();
        }


        public GyroMPU6050(WirekiteDevice device, UInt16 i2cPort)
        {
            this.device = device;
            this.i2cPort = i2cPort;
            releasePort = false;
            InitSensor();
        }


        ~GyroMPU6050()
        {
            if (releasePort)
                device.ReleaseI2CPort(i2cPort);
        }


        private void InitSensor()
        {
            SetRegister(0x6b, 0x00);
            SetRegister(0x1b, 0x00);
            SetRegister(0x1c, 0x08);
            SetRegister(0x1a, 0x03);
        }


        public void Read()
        {
            byte[] data = ReadBytes(0x3b, 14);
            AccelX = (Int16)((data[0] << 8) | data[1]);
            AccelY = (Int16)((data[2] << 8) | data[3]);
            AccelZ = (Int16)((data[4] << 8) | data[5]);
            double t = (Int16)((data[6] << 8) | data[7]);
            Temperature = t / 340 + 36.53;
            GyroX = (Int16)((data[8] << 8) | data[9]) + gyroXOffset;
            GyroY = (Int16)((data[10] << 8) | data[11]) + gyroYOffset;
            GyroZ = (Int16)((data[12] << 8) | data[13]) + gyroZOffset;
        }


        private void SetRegister(byte register, byte value)
        {
            byte[] data = new byte[] { register, value };
            int transmitted = device.SendOnI2CPort(i2cPort, data, GyroAddress);
            if (transmitted != data.Length)
                throw new Exception("Faild to set gyro register");
        }


        private byte[] ReadBytes(byte startRegister, int numBytes)
        {
            byte[] sendData = new byte[] { startRegister };
            byte[] receivedData = device.SendAndRequestOnI2CPort(i2cPort, sendData, GyroAddress, (UInt16)numBytes);
            if (receivedData.Length != numBytes)
                throw new Exception("Failed to read gyro values");
            return receivedData;
        }


        public void StartCalibration()
        {
            Task.Run(() => Calibrate());
        }


        private void Calibrate()
        {
            IsCalibrating = true;

            int offsetX = 0;
            int offsetY = 0;
            int offsetZ = 0;

            const int numSamples = 500;
            for (int i = 0; i < numSamples; i++)
            {
                byte[] data = ReadBytes(0x43, 6);
                offsetX += (Int16)((data[0] << 8) | data[1]);
                offsetY += (Int16)((data[2] << 8) | data[3]);
                offsetZ += (Int16)((data[4] << 8) | data[5]);

                Thread.Sleep(1);
            }

            gyroXOffset = -(offsetX + numSamples / 2) / numSamples;
            gyroYOffset = -(offsetY + numSamples / 2) / numSamples;
            gyroZOffset = -(offsetZ + numSamples / 2) / numSamples;

            IsCalibrating = false;
        }

    }
}
