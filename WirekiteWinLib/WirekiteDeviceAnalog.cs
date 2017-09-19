/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Codecrete.Wirekite.Device.Messages;
using Codecrete.Wirekite.Device.USB;
using System;
using System.Collections.Concurrent;


namespace Codecrete.Wirekite.Device
{
    /// <summary>
    /// Pin to use for analog input or output
    /// </summary>
    public enum AnalogPin
    {
        /// <summary>Analog pin A0</summary>
        A0 = 0,
        /// <summary>Analog pin A1</summary>
        A1 = 1,
        /// <summary>Analog pin A2</summary>
        A2 = 2,
        /// <summary>Analog pin A3</summary>
        A3 = 3,
        /// <summary>Analog pin A4</summary>
        A4 = 4,
        /// <summary>Analog pin A5</summary>
        A5 = 5,
        /// <summary>Analog pin A6</summary>
        A6 = 6,
        /// <summary>Analog pin A7</summary>
        A7 = 7,
        /// <summary>Analog pin A8</summary>
        A8 = 8,
        /// <summary>Analog pin A9</summary>
        A9 = 9,
        /// <summary>Analog pin A10</summary>
        A10 = 10,
        /// <summary>Analog pin A11</summary>
        A11 = 11,
        /// <summary>Analog pin A12</summary>
        A12 = 12,
        /// <summary>Analog pin A13</summary>
        A13 = 13,
        /// <summary>Analog pin A14</summary>
        A14 = 14,
        /// <summary>Analog pin A15</summary>
        A15 = 15,
        /// <summary>Analog pin A16</summary>
        A16 = 16,
        /// <summary>Analog pin A17</summary>
        A17 = 17,
        /// <summary>Analog pin A18</summary>
        A18 = 18,
        /// <summary>Analog pin A19</summary>
        A19 = 19,
        /// <summary>Analog pin A20</summary>
        A20 = 20,
        /// <summary> Vref / Vref high</summary>
        VREF = 128,
        /// <summary>Temperature</summary>
        Temp = 129,
        /// <summary>Vref low</summary>
        VREFL = 130,
        /// <summary>Band gap</summary>
        BandGap = 131
    }

    /// <summary>
    /// Delegate called periodically to notify about the analog input value.
    /// </summary>
    /// <param name="port">the port ID associated with the analog input</param>
    /// <param name="value">the input value in the range between -1.0 and +1.0</param>
    public delegate void AnalogInputCallback(int port, double value);


    public partial class WirekiteDevice
    {
        private ConcurrentDictionary<int, AnalogInputCallback> _analogInputCallbacks = new ConcurrentDictionary<int, AnalogInputCallback>();


        /// <summary>
        /// Configure a pin as an analog input
        /// </summary>
        /// <remarks>
        /// The analog input value can be read on demand with <see cref="ReadAnalogPin(int)"/>
        /// </remarks>
        /// <param name="pin">the analog pin (as per Teensy documentation)</param>
        /// <returns>the port ID of the configured analog input</returns>
        public int ConfigureAnalogInputPin(AnalogPin pin)
        {
            Port port = ConfigureAnalogInput(pin, 0);
            return port.Id;
        }


        /// <summary>
        /// Configures a pin as an analog input with a delegate the receives a new input value at a specified interval
        /// </summary>
        /// <param name="pin">the analog pin (as per Teensy documentation)</param>
        /// <param name="interval">the interval (in ms) to sample the input value and notify the delegate</param>
        /// <param name="callback">the delegate called periodically with a new input value</param>
        /// <returns>the port ID of the configured analog input</returns>
        /// <remarks>
        /// The notification delegate is called on a background thread.
        /// </remarks>
        public int ConfigureAnalogInputPin(AnalogPin pin, int interval, AnalogInputCallback callback)
        {
            if (interval == 0)
            {
                throw new WirekiteException("Analog input with periodc sampling requires interval > 0");
            }

            Port port = ConfigureAnalogInput(pin, interval);
            _analogInputCallbacks.TryAdd(port.Id, callback);
            return port.Id;
        }

        private Port ConfigureAnalogInput(AnalogPin pin, int interval)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionConfigPort,
                PortType = Message.PortTypeAnalogIn,
                PinConfig = (UInt16)pin,
                Value1 = (UInt32)interval
            };

            ConfigResponse response = SendConfigRequest(request);
            Port port = new Port(response.PortId, interval == 0 ? PortType.AnalogInputOnDemand : PortType.AnalogInputSampling, 10);

            _ports.AddPort(port);
            return port;
        }


        /// <summary>
        /// Releases a analog input and frees the pin for other use
        /// </summary>
        /// <param name="port">the port ID of the analog input</param>
        public void ReleaseAnalogPin(int port)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionRelease,
                PortId = (UInt16)port
            };

            SendConfigRequest(request);
            _analogInputCallbacks.TryRemove(port, out AnalogInputCallback callback);
            Port p = _ports.GetPort(port);
            if (p != null)
                p.Dispose();
            _ports.RemovePort(port);
        }


        /// <summary>
        /// Reads the value of the analog input.
        /// </summary>
        /// <param name="port">the analog input's port ID</param>
        /// <returns>the input value in the range between -1.0 and 1.0</returns>
        public double ReadAnalogPin(int port)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", port));

            PortRequest request = new PortRequest
            {
                PortId = (UInt16)port,
                Action = Message.PortActionGetValue
            };
            SendPortRequest(request);

            PortEvent evt = p.WaitForEvent();
            Int32 v = (Int32)evt.Value1;
            return v < 0 ? v / 2147483648.0 : v / 2147483647.0;
        }


        private void HandleAnalogPinEvent(PortEvent evt)
        {
            Port port = _ports.GetPort(evt.PortId);

            if (evt.Event == Message.EventSingleSample)
            {
                PortType type = port.Type;
                if (type == PortType.AnalogInputOnDemand)
                {
                    port.PushEvent(evt);
                    return;
                }
                else
                {
                    Int32 v = (Int32)evt.Value1;
                    double value = v < 0 ? v / 2147483648.0 : v / 2147483647.0;
                    port.LastSample = evt.Value1;

                    if (_analogInputCallbacks.TryGetValue(port.Id, out AnalogInputCallback callback))
                    {
                        callback(port.Id, value);
                    }
                }
            }
        }

    }
}
