/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;


namespace Codecrete.Wirekite.Device.Messages
{
    internal class ConfigRequest : Message
    {
        internal byte Action;
        internal byte PortType;
        internal UInt16 PinConfig;
        internal UInt32 Value1;
        internal UInt16 PortAttributes1;
        internal UInt16 PortAttributes2;

        internal ConfigRequest()
        {
            MessageSize = 20;
            MessageType = MessageTypeConfigRequest;
        }

        internal override int GetMinimumLength()
        {
            return 20;
        }

        internal override void Read(byte[] buf, int offset)
        {
            throw new NotImplementedException();
        }

        internal override void Write(byte[] buf, int offset)
        {
            WriteHeader(buf, offset);
            buf[8] = Action;
            buf[9] = PortType;
            WriteUInt16(buf, 10, PinConfig);
            WriteUInt32(buf, 12, Value1);
            WriteUInt16(buf, 16, PortAttributes1);
            WriteUInt16(buf, 18, PortAttributes2);
        }
    }
}
