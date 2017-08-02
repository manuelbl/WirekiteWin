/**
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
    internal enum PortType
    {
        DigitalOutput,
        DigitalInputOnDemand,
        DigitalInputPrecached,
        DigitalInputTriggering,
        AnalogInputOnDemand,
        AnalogInputSampling,
        PWMOutput
    }


    internal class Port
    {
        internal UInt16 Id { get; private set; }
        internal PortType Type { get; private set; }

        private UInt16 _lastSample;
        private BlockingCollection<Message> _eventQueue;


        internal Port(UInt16 id, PortType type, int queueLength)
        {
            Id = id;
            Type = type;
            _eventQueue = new BlockingCollection<Message>(queueLength);
        }
    }
}
