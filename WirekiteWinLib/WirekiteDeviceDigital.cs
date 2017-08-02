using Codecrete.Wirekite.Device.Messages;
using System;
using System.Collections.Concurrent;
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

    [Flags]
    public enum DigitalInputPinAttributes
    {
        Default = 0,
        Pullup = 4,
        Pulldown = 8,
        TriggerRaising = 16,
        TriggerFalling = 32
    }

    public enum InputCommunication
    {
        OnDemand,
        Precached
    }

    public delegate void DigitalInputCallback(UInt16 port, bool value);



    public partial class WirekiteDevice
    {
        private ConcurrentDictionary<UInt16, DigitalInputCallback> _digitalInputCallbacks = new ConcurrentDictionary<ushort, DigitalInputCallback>();


        public UInt16 ConfigureDigitalOutputPin(int pin, DigitalOutputPinAttributes attributes)
        {
            Port port = ConfigureDigitalPin(pin, PortType.DigitalOutput, (UInt16)(1 + (UInt16)attributes));
            return port.Id;
        }


        public void ReleaseDigitalPin(UInt16 port)
        {
            ConfigRequest request = new ConfigRequest();
            request.Action = Message.ConfigActionRelease;
            request.PortOrRequestId = port;

            SendConfigRequest(request);
            DigitalInputCallback callback;
            _digitalInputCallbacks.TryRemove(port, out callback);
            _ports.RemovePort(port);
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

        public UInt16 ConfigureDigitalInputPin(int pin, DigitalInputPinAttributes attributes, InputCommunication communication)
        {
            if (communication != InputCommunication.OnDemand && communication != InputCommunication.Precached)
                throw new WirekiteException("Digital input pin witout notification must use communication \"OnDemand\" or \"Precached\"");

            if ((attributes & (DigitalInputPinAttributes.TriggerRaising | DigitalInputPinAttributes.TriggerFalling)) != 0)
                throw new WirekiteException("Digital input pin without callback must not use attributes \"DigiInPinTriggerRaising\" and/or \"DigiInPinTriggerFalling\"");

            PortType type;
            if (communication == InputCommunication.OnDemand)
            {
                type = PortType.DigitalInputOnDemand;
            }
            else
            {
                type = PortType.DigitalInputPrecached;
                attributes |= DigitalInputPinAttributes.TriggerRaising | DigitalInputPinAttributes.TriggerFalling;
            }

            Port port = ConfigureDigitalPin(pin, type, (UInt16)attributes);
            return port.Id;
        }


        public UInt16 ConfigureDigitalInputPin(int pin, DigitalInputPinAttributes attributes, DigitalInputCallback callback)
        {
            if ((attributes & (DigitalInputPinAttributes.TriggerRaising | DigitalInputPinAttributes.TriggerFalling)) == 0)
                throw new WirekiteException("Digital input pin with callback requires attribute \"DigiInPinTriggerRaising\" and/or \"DigiInPinTriggerFalling\"");

            Port port = ConfigureDigitalPin(pin, PortType.DigitalInputTriggering, (UInt16)attributes);
            _digitalInputCallbacks.TryAdd(port.Id, callback);
            return port.Id;
        }


        public bool ReadDigitalPin(UInt16 port)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", port));

            PortType type = p.Type;
            if (type == PortType.DigitalInputPrecached || type == PortType.DigitalInputTriggering)
                return p.LastSample != 0;

            PortRequest request = new PortRequest();
            request.PortId = port;
            request.Action = Message.PortActionGetValue;
            SendPortRequest(request);

            PortEvent evt = p.WaitForEvent();
            return evt.Data[0] != 0;
        }


        private Port ConfigureDigitalPin(int pin, PortType portType, UInt16 attributes)
        {
            ConfigRequest request = new ConfigRequest();
            request.Action = Message.ConfigActionConfigPort;
            request.PortType = Message.PortTypeDigitalPin;
            request.PinConfig = (UInt16)pin;
            request.PortAttributes = attributes;

            ConfigResponse response = SendConfigRequest(request);
            Port port = new Port(response.PortId, portType, 10);
            _ports.AddPort(port);
            if ((attributes & 1) == 0) // input pin
                port.LastSample = response.Optional1;
            return port;
        }


        private void HandleDigitalPinEvent(PortEvent evt)
        {
            Port port = _ports.GetPort(evt.PortId);

            if (evt.Action == Message.EventSingleSample)
            {
                PortType type = port.Type;
                if (type == PortType.DigitalInputOnDemand)
                {
                    port.PushEvent(evt);
                    return;
                }
                else if (type == PortType.DigitalInputPrecached || type == PortType.DigitalInputTriggering)
                {
                    bool value = evt.Data[0] != 0;
                    port.LastSample = evt.Data[0];

                    if (type == PortType.DigitalInputTriggering)
                    {
                        DigitalInputCallback callback;
                        if (_digitalInputCallbacks.TryGetValue(port.Id, out callback))
                        {
                            callback(port.Id, value);
                        }
                    }
                }
            }
        }
    }
}
