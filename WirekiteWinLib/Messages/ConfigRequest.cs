/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;
using System.Diagnostics;

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

        public override void Dump()
        {
            base.Dump();
            Debug.WriteLine("Body   - action: {0:X2}, port type: {1:X2}, pin config: {2:X4}, value: {3:X8}, attr 1: {4:X4}, attr 2: {5:X4}",
                Action, PortType, PinConfig, Value1, PortAttributes1, PortAttributes2);
        }
    }
}
