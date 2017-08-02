/**
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;


namespace Codecrete.Wirekite.Device
{
    class WirekiteException : Exception
    {
        public WirekiteException(string message)
            : base(message)
        {
        }

        public WirekiteException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        internal static void ThrowWin32Exception(string message)
        {
            throw new WirekiteException(message, new Win32Exception(Marshal.GetLastWin32Error()));
        }
    }
}
