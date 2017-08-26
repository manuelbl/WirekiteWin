/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;

namespace Codecrete.Wirekite.Device.Messages
{
    internal class PortRequest : Message
    {
        internal UInt16 PortId;
        internal byte Action;
        internal byte ActionAttribute1;
        internal UInt16 ActionAttribute2;
        internal UInt16 RequestId;
        internal UInt32 Value1;
        internal byte[] Data;

        internal PortRequest()
        {
            MessageSize = 16;
            MessageType = MessageTypePortRequest;
        }

        internal override int GetMinimumLength()
        {
            return 16;
        }

        internal override void Read(byte[] buf, int offset)
        {
            throw new NotImplementedException();
        }

        internal override void Write(byte[] buf, int offset)
        {
            MessageSize = (UInt16)(16 + (Data != null ? Data.Length : 0));
            WriteHeader(buf, offset);
            WriteUInt16(buf, offset + 4, PortId);
            buf[offset + 6] = Action;
            buf[offset + 7] = ActionAttribute1;
            WriteUInt16(buf, offset + 8, ActionAttribute2);
            WriteUInt16(buf, offset + 10, RequestId);
            WriteUInt32(buf, offset + 12, Value1);
            if (Data != null)
                Array.Copy(Data, 0, buf, offset + 12, Data.Length);
        }
    }
}
