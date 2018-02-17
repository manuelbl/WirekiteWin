# Wirekite for Windows

Wire up digital and analog IOs to your PC and control them with your C# / VB.NET code run on your PC.

To connect the inputs and outputs, use a [Teensy development board](https://www.pjrc.com/teensy/) connected via USB. It looks a lot like an Arduino Nano connected for programming. Yet with Wirekite the custom code is written for and run on your computer – not for the microcontroller.

See the [Wiki](https://github.com/manuelbl/Wirekite/wiki) for more information and [how to get started](https://github.com/manuelbl/Wirekite/wiki/Visual-Studio-Project-Setup).

[.NET API documentation](https://www.codecrete.net/WirekiteWin/)


## Supported boards

- [Teensy LC](https://www.pjrc.com/store/teensylc.html)
- [Teensy 3.2](https://www.pjrc.com/store/teensy32.html)


## Supported inputs / outputs / protocols

- Digital output
- Digital input
- Analog input
- PWM output
- I2C
- SPI


## Repositories

There are three repositories:

 - [Wirekite](https://github.com/manuelbl/Wirekite) – code for the Teensy board and home of the [Wiki](https://github.com/manuelbl/Wirekite/wiki)
 - [WirekiteMac](https://github.com/manuelbl/WirekiteMac) – the Mac libraries for using the Wirekite in Objective-C or Swift on a Macintosh
 - [WirekiteWin](https://github.com/manuelbl/WirekiteWin) – the .NET libraries for using the Wirekite in C# or VB.NET on a Windows computer  (this repository)
