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
        private ushort _ledPort;
        private bool _ledOn = false;


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
            _device.WriteDigitalPin(_ledPort, _ledOn);
        }

        public void OnDeviceAdded(WirekiteDevice device)
        {
            _device = device;
            _device.ResetConfiguration();
            _ledPort = device.ConfigureDigitalOutputPin(13, DigitalOutputPinAttributes.Default);
        }


        public void OnDeviceRemoved(WirekiteDevice device)
        {

        }
    }
}
