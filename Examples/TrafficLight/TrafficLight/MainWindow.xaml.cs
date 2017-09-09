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

namespace TrafficLight
{
    public partial class MainWindow : Window, IWirekiteDeviceNotification
    {
        private WirekiteDevice device;
        private WirekiteService service;

        private Timer timer;
        private int redLight;
        private int orangeLight;
        private int greenLight;
        private int trafficLightPhase;

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

            redLight = device.ConfigureDigitalOutputPin(16, DigitalOutputPinAttributes.HighCurrent);
            orangeLight = device.ConfigureDigitalOutputPin(17, DigitalOutputPinAttributes.HighCurrent);
            greenLight = device.ConfigureDigitalOutputPin(21, DigitalOutputPinAttributes.HighCurrent);
            timer = new Timer(SwichTrafficLight, null, 0, 500);
        }

        public void SwichTrafficLight(Object stateInfo)
        {
            device.WriteDigitalPin(redLight, trafficLightPhase <= 3);
            device.WriteDigitalPin(orangeLight, trafficLightPhase == 3 || trafficLightPhase == 6);
            device.WriteDigitalPin(greenLight, trafficLightPhase >= 4 && trafficLightPhase <= 5);

            trafficLightPhase++;
            if (trafficLightPhase == 7)
                trafficLightPhase = 0;
        }

        public void OnDeviceDisconnected(WirekiteDevice device)
        {
            timer.Dispose();
            device = null;
        }
    }
}
