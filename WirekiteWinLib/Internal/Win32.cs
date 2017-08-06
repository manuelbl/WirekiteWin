/**
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;


namespace Codecrete.Wirekite.Device.Internal
{
    internal static class Win32
    {
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        internal const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;

        internal const UInt32 FILE_FLAG_OVERLAPPED = 0x40000000;

        internal const UInt32 FILE_SHARE_READ = 1;
        internal const UInt32 FILE_SHARE_WRITE = 2;

        internal const UInt32 GENERIC_READ = 0x80000000;
        internal const UInt32 GENERIC_WRITE = 0x40000000;

        internal const UInt32 OPEN_EXISTING = 3;

        internal const UInt32 ERROR_NO_MORE_ITEMS = 259;
        internal const UInt32 ERROR_IO_PENDING = 997;

        internal const int WM_DEVICECHANGE = 0x0219;
        internal const int WM_NCDESTROY = 0x0082;

        internal const UInt32 DBT_DEVTYP_DEVICEINTERFACE = 5;
        internal const int DBT_DEVICEARRIVAL = 0x8000;        
        internal const int DBT_DEVICEREMOVECOMPLETE = 0x8004;


        [StructLayout(LayoutKind.Sequential)]
        internal class DEV_BROADCAST_HDR
        {
            internal UInt32 dbchSize;
            internal UInt32 dbchDevicetype;
            internal UInt32 dbchReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DEV_BROADCAST_DEVICEINTERFACE
        {
            internal UInt32 Size;
            internal UInt32 DeviceType;
            internal UInt32 Reserved;
            internal Guid ClassGuid;
            internal UInt16 Name;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal class DEV_BROADCAST_DEVICEINTERFACE_2
        {
            internal UInt32 dbchSize;
            internal UInt32 dbchDevicetype;
            internal UInt32 dbchReserved;
            internal Guid dbccClassguid;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
            internal Char[] dbccName;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFile(string fileName, UInt32 desiredAccess, UInt32 shareMode,
            IntPtr securityAttributes, UInt32 creationDisposition, UInt32 flagsAndAttributes, IntPtr templateFile);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, UInt32 flags);

        [DllImport("user32.dll")]
        internal static extern bool UnregisterDeviceNotification(IntPtr handle);

    }
}
