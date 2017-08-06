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
    public partial class MainWindow : Window, IWirekiteDeviceNotification
    {
        private WirekiteDevice _device;
        private Timer _timer;
        private ushort _builtinLED;
        private bool _ledOn = false;

        private ushort _redLED;
        private ushort _orangeLED;
        private ushort _greenLED;

        private ushort _switchPort;


        public MainWindow()
        {
            InitializeComponent();
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WirekiteService service = new WirekiteService(this, this);
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
                Dispatcher.Invoke(new Action(() => { SetSwitchDisplay(value); }));
            });
            SetSwitchDisplay(device.ReadDigitalPin(_switchPort));

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


        private void ledCheckBox_Changed(object sender, RoutedEventArgs e)
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
    }
}
