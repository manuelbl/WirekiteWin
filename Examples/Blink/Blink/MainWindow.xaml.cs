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

namespace Blink
{
    public partial class MainWindow : Window, IWirekiteDeviceNotification
    {
        private WirekiteDevice _device;
        private WirekiteService _service;

        private Timer _ledTimer;
        private int _builtinLED;
        private bool _ledOn = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _service = new WirekiteService(this, this);
        }

        public void OnDeviceConnected(WirekiteDevice device)
        {
            _device = device;
            _device.ResetConfiguration();

            _builtinLED = device.ConfigureDigitalOutputPin(13, DigitalOutputPinAttributes.Default);
            _ledTimer = new Timer(Blink, null, 300, 500);
        }

        public void Blink(Object stateInfo)
        {
            _ledOn = !_ledOn;
            _device.WriteDigitalPin(_builtinLED, _ledOn);
        }

        public void OnDeviceDisconnected(WirekiteDevice device)
        {
            _ledTimer.Dispose();
            _device = null;
        }
    }
}
