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
    internal class PortRequest : Message
    {
        internal byte Action;
        internal byte ActionAttribute1;
        internal UInt16 ActionAttribute2;
        internal UInt32 Value1;

        private byte[] _data;
        internal byte[] Data
        {
            get { return _data; }
            set
            {
                _data = value;
                MessageSize = (UInt16)(16 + (value != null ? value.Length : 0));
            }
        }

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
            buf[offset + 8] = Action;
            buf[offset + 9] = ActionAttribute1;
            WriteUInt16(buf, offset + 10, ActionAttribute2);
            WriteUInt32(buf, offset + 12, Value1);
            if (Data != null)
                Array.Copy(Data, 0, buf, offset + 16, Data.Length);
        }

        public override void Dump()
        {
            base.Dump();
            Debug.WriteLine("Body   - action: {0:X2}, attr1: {1:X2}, attr2: {2:X4}, value: {3:X8}",
                Action, ActionAttribute1, ActionAttribute2, Value1);
            if (Data != null)
                DumpData(Data);
        }
    }
}
