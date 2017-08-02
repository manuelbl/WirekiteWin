/**
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
        internal UInt16 PortOrRequestId;
        internal UInt16 PortAttributes;
        internal UInt16 PinConfig;
        internal UInt32 Value1;

        internal ConfigRequest()
        {
            MessageSize = 16;
            MessageType = MessageTypeConfigRequest;
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
            WriteHeader(buf, offset);
            buf[4] = Action;
            buf[5] = PortType;
            WriteUInt16(buf, 6, PortOrRequestId);
            WriteUInt16(buf, 8, PortAttributes);
            WriteUInt16(buf, 10, PinConfig);
            WriteUInt32(buf, 12, Value1);
        }
    }
}
