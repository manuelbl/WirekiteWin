using Codecrete.Wirekite.Device.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecrete.Wirekite.Device
{
    [Flags]
    public enum DigitalOutputPinAttributes
    {
        Default = 0,
        LowCurrent = 4,
        HighCurrent = 8
    }


    public partial class WirekiteDevice
    {
        public UInt16 ConfigureDigitalOutputPin(int pin, DigitalOutputPinAttributes attributes)
        {
            ConfigRequest request = new ConfigRequest();
            request.Action = Message.ConfigActionConfigPort;
            request.PortType = Message.PortTypeDigitalPin;
            request.PinConfig = (UInt16)pin;
            request.PortAttributes = (UInt16)(1 + (UInt16)attributes);

            ConfigResponse response = SendConfigRequest(request);
            Port port = new Port(response.PortId, PortType.DigitalOutput, 10);
            _ports.AddPort(port);
            return port.Id;
        }


        public void ReleaseDigitalPin(UInt16 port)
        {
            ConfigRequest request = new ConfigRequest();
            request.Action = Message.ConfigActionRelease;
            request.PortOrRequestId = port;

            SendConfigRequest(request);
        }

        public void WriteDigitalPin(UInt16 port, bool value)
        {
            PortRequest request = new PortRequest();
            request.PortId = port;
            request.Action = Message.PortActionSetValue;
            request.Data = new byte[4];
            request.Data[0] = value ? (byte)1 : (byte)0;

            SendPortRequest(request);
        }
    }
}
