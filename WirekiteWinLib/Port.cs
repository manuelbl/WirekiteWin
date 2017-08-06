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


    internal class Port : IDisposable
    {
        internal UInt16 Id { get; private set; }
        internal PortType Type { get; private set; }

        internal UInt16 LastSample;
        private BlockingCollection<PortEvent> _eventQueue;


        internal Port(UInt16 id, PortType type, int queueLength)
        {
            Id = id;
            Type = type;
            _eventQueue = new BlockingCollection<PortEvent>(queueLength);
        }

        internal void PushEvent(PortEvent evt)
        {
            _eventQueue.TryAdd(evt);
        }

        internal PortEvent WaitForEvent()
        {
            return _eventQueue.Take();
        }

        #region IDisposable Support
        private bool _isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _eventQueue.Dispose();
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
