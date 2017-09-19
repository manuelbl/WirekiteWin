/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;


namespace Codecrete.Wirekite.Device.USB
{
    internal class NativeMethods
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

        internal const Int32 DIGCF_PRESENT = 0x02;
        internal const Int32 DIGCF_DEVICEINTERFACE = 0x10;


        internal struct SP_DEVICE_INTERFACE_DATA
        {
            internal UInt32 Size;
            internal Guid InterfaceClassGuid;
            internal UInt32 Flags;
            internal IntPtr Reserved;
        }

        internal struct SP_DEVINFO_DATA
        {
            internal UInt32 Size;
            internal Guid ClassGuid;
            internal UInt32 DevInst;
            internal IntPtr Reserved;
        }

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

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, UInt32 flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, UInt32 memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData, UInt32 deviceInterfaceDetailDataSize,
            ref UInt32 requiredSize, ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("winusb.dll", SetLastError = true)]
        internal static extern bool WinUsb_Initialize(SafeFileHandle deviceHandle, ref IntPtr interfaceHandle);

        [DllImport("winusb.dll", SetLastError = true)]
        internal static extern bool WinUsb_Free(IntPtr interfaceHandle);

        [DllImport("winusb.dll", SetLastError = true)]
        internal static unsafe extern bool WinUsb_WritePipe(IntPtr interfaceHandle, byte pipeID,
            byte* buffer, UInt32 bufferLength,
            out UInt32 lengthTransferred, NativeOverlapped* overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        internal static unsafe extern bool WinUsb_ReadPipe(IntPtr interfaceHandle, byte pipeID,
            byte* buffer, UInt32 bufferLength,
            out UInt32 lengthTransferred, NativeOverlapped* overlapped);
    }
}
