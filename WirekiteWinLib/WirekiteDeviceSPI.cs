/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Codecrete.Wirekite.Device.Messages;
using Codecrete.Wirekite.Device.USB;
using System;


namespace Codecrete.Wirekite.Device
{

    /// <summary>
    /// Additional features of digital output pins
    /// </summary>
    [Flags]
    public enum SPIAttributes
    {
        /// <summary>
        /// Default. No special features enabled.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Transmit/receive most significant bit (MSB) first.
        /// </summary>
        MSBFirst = 0,
        /// <summary>
        /// Transmit/receive least significant bit (LSB) first.
        /// </summary>
        LSBFirst = 1,
        /// <summary>
        /// Transmit/receive in SPI mode 0 (CPOL = 0 / clock idles in low / CPHA = 0 / "out" changes on trailing clock edge / "in" is cpatured on leading clock edge).
        /// </summary>
        SPIMode0 = 0,
        /// <summary>
        /// Transmit/receive in SPI mode 1 (CPOL = 0 / clock idles in low / CPHA = 1 / "out" changes on leading clock edge / "in" is cpatured on trailing clock edge).
        /// </summary>
        SPIMode1 = 4,
        /// <summary>
        /// Transmit/receive in SPI mode 2 (CPOL = 1 / clock idles in high / CPHA = 0 / "out" changes on trailing clock edge / "in" is cpatured on leading clock edge).
        /// </summary>
        SPIMode2 = 8,
        /// <summary>
        /// Transmit/receive in SPI mode 3 (CPOL = 1 / clock idles in high / CPHA = 1 / "out" changes on leading clock edge / "in" is cpatured on trailing clock edge).
        /// </summary>
        SPIMode3 = 16
    }


    /// <summary>
    /// Result code for SPI send and receive transactions
    /// </summary>
    public enum SPIResult
    {
        /// <summary> Action was successful </summary>
        OK = 0,
        /// <summary> Action timed out </summary>
        Timeout = 1,
        /// <summary> Unknown error occurred </summary>
        Unknwon = 7,
        /// <summary> Invalid parameters were specified </summary>
        InvalidParameter = 8
    };

    public partial class WirekiteDevice
    {
        /// <summary>
        /// Configures a SPI port as a master
        /// </summary>
        /// <remarks>
        ///  The pins are specified with the index as printed on the Teensy board.
        /// The MISO pin is optional and can be ommitted if there is no communication from
        /// the slave to the master.
        /// </remarks>
        /// <param name="sckPin">the index of the pin to use for the SCK signal (serial clock).</param>
        /// <param name="mosiPin">the index of the pin to use for the MOSI signal (master out - slave in).</param>
        /// <param name="misoPin">the index of the pin to use for the MISO signal (master in - slave out) or -1 if not used.</param>
        /// <param name="frequency">the frequency for the SPI communication (in Hz). If in doubt, use 100,000 Hz.</param>
        /// <param name="attributes">additional settings of the SPI bus.</param>
        /// <returns>the I2C port ID</returns>
        public int ConfigureSPIMaster(int sckPin, int mosiPin, int misoPin, int frequency, SPIAttributes attributes)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionConfigPort,
                PortType = Message.PortTypeSPI,
                PinConfig = (UInt16)((sckPin & 0xff) | ((mosiPin & 0xff) << 8)),
                PortAttributes1 = (UInt16)attributes,
                PortAttributes2 = (UInt16)(misoPin & 0xff),
                Value1 = (UInt32)frequency
            };
            ConfigResponse response = SendConfigRequest(request);
            Port port = new Port(response.PortId, PortType.SPIPort, 10);
            _ports.AddPort(port);
            return port.Id;
        }

        /// <summary>
        /// Releases the SPI port.
        /// </summary>
        /// <param name="port">the SPI port ID</param>
        public void ReleaseSPIPort(int port)
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
        /// Result code of the last transmission or receipt
        /// </summary>
        /// <param name="port">the SPI port ID</param>
        /// <returns>the result code of the last operation on this port</returns>
        public SPIResult GetLastSPIResult(int port)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                return SPIResult.InvalidParameter;

            return (SPIResult)p.LastSample;
        }


        /// <summary>
        /// Transmit data to a SPI slave
        /// </summary>
        /// <remarks>
        /// <para>
        /// The operation performs a complete SPI transaction, i.e. enables the clock for the duration of
        /// transation and transmits the data.Optionally, a digital output can be used as the chip select(CS),
        /// which is then held low for the duration of the transaction and set to high at the end of the transaction.
        /// </para>
        /// <para>
        /// The request is executed sychnronously, i.e. the call blocks until the data
        /// has been transmitted or the transmission has failed.
        /// </para>
        /// <para>
        /// If less than the specified number of bytes are transmitted,
        /// <see cref="GetLastSPIResult(int)"/> returns the associated reason.
        /// </para>
        /// </remarks>
        /// <param name="port">the SPI port ID</param>
        /// <param name="data">the data to transmit</param>
        /// <param name="chipSelect">the digital output port ID to use as chip select (or <see cref="InvalidPort"/> if not used)</param>
        /// <returns>the number of sent bytes</returns>
        public int TransmitOnSPIPort(int port, byte[] data, int chipSelect)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", port));

            UInt16 requestId = SubmitSPITx(port, data, chipSelect);

            PortEvent response = _pendingRequests.WaitForResponse(requestId) as PortEvent;
            p.LastSample = response.EventAttribute1; // status code
            return response.EventAttribute2;
        }


        /// <summary>
        /// Submits data to be transmitted to a SPI slave
        /// </summary>
        /// <remarks>
        /// <para>
        /// The operation performs a complete SPI transaction, i.e. enables the clock for the duration of
        /// transation and transmits the data.Optionally, a digital output can be used as the chip select(CS),
        /// which is then held low for the duration of the transaction and set to high at the end of the transaction.
        /// </para>
        /// <para>
        /// The request is executed asychnronously, i.e. the call returns immediately. If the
        /// transaction fails, a message appears in the log.
        /// </para>
        /// </remarks>
        /// <param name="port">the SPI port ID</param>
        /// <param name="data">the data to transmit</param>
        /// <param name="chipSelect">the digital output port ID to use as chip select (or <see cref="InvalidPort"/> if not used)</param>
        public void SubmitOnSPIPort(int port, byte[] data, int chipSelect)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", port));

            SubmitSPITx(port, data, chipSelect);
        }


        private UInt16 SubmitSPITx(int port, byte[] data, int chipSelect)
        {
            PortRequest request = new PortRequest
            {
                PortId = (UInt16)port,
                Action = Message.PortActionTxData,
                Data = data,
                ActionAttribute2 = (UInt16)chipSelect,
                RequestId = _ports.NextRequestId()
            };

            WaitUntilAvailable(request);
            WriteMessage(request);
            return request.RequestId;
        }


        private void HandleSPIEvent(PortEvent evt)
        {
            Port p = _ports.GetPort(evt.PortId);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", evt.PortId));

            if (evt.RequestId != 0)
            {
                _throttler.RequestCompleted(evt.RequestId);
                _pendingRequests.PutResponse(evt.RequestId, evt);
            }
        }

    }
}
