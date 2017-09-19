/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;
using System.Collections.Generic;

namespace Codecrete.Wirekite.Device.USB
{
    internal class PortList
    {
        private readonly object _synch = new object();
        private Dictionary<int, Port> _ports = new Dictionary<int, Port>();
        private UInt16 _lastRequestId = 0;


        internal Port GetPort(int portId)
        {
            lock (_synch)
            {
                return _ports[portId];
            }
        }

        internal void AddPort(Port port)
        {
            lock (_synch)
            {
                _ports.Add(port.Id, port);
            }
        }

        internal void RemovePort(int portId)
        {
            lock (_synch)
            {
                _ports.Remove(portId);
            }
        }

        internal void Clear()
        {
            lock (_synch)
            {
                foreach (var kv in _ports)
                {
                    kv.Value.Dispose();
                }
                _ports.Clear();
            }
        }

        internal UInt16 NextRequestId()
        {
            lock (_synch)
            {
                _lastRequestId++;
                return _lastRequestId;
            }
        }
    }
}
