/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static Codecrete.Wirekite.Device.USB.NativeMethods;
using System.Windows;
using System.Windows.Interop;
using System.Collections.Generic;

namespace Codecrete.Wirekite.Device
{
    /// <summary>
    /// Interface to be implemented by classes that receive notifications about connected and disconnected Wirekite devices.
    /// </summary>
    public interface IWirekiteDeviceNotification
    {
        /// <summary>
        /// Called when a device has been connected and initially when the <see cref="WirekiteService"/> is created.
        /// </summary>
        /// <param name="device">the connected device</param>
        void OnDeviceConnected(WirekiteDevice device);
        /// <summary>
        /// Called when a device has been disconnected.
        /// </summary>
        /// <param name="device">the disconnected device</param>
        /// <remarks>
        /// The device is already closed and all configured ports are invalid.
        /// </remarks>
        void OnDeviceDisconnected(WirekiteDevice device);
    }



    /// <summary>
    /// Service to notify about connected and disconnected devices.
    /// </summary>
    /// <remarks>
    /// When an instance of this class is created, it immediately notifies about
    /// the already connected devices.
    /// </remarks>
    public class WirekiteService : IDisposable
    {
        /// <summary>
        /// The GUID of the USB interface of the Wirekite device.
        /// </summary>
        public static readonly Guid WirekiteInterfaceGuid = new Guid("{d010ba90-025a-4c94-b2db-a13f205d437e}");

        private IWirekiteDeviceNotification _deviceNotification;

        private Guid _interfaceGuid = new Guid(WirekiteInterfaceGuid.ToByteArray());
        private HwndSource _hwndSource;
        private IntPtr _notificationHandle;
        private IntPtr _notificationFilterBuffer;
        private List<WirekiteDevice> _devices = new List<WirekiteDevice>();


        /// <summary>
        /// Constructs an instance
        /// </summary>
        /// <param name="notification">the object to be notified about connected and disconnected Wirekite devices</param>
        /// <param name="wpfWindow">a WPF window used to hook into Windows message handling</param>
        /// <remarks>The specified notification object will be immediately notified about already connected Wirekite devices</remarks>
        public WirekiteService(IWirekiteDeviceNotification notification, Window wpfWindow)
        {
            _deviceNotification = notification;
            HwndSource _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(wpfWindow).Handle);
            if (_hwndSource == null)
                throw new WirekiteException("Unable to create the HwndSource");

            _hwndSource.AddHook(HwndHandler);
            RegisterUSBDeviceNotification(_hwndSource.Handle);
            FindDevices();
        }


        internal void RemoveDevice(WirekiteDevice device)
        {
            _devices.Remove(device);
        }


        #region Enumerate connected devices

        private void FindDevices()
        {
            IntPtr deviceInfoSet = SetupDiGetClassDevs(ref _interfaceGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (deviceInfoSet == INVALID_HANDLE_VALUE)
                WirekiteException.ThrowWin32Exception("Failed to enumerate Wirekite devices");

            try
            {
                for (UInt32 index = 0; true; index++)
                {
                    SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                    deviceInterfaceData.Size = (UInt32)Marshal.SizeOf(deviceInterfaceData);
                    bool success = SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref _interfaceGuid, index, ref deviceInterfaceData);
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

            WirekiteDevice device = new WirekiteDevice(this, devicePath, deviceHandle, interfaceHandle);
            _devices.Add(device);
            if (_deviceNotification != null)
                _deviceNotification.OnDeviceConnected(device);
        }

        #endregion


        #region Device notification

        private void RegisterUSBDeviceNotification(IntPtr windowHandle)
        {
            DEV_BROADCAST_DEVICEINTERFACE dbi = new DEV_BROADCAST_DEVICEINTERFACE
            {
                DeviceType = DBT_DEVTYP_DEVICEINTERFACE,
                Reserved = 0,
                ClassGuid = _interfaceGuid,
                Name = 0
            };

            dbi.Size = (UInt32)Marshal.SizeOf(dbi);
            _notificationFilterBuffer = Marshal.AllocHGlobal((int)dbi.Size);
            Marshal.StructureToPtr(dbi, _notificationFilterBuffer, false);

            _notificationHandle = RegisterDeviceNotification(windowHandle, _notificationFilterBuffer, 0);
            if (_notificationHandle == INVALID_HANDLE_VALUE)
                WirekiteException.ThrowWin32Exception("Registration of device notification failed");
        }


        private void UnregisterUSBDeviceNotification()
        {
            if (_notificationHandle == IntPtr.Zero)
                return;

            UnregisterDeviceNotification(_notificationHandle);
            _notificationHandle = IntPtr.Zero;

            Marshal.FreeHGlobal(_notificationFilterBuffer);
            _notificationFilterBuffer = IntPtr.Zero;
        }


        private void OnDeviceConnected(IntPtr deviceInfo)
        {
            string devicePath = GetDevicePathFromBroadcast(deviceInfo);
            if (devicePath == null)
                return;

            OpenUSBDevice(devicePath);
        }


        private void OnDeviceDisconnected(IntPtr deviceInfo)
        {
            string devicePath = GetDevicePathFromBroadcast(deviceInfo);
            if (devicePath == null)
                return;

            foreach (WirekiteDevice device in _devices)
            {
                if (String.Equals(device.DevicePath, devicePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (_deviceNotification != null)
                        _deviceNotification.OnDeviceDisconnected(device);
                    device.Close();
                    return;
                }
                    
            }
        }


        private string GetDevicePathFromBroadcast(IntPtr deviceInfo)
        {
            DEV_BROADCAST_HDR header = new DEV_BROADCAST_HDR();
            Marshal.PtrToStructure(deviceInfo, header);

            if (header.dbchDevicetype != DBT_DEVTYP_DEVICEINTERFACE)
                return null;

            DEV_BROADCAST_DEVICEINTERFACE_2 devInterface = new DEV_BROADCAST_DEVICEINTERFACE_2();
            UInt32 stringLength = (header.dbchSize - 32) / 2;
            devInterface.dbccName = new char[stringLength + 1];
            Marshal.PtrToStructure(deviceInfo, devInterface);
            return new String(devInterface.dbccName, 0, (int)stringLength);
        }


        private IntPtr HwndHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == WM_DEVICECHANGE)
            {
                switch ((int)wparam)
                {
                    case DBT_DEVICEARRIVAL:
                        OnDeviceConnected(lparam);
                        break;

                    case DBT_DEVICEREMOVECOMPLETE:
                        OnDeviceDisconnected(lparam);
                        break;
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        #endregion

        #region IDisposable Support
        private bool isDisposed = false; // To detect redundant calls

        /// <summary>
        /// Disposes of the resources associated with this service
        /// </summary>
        /// <param name="disposing">Indicates if it is called from <see cref="Dispose()"/></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                UnregisterUSBDeviceNotification();

                if (disposing)
                {
                    if (_hwndSource != null)
                    {
                        _hwndSource.Dispose();
                        _hwndSource = null;
                    }
                }

                while (_devices.Count > 0)
                {
                    _devices[_devices.Count - 1].Close();
                }

                isDisposed = true;
            }
        }


        /// <summary>
        /// Finalizes this instance
        /// </summary>
        ~WirekiteService() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }


        /// <summary>
        /// Disposes of all resources associated with this instance
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }


}
