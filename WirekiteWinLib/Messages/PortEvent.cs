/**
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;


namespace Codecrete.Wirekite.Device.Messages
{
    internal class PortEvent : Message
    {
        internal UInt16 PortId;
        internal byte Event;
        internal byte EventAttribute1;
        internal UInt16 RequestId;
        internal byte[] Data;


        internal override int GetMinimumLength()
        {
            return 14;
        }

        internal override void Read(byte[] buf, int offset)
        {
            ReadHeader(buf, offset);
            PortId = ReadUInt16(buf, offset + 4);
            Event = buf[offset + 6];
            EventAttribute1 = buf[offset + 7];
            RequestId = ReadUInt16(buf, offset + 8);
            Data = new byte[MessageSize - 10];
            Array.Copy(buf, offset + 10, Data, 0, MessageSize - 10);
        }

        internal override void Write(byte[] buf, int offset)
        {
            throw new NotImplementedException();
        }
    }
}
