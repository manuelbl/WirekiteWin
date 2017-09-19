/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Codecrete.Wirekite.Device.Messages;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Codecrete.Wirekite.Device.USB
{
    internal struct PendingResponse
    {
        internal UInt16 RequestId;
        internal Message Response;
    }


    internal class PendingResponseList
    {
        private readonly object inserted = new object();
        private List<PendingResponse> responses = new List<PendingResponse>();


        internal void PutResponse(UInt16 requestId, Message response)
        {
            lock(inserted)
            {
                responses.Add(new PendingResponse { RequestId = requestId, Response = response });
                Monitor.PulseAll(inserted);
            }
        }


        internal Message WaitForResponse(UInt16 requestId)
        {
            lock(inserted)
            {
                while (true)
                {
                    int cnt = responses.Count;
                    for (int i = 0; i < cnt; i++)
                    {
                        if (responses[i].RequestId == requestId)
                        {
                            Message message = responses[i].Response;
                            responses.RemoveAt(i);
                            return message;
                        }
                    }

                    // not found: wait...
                    Monitor.Wait(inserted);
                }
            }
        }

        internal void Clear()
        {
            lock (inserted)
            {
                responses.Clear();
                Monitor.PulseAll(inserted);
            }
        }
    }
}
