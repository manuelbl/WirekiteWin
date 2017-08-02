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

        internal const int ERROR_NO_MORE_ITEMS = 259;


        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFile(string fileName, UInt32 desiredAccess, UInt32 shareMode,
            IntPtr securityAttributes, UInt32 creationDisposition, UInt32 flagsAndAttributes, IntPtr templateFile);
    }
}
