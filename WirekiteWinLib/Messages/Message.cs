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

        public virtual void Dump()
        {
            Debug.WriteLine("Header - size: {0:X4}, type: {1:X2}, port ID: {2:X4}, request ID: {3:X4}",
                MessageSize, MessageType, PortId, RequestId);
        }

        internal void DumpData(byte[] data)
        {
            Debug.WriteLine("Data   - {0}", BinaryDataToHex(data), null);
        }

        static string BinaryDataToHex(byte[] bytes)
        {
            int len = bytes.Length;
            char[] c = new char[len * 2];
            for (int i = 0; i < len; i++)
            {
                int b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }
            return new string(c);
        }

    }
}
