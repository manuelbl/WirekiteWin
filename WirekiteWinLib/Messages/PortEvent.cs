/*
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
        internal byte Event;
        internal byte EventAttribute1;
        internal UInt16 EventAttribute2;
        internal UInt32 Value1;
        internal byte[] Data;


        internal override int GetMinimumLength()
        {
            return 16;
        }

        internal override void Read(byte[] buf, int offset)
        {
            ReadHeader(buf, offset);
            Event = buf[offset + 8];
            EventAttribute1 = buf[offset + 9];
            EventAttribute2 = ReadUInt16(buf, offset + 10);
            Value1 = ReadUInt32(buf, offset + 12);
            if (MessageSize > 16)
            {
                Data = new byte[MessageSize - 16];
                Array.Copy(buf, offset + 16, Data, 0, MessageSize - 16);
            }
        }

        internal override void Write(byte[] buf, int offset)
        {
            throw new NotImplementedException();
        }
    }
}
