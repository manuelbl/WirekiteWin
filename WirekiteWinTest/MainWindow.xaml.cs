/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Codecrete.Wirekite.Device;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;


namespace Codecrete.Wirekite.Test.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IWirekiteDeviceNotification, IDisposable
    {
        private WirekiteDevice _device;
        private WirekiteService _service;
        private Timer _timer;
        private ushort _builtinLED;
        private bool _ledOn = false;

        private ushort _redLED;
        private ushort _orangeLED;
        private ushort _greenLED;

        private ushort _dutyCyclePin;
        private ushort _frequencyPin;
        private int _prevFrequencyValue;

        private ushort _switchPort;

        private ushort _voltageXPin;
        private ushort _voltageYPin;
        private ushort _stickSwitchPin;
        private Brush _stickPressedColor = Brushes.Orange;
        private Brush _stickReleasedColor = Brushes.LightGray;


        public MainWindow()
        {
            InitializeComponent();
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _service = new WirekiteService(this, this);
        }


        public void Blink(Object stateInfo)
        {
            _ledOn = !_ledOn;
            _device.WriteDigitalPin(_builtinLED, _ledOn);
        }


        public void OnDeviceConnected(WirekiteDevice device)
        {
            _device = device;
            _device.ResetConfiguration();
            _builtinLED = device.ConfigureDigitalOutputPin(13, DigitalOutputPinAttributes.Default);

            _redLED = device.ConfigureDigitalOutputPin(16, DigitalOutputPinAttributes.HighCurrent);
            _orangeLED = device.ConfigureDigitalOutputPin(17, DigitalOutputPinAttributes.HighCurrent);
            _greenLED = device.ConfigureDigitalOutputPin(21, DigitalOutputPinAttributes.HighCurrent);

            _switchPort = device.ConfigureDigitalInputPin(12, DigitalInputPinAttributes.Pullup | DigitalInputPinAttributes.TriggerRaising | DigitalInputPinAttributes.TriggerFalling, (port, value) =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    SetSwitchDisplay(value);
                }));
            });
            SetSwitchDisplay(device.ReadDigitalPin(_switchPort));

            _dutyCyclePin = device.ConfigureAnalogInputPin(AnalogPin.A4, 127, (port, value) => {
                string text = String.Format("{0:##0.0} %", value * 100 / 32767.0);
                Dispatcher.Invoke(new Action(() =>
                {
                    dutyCycleValueLabel.Content = text;
                }));
            });

            _frequencyPin = device.ConfigureAnalogInputPin(AnalogPin.A1, 149, (port, value) => {
                if (Math.Abs(value - _prevFrequencyValue) > 100) {
                    _prevFrequencyValue = value;
                    int frequency = (int)((Math.Exp(Math.Exp(value / 32767.0)) - Math.Exp(1)) * 900 + 10);
                    string text = String.Format("{0} Hz", frequency);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        frequencyValueLabel.Content = text;
                    }));
                }
            });

            _voltageXPin = device.ConfigureAnalogInputPin(AnalogPin.A8, 137, (port, value) => {
                Dispatcher.Invoke(new Action(() =>
                {
                    analogStick.XDirection = 1.0 - value / 16383.0;
                }));
            });
            _voltageYPin = device.ConfigureAnalogInputPin(AnalogPin.A9, 139, (port, value) => {
                Dispatcher.Invoke(new Action(() =>
                {
                    analogStick.YDirection = 1.0 - value / 16383.0;
                }));
            });


            _stickSwitchPin = device.ConfigureDigitalInputPin(20,
                DigitalInputPinAttributes.TriggerRaising | DigitalInputPinAttributes.TriggerFalling | DigitalInputPinAttributes.Pullup,
                (port, value) => {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        analogStick.Foreground = value ? _stickReleasedColor : _stickPressedColor;
                    }));
            });
            analogStick.Foreground = device.ReadDigitalPin(_stickSwitchPin) ? _stickReleasedColor : _stickPressedColor;

            _timer = new Timer(Blink, null, 300, 500);
        }


        private void SetSwitchDisplay(bool value)
        {
            SolidColorBrush brush = value ? Brushes.Crimson : Brushes.LightGray;
            buttonDisplay.Fill = brush;
        }


        public void OnDeviceDisconnected(WirekiteDevice device)
        {
            _timer.Dispose();
            _timer = null;
            _device = null;

            redCheckBox.IsChecked = false;
            orangeCheckBox.IsChecked = false;
            greenCheckBox.IsChecked = false;
            SetSwitchDisplay(false);
        }


        private void LedCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_device == null)
                return; // this event is even fired if the checkbox is changed programmatically

            ushort led;
            if (sender == redCheckBox)
                led = _redLED;
            else if (sender == orangeCheckBox)
                led = _orangeLED;
            else
                led = _greenLED;

            _device.WriteDigitalPin(led, ((CheckBox)sender).IsChecked.Value);
        }

        #region IDisposable Support
        private bool _isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_device != null)
                        _device.Dispose();
                    if (_timer != null)
                        _timer.Dispose();
                    if (_service != null)
                        _service.Dispose();
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
