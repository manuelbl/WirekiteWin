using Codecrete.Wirekite.Device.Messages;
using Codecrete.Wirekite.Device.USB;
using System;


namespace Codecrete.Wirekite.Device
{
    /// <summary>
    /// Additional features of PWM timers
    /// </summary>
    [Flags]
    public enum PWMTimerAttributes
    {
        /// <summary> Default. No special features enabled.</summary>
        Default = 0,
        /// <summary> Edge-aligned PWM signals. </summary>
        EdgeAligned = 0,
        /// <summary> Center-aligned PWM signals. </summary>
        CenterAligned = 1
    }


    /// <summary>
    /// Additional features of PWM channels
    /// </summary>
    [Flags]
    public enum PWMChannelAttributes
    {
        /// <summary> Default. No special features enabled.</summary>
        Default = 0,
        /// <summary> Output high on pulse.</summary>
        HighPulse = 0,
        /// <summary> Output low on pulse.</summary>
        LowPulse = 1
    }


    public partial class WirekiteDevice
    {
        /// <summary>
        /// Configures a pin as a PWM output.
        /// </summary>
        /// <param name="pin">the pin as labelled on the board</param>
        /// <param name="initialDutyCycle">the initial duty cycle between 0.0 for 0% and 1.0 for 100%</param>
        /// <returns>the PWM output's port ID</returns>
        public int ConfigurePWMOutputPin(int pin, double initialDutyCycle = 0.0)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionConfigPort,
                PortType = Message.PortTypePWM,
                PinConfig = (UInt16)pin,
                Value1 = (UInt32)(initialDutyCycle * 2147483647 + 0.5)
            };

            ConfigResponse response = SendConfigRequest(request);
            Port port = new Port(response.PortId, PortType.PWMOutput, 10);
            _ports.AddPort(port);
            return port.Id;
        }


        /// <summary>
        /// Releases a pin configured as a PWM output.
        /// </summary>
        /// <param name="port">the PWM output's port ID</param>
        public void ReleasePWMPin(int port)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionRelease,
                PortId = (UInt16)port
            };

            SendConfigRequest(request);
            Port p = _ports.GetPort(port);
            if (p != null)
                p.Dispose();
            _ports.RemovePort(port);
        }


        /// <summary>
        /// Configures a timer associated with PWM outputs.
        /// </summary>
        /// <remarks>
        /// A PWM output is linked to a timer. Several PWM outputs
        /// share the same timer.
        /// See ... for the association between PWM pins and timers.
        /// When configuring a PWM timer, it might affect several PWM outputs.
        /// </remarks>
        /// <param name="timer">the timer index (0 .. n, depending on the board)</param>
        /// <param name="frequency">the frequency of the PWM signal (in Hz)</param>
        /// <param name="attributes">PWM attributes such as edge/center aligned</param>
        public void ConfigurePWMTimer(int timer, int frequency, PWMTimerAttributes attributes)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionConfigModule,
                PortType = Message.ConfigModulePWMTimer,
                PinConfig = (UInt16)timer,
                PortAttributes1 = (UInt16)attributes,
                Value1 = (UInt32)frequency
            };

            SendConfigRequest(request);
        }


        /// <summary>
        /// Configures a channel associated with PWM outputs.
        /// </summary>
        /// <remarks>
        /// A PWM output is linked to a timer and a channel. Several PWM outputs
        /// share the same timer and several ones share the same channel, which is
        /// itself linked to a timer.
        /// See ... for the association between PWM pins, timers and channels.
        /// When configuring a PWM channel, it might affect several PWM outputs.
        /// </remarks>
        /// <param name="timer">the timer index (0 .. n, depending on the board)</param>
        /// <param name="channel">the channel index (0 .. n, depending on the board and the timer)</param>
        /// <param name="attributes">PWM channel attributes such as high or low puleses</param>
        public void ConfigurePWMChannel(int timer, int channel, PWMChannelAttributes attributes)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionConfigModule,
                PortType = Message.ConfigModulePWMChannel,
                PinConfig = (UInt16)timer,
                PortAttributes1 = (UInt16)attributes,
                Value1 = (UInt16)channel
            };

            SendConfigRequest(request);
        }


        /// <summary>
        /// Sets the duty cycle of a PWM output
        /// </summary>
        /// <param name="port">the PWM output's port ID</param>
        /// <param name="dutyCycle">the duty cycle between 0.0 (for 0%) and 1.0 (for 100%)</param>
        public void WritePWMPin(int port, double dutyCycle)
        {
            PortRequest request = new PortRequest
            {
                PortId = (UInt16)port,
                Action = Message.PortActionSetValue,
                Value1 = (UInt32)(dutyCycle * 2147483647 + 0.5)
            };

            SubmitPortRequest(request);
        }
    }
}
