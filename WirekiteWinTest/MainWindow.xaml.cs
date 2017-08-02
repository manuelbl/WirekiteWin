using Codecrete.Wirekite.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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


        public MainWindow()
        {
            InitializeComponent();

            WirekiteService service = new WirekiteService();
            service.deviceNotification = this;
            service.FindDevices();

            _timer = new Timer(Blink, null, 300, 500);
        }


        public void Blink(Object stateInfo)
        {
            _ledOn = !_ledOn;
            _device.WriteDigitalPin(_builtinLED, _ledOn);
        }

        public void OnDeviceAdded(WirekiteDevice device)
        {
            _device = device;
            _device.ResetConfiguration();
            _builtinLED = device.ConfigureDigitalOutputPin(13, DigitalOutputPinAttributes.Default);

            _redLED = device.ConfigureDigitalOutputPin(16, DigitalOutputPinAttributes.HighCurrent);
            _orangeLED = device.ConfigureDigitalOutputPin(17, DigitalOutputPinAttributes.HighCurrent);
            _greenLED = device.ConfigureDigitalOutputPin(21, DigitalOutputPinAttributes.HighCurrent);
        }


        public void OnDeviceRemoved(WirekiteDevice device)
        {

        }


        private void ledCheckBox_Changed(object sender, RoutedEventArgs e)
        {
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
