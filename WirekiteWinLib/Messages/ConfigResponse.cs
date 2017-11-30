/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */
 
 using System;


namespace Codecrete.Wirekite.Device.Messages
{
    internal class ConfigResponse : Message
    {
        internal UInt16 Result;
        internal UInt16 Optional1;
        internal UInt32 Value1;

        internal override int GetMinimumLength()
        {
            return 16;
        }

        internal override void Read(byte[] buf, int offset)
        {
            ReadHeader(buf, offset);
            Result = ReadUInt16(buf, offset + 8);
            Optional1 = ReadUInt16(buf, offset + 10);
            Value1 = ReadUInt32(buf, offset + 12);
        }

        internal override void Write(byte[] buf, int offset)
        {
            throw new NotImplementedException();
        }
    }
}
