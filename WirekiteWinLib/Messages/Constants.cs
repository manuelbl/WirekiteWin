/**
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using System;


namespace Codecrete.Wirekite.Device.Messages
{
    internal partial class Message
    {
        internal const byte MessageTypeConfigRequest = 1;
        internal const byte MessageTypeConfigResponse = 2;
        internal const byte MessageTypePortRequest = 3;
        internal const byte MessageTypePortEvent = 4;

        internal const byte ConfigActionConfigPort = 1;
        internal const byte ConfigActionRelease = 2;
        internal const byte ConfigActionReset = 3;
        internal const byte ConfigActionConfigModule = 4;

        internal const byte PortActionSetValue = 1;
        internal const byte PortActionGetValue = 2;

        internal const byte PortTypeDigitalPin = 1;
        internal const byte PortTypeAnalogIn = 2;
        internal const byte PortTypePWM = 3;

        internal const byte ConfigModulePWMTimer = 1;
        internal const byte ConfigModulePWMChannel = 2;

        internal const byte ResultOK = 0;
        internal const byte ResultInvalidData = 1;

        internal const byte EventDodo = 0;
        internal const byte EventSingleSample = 1;
    }
}
