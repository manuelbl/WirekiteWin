//
// Wirekite for Windows 
// Copyright (c) 2017 Manuel Bleichenbacher
// Licensed under MIT License
// https://opensource.org/licenses/MIT
//

using Codecrete.Wirekite.Device;
using System;


namespace Nunchuck
{
    public class NunchuckController
    {
        public WirekiteDevice Device;
        public int I2CPort;
        public int SlaveAddress = 0x52;

        public int JoystickX;
        public int JoystickY;
        public int AccelerometerX;
        public int AccelerometerY;
        public int AccelerometerZ;
        public bool ButtonC;
        public bool ButtonZ;

        public NunchuckController(WirekiteDevice device, int i2cPort)
        {
            Device = device;
            I2CPort = i2cPort;
            InitController();
        }

        public void ReadData()
        {
            // Read the six bytes with the sensor data
            byte[] sensorData = Device.RequestDataOnI2CPort(I2CPort, SlaveAddress, 6);
            if (sensorData == null || sensorData.Length != 6)
                throw new Exception(String.Format("Nunchuck read failed - reason {0}", Device.GetLastI2CResult(I2CPort)));

            // Assign the sensor data to the public variables
            JoystickX = sensorData[0];
            JoystickY = sensorData[1];
            AccelerometerX = (sensorData[2] << 2) | ((sensorData[5] >> 2) & 0x3);
            AccelerometerY = (sensorData[3] << 2) | ((sensorData[5] >> 4) & 0x3);
            AccelerometerZ = (sensorData[4] << 2) | ((sensorData[5] >> 6) & 0x3);
            ButtonC = (sensorData[5] & 2) == 0;
            ButtonZ = (sensorData[5] & 1) == 0;

            // Prepare the next data read (start conversion)
            Device.SubmitOnI2CPort(I2CPort, new byte[] { 0 }, SlaveAddress);
        }


        private void InitController()
        {
            byte[][] initSequences = new byte[][] {
                new byte[] { 0xf0, 0x55 },
                new byte[] { 0xfb, 0x00 }
            };
        
            foreach (byte[] seq in initSequences)
            {
                int numBytes = Device.SendOnI2CPort(I2CPort, seq, SlaveAddress);
                if (numBytes != seq.Length)
                    throw new Exception(String.Format("Nunchuck init failed - reason {0}", Device.GetLastI2CResult(I2CPort)));
            }
        }
    }
}
