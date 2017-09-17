//
// Wirekite for Windows 
// Copyright (c) 2017 Manuel Bleichenbacher
// Licensed under MIT License
// https://opensource.org/licenses/MIT
//

using Codecrete.Wirekite.Device;
using System;
using System.Threading;
using System.Windows;

namespace Nunchuck
{
    public partial class MainWindow : Window, IWirekiteDeviceNotification
    {
        private WirekiteDevice device;
        private WirekiteService service;

        private Timer timer;
        private int i2cPort;
        private NunchuckController nunchuck;


        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            service = new WirekiteService(this, this);
        }

        public void OnDeviceConnected(WirekiteDevice device)
        {
            this.device = device;
            device.ResetConfiguration();

            i2cPort = device.ConfigureI2CMaster(I2CPins.I2CPinsSCL19_SDA18, 100000);
            nunchuck = new NunchuckController(device, i2cPort);
            timer = new Timer(ReadNunchuck, null, 0, 100);
        }

        public void ReadNunchuck(Object stateInfo)
        {
            nunchuck.ReadData();

            Dispatcher.Invoke(() =>
            {
                joystickXLabel.Content = String.Format("X: {0}", nunchuck.JoystickX);
                joystickYLabel.Content = String.Format("Y: {0}", nunchuck.JoystickY);
                acceleroMeterXLabel.Content = String.Format("X: {0}", nunchuck.AccelerometerX);
                acceleroMeterYLabel.Content = String.Format("Y: {0}", nunchuck.AccelerometerY);
                acceleroMeterZLabel.Content = String.Format("Z: {0}", nunchuck.AccelerometerZ);
                buttonCLabel.Content = nunchuck.ButtonC ? "C: Pressed" : "C: -";
                buttonZLabel.Content = nunchuck.ButtonZ ? "Z: Pressed" : "Z: -";
            });
        }

        public void OnDeviceDisconnected(WirekiteDevice device)
        {
            timer.Dispose();
            device = null;
        }
    }
}