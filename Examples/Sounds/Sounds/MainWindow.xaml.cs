//
// Wirekite for Windows 
// Copyright (c) 2017 Manuel Bleichenbacher
// Licensed under MIT License
// https://opensource.org/licenses/MIT
//

using Codecrete.Wirekite.Device;
using System;
using System.Media;
using System.Windows;

namespace Sounds
{
    public partial class MainWindow : Window, IWirekiteDeviceNotification
    {
        private WirekiteDevice device;
        private WirekiteService service;

        private int greenButton;
        private int redButton;

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

            greenButton = device.ConfigureDigitalInputPin(14, DigitalInputPinAttributes.Pullup | DigitalInputPinAttributes.TriggerFalling, (port, value) =>
            {
                PlaySound("ding_dong");
            });
            redButton = device.ConfigureDigitalInputPin(20, DigitalInputPinAttributes.Pullup | DigitalInputPinAttributes.TriggerFalling, (port, value) =>
            {
                PlaySound("gliss");
            });
        }

        public void PlaySound(string soundName)
        {
            SoundPlayer player = new SoundPlayer(string.Format(@"..\..\{0}.wav", soundName));
            player.Play();
        }

        public void OnDeviceDisconnected(WirekiteDevice device)
        {
            device = null;
        }
    }
}