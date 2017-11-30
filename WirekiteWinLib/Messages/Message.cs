/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;


namespace Codecrete.Wirekite.Device.Messages
{
    internal abstract partial class Message
    {
        internal UInt16 MessageSize;
        internal byte MessageType;
        internal byte Reserved0;
        internal UInt16 PortId;
        internal UInt16 RequestId;

        internal abstract int GetMinimumLength();
        internal abstract void Write(byte[] buf, int offset);
        internal abstract void Read(byte[] buf, int offset);

        internal void WriteHeader(byte[] buf, int offset)
        {
            WriteUInt16(buf, offset, MessageSize);
            buf[offset + 2] = MessageType;
            buf[offset + 3] = Reserved0;
            WriteUInt16(buf, offset + 4, PortId);
            WriteUInt16(buf, offset + 6, RequestId);
        }

        internal void WriteUInt16(byte[] buf, int offset, UInt16 value)
        {
            buf[offset] = (byte)value;
            buf[offset + 1] = (byte)(value >> 8);
        }

        internal void WriteUInt32(byte[] buf, int offset, UInt32 value)
        {
            buf[offset] = (byte)value;
            buf[offset + 1] = (byte)(value >> 8);
            buf[offset + 2] = (byte)(value >> 16);
            buf[offset + 3] = (byte)(value >> 24);
        }

        internal void ReadHeader(byte[] buf, int offset)
        {
            MessageSize = ReadUInt16(buf, offset);
            MessageType = buf[offset + 2];
            Reserved0 = buf[offset + 3];
            PortId = ReadUInt16(buf, offset + 4);
            RequestId = ReadUInt16(buf, offset + 6);
        }

        internal UInt16 ReadUInt16(byte[] buf, int offset)
        {
            UInt16 b0 = buf[offset];
            UInt16 b1 = buf[offset + 1];
            return (UInt16)((b1 << 8) | b0);
        }

        internal UInt32 ReadUInt32(byte[] buf, int offset)
        {
            UInt32 b0 = buf[offset];
            UInt32 b1 = buf[offset + 1];
            UInt32 b2 = buf[offset + 2];
            UInt32 b3 = buf[offset + 3];
            return (b3 << 24) | (b2 << 16) | (b1 << 8) | b0;
        }
    }
}
