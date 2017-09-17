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

namespace VoltageSampling
{
    public partial class MainWindow : Window, IWirekiteDeviceNotification
    {
        private WirekiteDevice device;
        private WirekiteService service;

        private int analogInput;
        private int bandgapReference;
        private double bandgap = 1 / 3.3;

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

            analogInput = device.ConfigureAnalogInputPin(AnalogPin.A1, 100, (port, value) =>
            {
                Dispatcher.Invoke(() =>
                {
                    double voltage = value / bandgap;
                    string text = String.Format("{0:0.000} V", voltage);
                    voltageLabel.Content = text;
                });
            });

            bandgapReference = device.ConfigureAnalogInputPin(AnalogPin.BandGap, 990, (port, value) =>
            {
                bandgap = value;
            });
        }

        public void OnDeviceDisconnected(WirekiteDevice device)
        {
            device = null;
        }
    }
}
