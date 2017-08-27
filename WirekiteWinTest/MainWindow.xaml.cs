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
using System.Windows.Media;


namespace Codecrete.Wirekite.Test.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IWirekiteDeviceNotification, IDisposable
    {
        private const bool useLEDBoard = false;
        private const bool useI2CBoard= true;
        private const bool hasBuiltInLED = true;
        
        private WirekiteDevice _device;
        private WirekiteService _service;

        private Timer _ledTimer;
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

        private ushort _pwmOutputPin;

        private ushort _i2cPort;

        private Timer _gyroTimer;
        private GyroMPU6050 _gyro;

        private Timer _ammeterTimer;
        private Ammeter _ammeter;


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


        public void ReadGyro(Object stateInfo)
        {
            if (!_gyro.IsCalibrating)
                _gyro.Read();

            Dispatcher.Invoke(() => {
                if (_gyro.IsCalibrating)
                {
                    gyroXValueLabel.Content = "Calibrating...";
                    gyroYValueLabel.Content = "";
                    gyroZValueLabel.Content = "";
                }
                else
                {
                    gyroXValueLabel.Content = String.Format("X: {0}", _gyro.GyroX);
                    gyroYValueLabel.Content = String.Format("Y: {0}", _gyro.GyroY);
                    gyroZValueLabel.Content = String.Format("Z: {0}", _gyro.GyroZ);
                }
            });
        }


        public void ReadAmmeter(object stateInfo)
        {
            double current = _ammeter.ReadAmps();

            Dispatcher.Invoke(() =>
            {
                string text = "- mA";
                if (!double.IsNaN(current))
                    text = string.Format("{0} mA", current);
                ammeterValueLabel.Content = text;
            });
        }


        public void OnDeviceConnected(WirekiteDevice device)
        {
            _device = device;
            _device.ResetConfiguration();

            if (hasBuiltInLED)
            {
                _builtinLED = device.ConfigureDigitalOutputPin(13, DigitalOutputPinAttributes.Default);

                _ledTimer = new Timer(Blink, null, 300, 500);
            }

            if (useLEDBoard)
            {
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

                _dutyCyclePin = device.ConfigureAnalogInputPin(AnalogPin.A4, 127, (port, value) =>
                {
                    device.WritePWMPin(_pwmOutputPin, value);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        string text = String.Format("{0:##0.0} %", value * 100 / 32767.0);
                        dutyCycleValueLabel.Content = text;
                    }));
                });

                _frequencyPin = device.ConfigureAnalogInputPin(AnalogPin.A1, 149, (port, value) =>
                {
                    if (Math.Abs(value - _prevFrequencyValue) > 100)
                    {
                        _prevFrequencyValue = value;
                        int frequency = (int)((Math.Exp(Math.Exp(value / 32767.0)) - Math.Exp(1)) * 900 + 10);
                        _device.ConfigurePWMTimer(0, frequency, PWMTimerAttributes.Default);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            string text = String.Format("{0} Hz", frequency);
                            frequencyValueLabel.Content = text;
                        }));
                    }
                });

                _voltageXPin = device.ConfigureAnalogInputPin(AnalogPin.A8, 137, (port, value) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        analogStick.XDirection = 1.0 - value / 16383.0;
                    }));
                });
                _voltageYPin = device.ConfigureAnalogInputPin(AnalogPin.A9, 139, (port, value) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        analogStick.YDirection = 1.0 - value / 16383.0;
                    }));
                });

                _stickSwitchPin = device.ConfigureDigitalInputPin(20,
                    DigitalInputPinAttributes.TriggerRaising | DigitalInputPinAttributes.TriggerFalling | DigitalInputPinAttributes.Pullup,
                    (port, value) =>
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            analogStick.Foreground = value ? _stickReleasedColor : _stickPressedColor;
                        }));
                    });
                analogStick.Foreground = device.ReadDigitalPin(_stickSwitchPin) ? _stickReleasedColor : _stickPressedColor;

                _pwmOutputPin = device.ConfigurePWMOutputPin(PWMPin.Pin10);
            }

            if (useI2CBoard)
            {
                _i2cPort = _device.ConfigureI2CMaster(I2CPins.I2CPinsSCL16_SDA17, 400000);
                _gyro = new GyroMPU6050(_device, _i2cPort);
                _gyro.StartCalibration();
                _gyroTimer = new Timer(ReadGyro, null, 400, 400);

                _ammeter = new Ammeter(_device, _i2cPort);
                _ammeterTimer = new Timer(ReadAmmeter, null, 300, 350);
            }
        }


        private void SetSwitchDisplay(bool value)
        {
            SolidColorBrush brush = value ? Brushes.Crimson : Brushes.LightGray;
            buttonDisplay.Fill = brush;
        }


        private void DisposeTimers()
        {
            if (_ledTimer != null)
            {
                _ledTimer.Dispose();
                _ledTimer = null;
            }
            if (_gyroTimer != null)
            {
                _gyroTimer.Dispose();
                _gyroTimer = null;
            }
            if (_ammeterTimer != null)
            {
                _ammeterTimer.Dispose();
                _ammeterTimer = null;
            }
        }


        public void OnDeviceDisconnected(WirekiteDevice device)
        {
            DisposeTimers();

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
                    DisposeTimers();
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
