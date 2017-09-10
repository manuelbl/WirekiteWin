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

namespace Servo
{
    public partial class MainWindow : Window, IWirekiteDeviceNotification
    {
        private WirekiteDevice device;
        private WirekiteService service;

        private Timer timer;
        private ServoController servo;
        private double angle;
        private double angleInc = 30;


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

            device.ConfigurePWMTimer(0, 100, PWMTimerAttributes.Default);
            servo = new ServoController(device, 10);
            servo.TurnOn(0);
            timer = new Timer(MoveServo, null, 0, 1000);
        }

        public void MoveServo(Object stateInfo)
        {
            angle += angleInc;
            if (angle > 180.9)
            {
                angleInc = -30.0;
                angle = 150.0;
            }
            if (angle < 0)
            {
                angleInc = 30.0;
                angle = 30.0;
            }
            servo.MoveTo(angle);
        }

        public void OnDeviceDisconnected(WirekiteDevice device)
        {
            timer.Dispose();
            servo.Port = 0;
            servo = null;
            device = null;
        }
    }
}