/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Codecrete.Wirekite.Device.Messages;
using System;
using System.Collections.Concurrent;


namespace Codecrete.Wirekite.Device
{
    /// <summary>
    /// Additional features of digital output pins
    /// </summary>
    [Flags]
    public enum DigitalOutputPinAttributes
    {
        /// <summary>
        /// Default. No special features enabled.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Enables low-current output.
        /// </summary>
        LowCurrent = 4,
        /// <summary>
        /// Enables high-current output.
        /// </summary>
        HighCurrent = 8
    }


    /// <summary>
    /// Additional features of digital input pins
    /// </summary>
    [Flags]
    public enum DigitalInputPinAttributes
    {
        /// <summary>
        /// Default. No special features enabled.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Enable the pull-up resistor on the input pin.
        /// </summary>
        Pullup = 4,
        /// <summary>
        /// Enable the pull-down resistor on the input pin.
        /// </summary>
        Pulldown = 8,
        /// <summary>
        /// Trigger updates on the raising edge of the input signal.
        /// </summary>
        TriggerRaising = 16,
        /// <summary>
        /// Trigger updates on the falling edge of the input signal.
        /// </summary>
        TriggerFalling = 32
    }

    /// <summary>
    /// Communication type with Wirekite device for input pins.
    /// </summary>
    public enum InputCommunication
    {
        /// <summary>
        /// Read input value on demand.
        /// </summary>
        /// <remarks>
        /// Each read operation requires a communication round-trip with the device.
        /// </remarks>
        OnDemand,
        /// <summary>
        /// Precache input values.
        /// </summary>
        /// <remarks>
        /// The device transmits the input value to the host periodically or on change.
        /// Read operations do not require any communication with the device.</remarks>
        Precached
    }

    /// <summary>
    /// Delegate called when the digital input changes its value.
    /// </summary>
    /// <param name="port">the port ID associated with the digital input</param>
    /// <param name="value">the new input value</param>
    public delegate void DigitalInputCallback(UInt16 port, bool value);



    public partial class WirekiteDevice
    {
        private ConcurrentDictionary<UInt16, DigitalInputCallback> _digitalInputCallbacks = new ConcurrentDictionary<ushort, DigitalInputCallback>();


        /// <summary>
        /// Configures a pin of the device to act as a digital output pin.
        /// </summary>
        /// <param name="pin">the pin number (as labelled on the Teensy)</param>
        /// <param name="attributes">additional features to be configured for the output pin</param>
        /// <returns>the port ID for further operations on the configured digital output</returns>
        public UInt16 ConfigureDigitalOutputPin(int pin, DigitalOutputPinAttributes attributes)
        {
            Port port = ConfigureDigitalPin(pin, PortType.DigitalOutput, (UInt16)(1 + (UInt16)attributes));
            return port.Id;
        }


        /// <summary>
        /// Releases a digital input or output and frees the pin for other usage
        /// </summary>
        /// <param name="port">the port ID of the digital input or output</param>
        public void ReleaseDigitalPin(UInt16 port)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionRelease,
                PortOrRequestId = port
            };

            SendConfigRequest(request);
            _digitalInputCallbacks.TryRemove(port, out DigitalInputCallback callback);
            Port p = _ports.GetPort(port);
            if (p != null)
                p.Dispose();
            _ports.RemovePort(port);
        }


        /// <summary>
        /// Write a value to the digital output
        /// </summary>
        /// <param name="port">the digital output's port ID</param>
        /// <param name="value">the output value</param>
        public void WriteDigitalPin(UInt16 port, bool value)
        {
            PortRequest request = new PortRequest
            {
                PortId = port,
                Action = Message.PortActionSetValue,
                Data = new byte[4],
            };
            request.Data[0] = value ? (byte)1 : (byte)0;

            SendPortRequest(request);
        }


        /// <summary>
        /// Configure a pin as a digital input
        /// </summary>
        /// <remarks>
        /// The digital input value can be read with <see cref="ReadDigitalPin(ushort)"/>
        /// </remarks>
        /// <param name="pin">the pin number (as labelled on the Teensy board)</param>
        /// <param name="attributes">additional features to be configured</param>
        /// <param name="communication">the type of communication with the Wirekite device</param>
        /// <returns>the port ID of the configured digital input</returns>
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


        /// <summary>
        /// Configures a pin as a digitial input with notification about changes of the input value
        /// </summary>
        /// <param name="pin">the pin number (as labelled on the Teensy board)</param>
        /// <param name="attributes">additional features to be configured</param>
        /// <param name="callback">the delegate notified about changes of the input value</param>
        /// <returns>the port ID of the configured digital input</returns>
        /// <remarks>
        /// The <paramref name="attributes"/> must specify whether the notification should be triggered on
        /// the raising edge, the falling edge or both edges of the signal. The notification delegate is
        /// called on a background thread.
        /// </remarks>
        public UInt16 ConfigureDigitalInputPin(int pin, DigitalInputPinAttributes attributes, DigitalInputCallback callback)
        {
            if ((attributes & (DigitalInputPinAttributes.TriggerRaising | DigitalInputPinAttributes.TriggerFalling)) == 0)
                throw new WirekiteException("Digital input pin with callback requires attribute \"DigiInPinTriggerRaising\" and/or \"DigiInPinTriggerFalling\"");

            Port port = ConfigureDigitalPin(pin, PortType.DigitalInputTriggering, (UInt16)attributes);
            _digitalInputCallbacks.TryAdd(port.Id, callback);
            return port.Id;
        }


        /// <summary>
        /// Reads the value of the digital input.
        /// </summary>
        /// <param name="port">the digital input's port ID</param>
        /// <returns>the input value</returns>
        public bool ReadDigitalPin(UInt16 port)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", port));

            PortType type = p.Type;
            if (type == PortType.DigitalInputPrecached || type == PortType.DigitalInputTriggering)
                return p.LastSample != 0;

            PortRequest request = new PortRequest {
                PortId = port,
                Action = Message.PortActionGetValue
            };
            SendPortRequest(request);

            PortEvent evt = p.WaitForEvent();
            return evt.Data[0] != 0;
        }


        private Port ConfigureDigitalPin(int pin, PortType portType, UInt16 attributes)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionConfigPort,
                PortType = Message.PortTypeDigitalPin,
                PinConfig = (UInt16)pin,
                PortAttributes = attributes
            };

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

            if (evt.Event == Message.EventSingleSample)
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
                        if (_digitalInputCallbacks.TryGetValue(port.Id, out DigitalInputCallback callback))
                        {
                            callback(port.Id, value);
                        }
                    }
                }
            }
        }
    }
}
