/**
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;
using System.Runtime.InteropServices;

namespace Codecrete.Wirekite.Device.Internal
{
    internal static class Setup
    {
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
    }
}
