/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;
using System.Collections.Generic;
using System.Threading;

namespace Codecrete.Wirekite.Device.USB
{
    internal class Throttler
    {
        private int memorySize = 4200;
        public int MemorySize
        {
            get
            {
                return memorySize;
            }

            set
            {
                lock (available)
                {
                    int oldMemorySize = memorySize;
                    memorySize = value;
                    if (memorySize > oldMemorySize)
                        Monitor.PulseAll(available);
                }
            }
        }

        private int occupiedSize = 0;

        private int maxOutstandingRequests = 20;
        public int MaxOutstandingRequests
        {
            get
            {
                return maxOutstandingRequests;
            }

            set
            {
                lock (available)
                {
                    int oldMaxOutstandingRequest = maxOutstandingRequests;
                    maxOutstandingRequests = value;
                    if (maxOutstandingRequests > oldMaxOutstandingRequest)
                        Monitor.PulseAll(available);
                }
            }
        }

        private int outstandingRequests = 0;
        private readonly object available = new object();
        private Dictionary<UInt16, UInt16> requests = new Dictionary<UInt16, UInt16>();
        private bool isDestroyed = false;


        internal void WaitUntilAvailable(UInt16 requestId, UInt16 requiredMemSize)
        {
            requiredMemSize += 16;
            lock (available)
            {
                while (!isDestroyed)
                {
                    if (memorySize - occupiedSize >= requiredMemSize
                        && outstandingRequests < maxOutstandingRequests)
                        break;
                    Monitor.Wait(available);
                }

                if (!isDestroyed)
                {
                    occupiedSize += requiredMemSize;
                    outstandingRequests++;
                    requests[requestId] = requiredMemSize;
                }
            }
        }


        internal void RequestCompleted(UInt16 requestId)
        {
            lock (available)
            {
                if (requests.TryGetValue(requestId, out ushort requestSize))
                {
                    requests.Remove(requestId);
                    occupiedSize -= requestSize;
                    outstandingRequests--;
                    Monitor.PulseAll(available);
                }
            }
        }


        internal void Clear()
        {
            lock (available)
            {
                isDestroyed = true;
                Monitor.PulseAll(available);
            }

            lock (available)
            {
                isDestroyed = false;
                outstandingRequests = 0;
                occupiedSize = 0;
                requests.Clear();
            }
        }
    }
}
