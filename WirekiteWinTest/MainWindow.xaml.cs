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
        private const bool useI2CBoard = false;
        private const bool useSpiTFTBoard = false;
        private const bool useSpiRFBoard = true;
        private const bool hasBuiltInLED = true;
        
        private WirekiteDevice _device;
        private WirekiteService _service;

        private Timer _ledTimer;
        private int _builtinLED;
        private bool _ledOn = false;

        private int _redLED;
        private int _orangeLED;
        private int _greenLED;

        private int _dutyCyclePin;
        private int _frequencyPin;
        private double _prevFrequencyValue;

        private int _switchPort;

        private int _voltageXPin;
        private int _voltageYPin;
        private int _stickSwitchPin;
        private Brush _stickPressedColor = Brushes.Orange;
        private Brush _stickReleasedColor = Brushes.LightGray;

        private int _pwmOutputPin;

        private int _i2cPort;

        private Timer _gyroTimer;
        private GyroMPU6050 _gyro;

        private Timer _ammeterTimer;
        private Ammeter _ammeter;

        private OLEDDisplay _display;

        private int _spiPort;
        private ColorTFT _colorTFT;
        private RF24Radio _radio;


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
                        string text = String.Format("{0:##0.0} %", value * 100);
                        dutyCycleValueLabel.Content = text;
                    }));
                });

                _frequencyPin = device.ConfigureAnalogInputPin(AnalogPin.A1, 149, (port, value) =>
                {
                    if (Math.Abs(value - _prevFrequencyValue) > 0.01)
                    {
                        _prevFrequencyValue = value;
                        int frequency = (int)((Math.Exp(Math.Exp(value)) - Math.Exp(1)) * 900 + 10);
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
                        analogStick.XDirection = 1.0 - value * 2;
                    }));
                });
                _voltageYPin = device.ConfigureAnalogInputPin(AnalogPin.A9, 139, (port, value) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        analogStick.YDirection = 1.0 - value * 2;
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

                _pwmOutputPin = device.ConfigurePWMOutputPin(10);
            }

            if (useI2CBoard)
            {
                _i2cPort = _device.ConfigureI2CMaster(I2CPins.I2CPinsSCL16_SDA17, 400000);
                _gyro = new GyroMPU6050(_device, _i2cPort);
                _gyro.StartCalibration();
                _gyroTimer = new Timer(ReadGyro, null, 400, 400);

                _ammeter = new Ammeter(_device, _i2cPort);
                _ammeterTimer = new Timer(ReadAmmeter, null, 300, 350);

                _display = new OLEDDisplay(_device, _i2cPort)
                {
                    DisplayOffset = 2
                };
                StartOLEDShow();
            }

            if (useSpiTFTBoard)
            {
                if (_device.GetBoardInfo(BoardInfo.Board) == WirekiteDevice.BoardTeensyLC)
                {
                    _spiPort = _device.ConfigureSPIMaster(20, 21, WirekiteDevice.InvalidPortId, 16000000, SPIAttributes.Default);
                }
                else
                {
                    _device.ConfigureFlowControl(memorySize: 20000, maxOutstandingRequests: 100);
                    _spiPort = _device.ConfigureSPIMaster(14, 11, WirekiteDevice.InvalidPortId, 18000000, SPIAttributes.Default);
                }
                _colorTFT = new ColorTFT(_device, _spiPort, 6, 4, 5);

                StartColorShow();
            }

            if (useSpiRFBoard)
            {
                if (_device.GetBoardInfo(BoardInfo.Board) == WirekiteDevice.BoardTeensyLC)
                {
                    _spiPort = _device.ConfigureSPIMaster(20, 21, 5, 10000000, SPIAttributes.Default);
                }
                else
                {
                    _device.ConfigureFlowControl(memorySize: 20000, maxOutstandingRequests: 100);
                    _spiPort = _device.ConfigureSPIMaster(14, 11, WirekiteDevice.InvalidPortId, 10000000, SPIAttributes.Default);
                }
                _radio = new RF24Radio(_device, _spiPort, 14, 15);
                _radio.InitModule();
                _radio.RFChannel = 0x52;
                _radio.AutoAck = false;
                _radio.OutputPower = RF24Radio.RFOutputPower.Low;

                _radio.ConfigureIRQPin(4, 10, PacketReceived);

                _radio.OpenTransmitPipe(0x389f30cc1b);
                _radio.OpenReceivePipe(1, 0x38a8bb7201);
                _radio.StartListening();

                _radio.DebugRegisters();
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

            int led;
            if (sender == redCheckBox)
                led = _redLED;
            else if (sender == orangeCheckBox)
                led = _orangeLED;
            else
                led = _greenLED;

            _device.WriteDigitalPin(led, ((CheckBox)sender).IsChecked.Value);
        }


        private void StartOLEDShow()
        {
            Task.Run(() => { OLEDShow(); });
        }


        private void OLEDShow()
        {
            float stringWidth = 0;
            float offset = 0;

            while (!_device.IsClosed)
            {
                _display.ShowFrame((g) =>
                {
                    g.Clear(System.Drawing.Color.Black);
                    // Might only work in Windows 10
                    string text = "\uE007 \uE209 \uE706 \uE774 \uE83D \uE928 \uEA8E \ue007 \uE209";
                    System.Drawing.Font font = new System.Drawing.Font("Segoe MDL2 Assets", 64);
                    g.MeasureString(text, font);
                    g.DrawString(text, font, new System.Drawing.SolidBrush(System.Drawing.Color.White), new System.Drawing.PointF(-offset, 0));

                    if (stringWidth == 0)
                    {
                        System.Drawing.SizeF size = g.MeasureString("\uE007 \uE209 \uE706 \uE774 \uE83D \uE928 \uEA8E", font);
                        stringWidth = size.Width - 64 / 6.0f - 6;
                    }
                });

                offset++;
                if (offset >= stringWidth)
                    offset = 0;
            }
        }


        private void StartColorShow()
        {
            Task.Run(() => ColorShow());
        }


        private void ColorShow()
        {
            byte[] pixelData = new byte[128 * 160 * 2];
            for (int i = 0; i < pixelData.Length; i += 2)
            {
                pixelData[i] = 0xff;
                pixelData[i + 1] = 0xff;
            }
            _colorTFT.Draw(pixelData, 128, 0, 0);

            byte[] fruitStrip;

            using (System.Drawing.Image img = System.Drawing.Image.FromFile("Fruits.png"))
            {
                using (GraphicsBuffer graphics = new GraphicsBuffer(48, 540, true))
                {
                    fruitStrip = graphics.Draw((g) =>
                    {
                        g.DrawImage(img, new System.Drawing.Rectangle(0, 0, 48, 540),
                            new System.Drawing.Rectangle(0, 0, 48, 540), System.Drawing.GraphicsUnit.Pixel);
                    }, GraphicsFormat.RGB565Rotated180);
                }
            }

            int offset = 0;
            while (!_device.IsClosed)
            {
                _colorTFT.Draw(fruitStrip, 48, 0, offset, 48, 160, 10, 0);
                _colorTFT.Draw(fruitStrip, 48, 0, 7 * 54 - offset, 48, 160, 70, 0);

                offset += 2;
                if (offset >= 7 * 54)
                    offset = 0;
            }
        }


        private void PacketReceived(RF24Radio radio, int pipe, byte[] packet)
        {
            Dispatcher.Invoke(() =>
            {
                analogStick.XDirection = ((double)(packet[0] - 127)) / 128;
                analogStick.YDirection = ((double)(packet[1] - 128)) / 128;
                bool upperButton = packet[2] != 0;
                bool lowerButton = packet[3] != 0;
                analogStick.Foreground = upperButton || lowerButton ? _stickPressedColor : _stickReleasedColor;
            });
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
