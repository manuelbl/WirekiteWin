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
        internal byte Action;
        internal byte ActionAttribute1;
        internal UInt16 ActionAttribute2;
        internal UInt16 RequestId;
        internal byte[] Data;


        internal override int GetMinimumLength()
        {
            return 16;
        }

        internal override void Read(byte[] buf, int offset)
        {
            ReadHeader(buf, offset);
            PortId = ReadUInt16(buf, offset + 4);
            Action = buf[offset + 6];
            ActionAttribute1 = buf[offset + 7];
            ActionAttribute2 = ReadUInt16(buf, offset + 8);
            RequestId = ReadUInt16(buf, offset + 10);
            Data = new byte[MessageSize - 12];
            Array.Copy(buf, offset + 12, Data, 0, MessageSize - 12);
        }

        internal override void Write(byte[] buf, int offset)
        {
            throw new NotImplementedException();
        }
    }
}
