using Microsoft.Win32.SafeHandles;
using System;
using System.Threading;
using static Codecrete.Wirekite.Device.Internal.Win32;
using static Codecrete.Wirekite.Device.Internal.WinUsb;


namespace Codecrete.Wirekite.Device
{
    public class WirekiteDevice : IDisposable
    {
        private const Byte RxEndpointAddress = 0x81; // endpoint 1 / IN
        private const Byte TxEndpointAddress = 0x02; // endpoint 2 / OUT

        private SafeFileHandle _deviceHandle;
        private IntPtr _interfaceHandle;
        private bool _disposed = false;

        private NativeOverlapped[] rxBufferInfo;
        private int activeRxBuffer = 0;

        internal WirekiteDevice(SafeFileHandle deviceHandle, IntPtr interfaceHandle)
        {
            _deviceHandle = deviceHandle;
            _interfaceHandle = interfaceHandle;

            rxBufferInfo = new Overlapped[2];
            for (int i = 0; i < 2; i++)
            {
                rxBufferInfo[i] = new OVERLAPPED();
                rxBufferInfo[i].Offset = 0;
                rxBufferInfo[i].OffsetHigh = 0;
                rxBufferInfo[i].EventHandle = CreateEvent(IntPtr.Zero, false, false, null);
            }
        }


        ~WirekiteDevice()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            WinUsb_Free(_interfaceHandle);
            _interfaceHandle = IntPtr.Zero;

            if (disposing)
            {
                _deviceHandle.Dispose();
            }
            _deviceHandle = null;

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
