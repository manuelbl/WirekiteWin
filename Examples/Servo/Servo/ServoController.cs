//
// Wirekite for Windows 
// Copyright (c) 2017 Manuel Bleichenbacher
// Licensed under MIT License
// https://opensource.org/licenses/MIT
//

using Codecrete.Wirekite.Device;
using System;


/// <summary>
/// Controls a servo using pulses between about 0.5ms and 2.1ms
/// </summary>
/// <remarks>
/// Configure the timer associated with the PWM pin for a frequency of 100Hz.
/// </remarks>
namespace Servo
{
    public class ServoController
    {
        /// <summary>
        /// Pin number
        /// </summary>
        public int Pin;

        /// <summary>
        /// Configured PWM port ID
        /// </summary>
        public int Port;

        /// <summary>
        /// Wirekite device
        /// </summary>
        public WirekiteDevice Device;

        /// <summary>
        /// Pulse width for 0 degree (in ms)
        /// </summary>
        public double PulseWidth0Deg = 0.54;

        /// <summary>
        /// Pulse width for 180 degree (in ms)
        /// </summary>
        public double PulseWidth180Deg = 2.10;

        /// <summary>
        /// Frequency of pulse width modulation.
        /// </summary>
        /// <remarks>
        /// Set this to the frequency the timer is configured.
        /// </remarks>
        public double Frequency = 100;


        public ServoController(WirekiteDevice device, int pin)
        {
            Device = device;
            Pin = pin;
        }

        ~ServoController()
        {
            if (Port != 0)
                Device.ReleasePWMPin(Port);
        }

        public void TurnOn(double initialAngle)
        {
            if (Port == 0)
                Port = Device.ConfigurePWMOutputPin(Pin, DutyCycle(initialAngle));
        }

        public void TurnOff()
        {
            if (Port != 0)
            {
                Device.ReleasePWMPin(Port);
                Port = 0;
            }
        }

        private double DutyCycle(double angle)
        {
            var clampedAngle = angle < 0 ? 0 : (angle > 180 ? 180 : angle);
            var frequencyInt = (int)(Frequency + 0.5);
            var lengthMs = clampedAngle / 180 * (PulseWidth180Deg - PulseWidth0Deg) + PulseWidth0Deg;
            return lengthMs / (1000 / frequencyInt);
        }

        public void MoveTo(double angle)
        {
            Device.WritePWMPin(Port, DutyCycle(angle));
        }
    }
}
