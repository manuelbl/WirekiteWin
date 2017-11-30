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


    internal class PendingRequestList
    {
        private readonly object inserted = new object();
        private List<PendingResponse> completedRequests = new List<PendingResponse>();
        private HashSet<UInt16> waitingForRequests = new HashSet<UInt16>();
        private bool isDestroyed = false;


        internal void PutResponse(UInt16 requestId, Message response)
        {
            lock(inserted)
            {
                if (waitingForRequests.Contains(requestId))
                {
                    completedRequests.Add(new PendingResponse { RequestId = requestId, Response = response });
                    Monitor.PulseAll(inserted);
                }
            }
        }


        internal void AnnounceRequest(UInt16 requestId)
        {
            lock(inserted)
            {
                waitingForRequests.Add(requestId);
            }
        }


        internal Message WaitForResponse(UInt16 requestId)
        {
            lock(inserted)
            {
                waitingForRequests.Add(requestId);

                while (!isDestroyed)
                {
                    int cnt = completedRequests.Count;
                    for (int i = 0; i < cnt; i++)
                    {
                        if (completedRequests[i].RequestId == requestId)
                        {
                            Message response = completedRequests[i].Response;
                            completedRequests.RemoveAt(i);
                            waitingForRequests.Remove(requestId);
                            return response;
                        }
                    }

                    // not found: wait...
                    Monitor.Wait(inserted);
                }

                waitingForRequests.Remove(requestId);
                return null;
            }
        }

        internal void Clear()
        {
            lock (inserted)
            {
                completedRequests.Clear();
                waitingForRequests.Clear();
                Monitor.PulseAll(inserted);
            }
        }
    }
}
