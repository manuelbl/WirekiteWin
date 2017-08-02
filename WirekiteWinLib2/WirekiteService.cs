using System;
using Codecrete.Wirekite.Device.Internal;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static Codecrete.Wirekite.Device.Internal.Setup;
using static Codecrete.Wirekite.Device.Internal.Win32;
using static Codecrete.Wirekite.Device.Internal.WinUsb;

namespace Codecrete.Wirekite.Device
{
    public class WirekiteService
    {
        public void FindDevices()
        {
            Guid interfaceGuid = new Guid("{d010ba90-025a-4c94-b2db-a13f205d437e}");
            IntPtr deviceInfoSet = SetupDiGetClassDevs(ref interfaceGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (deviceInfoSet == Win32.INVALID_HANDLE_VALUE)
                WirekiteException.ThrowWin32Exception("Failed to enumerate Wirekite devices");

            try
            {
                for (UInt32 index = 0; true; index++)
                {
                    SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                    deviceInterfaceData.Size = (UInt32)Marshal.SizeOf(deviceInterfaceData);
                    bool success = SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref interfaceGuid, index, ref deviceInterfaceData);
                    if (!success)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        if (errorCode == ERROR_NO_MORE_ITEMS)
                            break;
                        WirekiteException.ThrowWin32Exception("Failed to enumerate Wirekite devices");
                    }

                    IntPtr interfaceDetailData = Marshal.AllocHGlobal(1024);
                    try
                    {
                        Marshal.WriteInt32(interfaceDetailData, IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 4 + 4);

                        SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                        deviceInfoData.Size = (UInt32)Marshal.SizeOf(deviceInfoData);
                        UInt32 requiredSize = 0;
                        success = SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, interfaceDetailData, 1024, ref requiredSize, ref deviceInfoData);
                        if (!success)
                            WirekiteException.ThrowWin32Exception("Failed to get device information");

                        IntPtr devicePathPtr = IntPtr.Add(interfaceDetailData, 4);
                        string devicePath = Marshal.PtrToStringUni(devicePathPtr);
                        OpenUSBDevice(devicePath);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(interfaceDetailData);
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }


        private void OpenUSBDevice(string devicePath)
        {
            SafeFileHandle deviceHandle = CreateFile(devicePath,
                GENERIC_WRITE | GENERIC_READ,
                FILE_SHARE_WRITE | FILE_SHARE_READ,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);

            if (deviceHandle.IsInvalid)
            {
                WirekiteException.ThrowWin32Exception("Cannot open Wirekite device for communication");
            }

            IntPtr interfaceHandle = IntPtr.Zero;
            bool success = WinUsb_Initialize(deviceHandle, ref interfaceHandle);
            if (!success)
            {
                WirekiteException.ThrowWin32Exception("Cannot open USB interface of Wirekite device");
            }

            WirekiteDevice device = new WirekiteDevice(deviceHandle, interfaceHandle);
        }
    }


}
