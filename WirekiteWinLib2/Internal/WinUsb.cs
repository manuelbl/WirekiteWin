using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;


namespace Codecrete.Wirekite.Device.Internal
{
    internal static class WinUsb
    {
        [DllImport("winusb.dll", SetLastError = true)]
        internal static extern bool WinUsb_Initialize(SafeFileHandle deviceHandle, ref IntPtr interfaceHandle);

        [DllImport("winusb.dll", SetLastError = true)]
        internal static extern bool WinUsb_Free(IntPtr interfaceHandle);

        [DllImport("winusb.dll", SetLastError = true)]
        internal static extern bool WinUsb_WritePipe(IntPtr interfaceHandle, byte pipeID,
            IntPtr buffer, UInt32 bufferLength,
            out UInt32 lengthTransferred, NativeOverlapped* overlapped);

    }
}
