/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Codecrete.Wirekite.Device.Messages;
using System;


namespace Codecrete.Wirekite.Device
{

    /// <summary>
    /// I2C SCL/SDA pin pairs
    /// </summary>
    public enum I2CPins
    {
        /// <summary> SCL/SDA pin pair 16/17 for I2C module 0 </summary>
        I2CPinsSCL16_SDA17 = 0,
        /// <summary> SCL/SDA pin pair 19/18 for I2C module 0 </summary>
        I2CPinsSCL19_SDA18 = 1,
        /// <summary> SCL/SDA pin pair 22/23 for I2C module 1 </summary>
        I2CPinsSCL22_SDA23 = 2
    };

    /// <summary>
    /// Result code for I2C send and receive transactions
    /// </summary>
    public enum I2CResult
    {
        /// <summary> Action was successful </summary>
        OK = 0,
        /// <summary> Action timed out </summary>
        Timeout = 1,
        /// <summary> Action was cancelled due to a lost bus arbitration </summary>
        ArbitrationLost = 2,
        /// <summary> Slave address was not acknowledged </summary>
        AddressNAK = 3,
        /// <summary> Transmitted data was not acknowledged </summary>
        DataNAK = 4,
        /// <summary> Invalid parameters were specified </summary>
        InvalidParameter = 5,
        /// <summary> Insufficient memory in Wirekite device to process transaction</summary>
        OutOfMemory = 6
    };

    public partial class WirekiteDevice
    {
        /// <summary>
        /// Configures an I2C port as a master
        /// </summary>
        /// <remarks>
        /// Each pin pair belongs to a specific I2C module. A single module can only
        /// be connected to a single pin pair at a time.
        /// </remarks>
        /// <param name="pins">the SCL/SDA pin pair for the port</param>
        /// <param name="frequency">the frequency of for the I2C communication (in Hz). If in doubt, use 100,000 Hz.</param>
        /// <returns>the I2C port ID</returns>
        public UInt16 ConfigureI2CMaster(I2CPins pins, int frequency)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionConfigPort,
                PortType = Message.PortTypeI2C,
                PinConfig = (UInt16)pins,
                Value1 = (UInt32)frequency
            };
            ConfigResponse response = SendConfigRequest(request);
            Port port = new Port(response.PortId, PortType.I2CPort, 10);
            _ports.AddPort(port);
            return port.Id;
        }

        /// <summary>
        /// Releases the I2C port.
        /// </summary>
        /// <param name="port">the I2C port ID</param>
        public void ReleaseI2CPort(UInt16 port)
        {
            ConfigRequest request = new ConfigRequest
            {
                Action = Message.ConfigActionRelease,
                PortId = port
            };

            SendConfigRequest(request);
            Port p = _ports.GetPort(port);
            if (p != null)
                p.Dispose();
            _ports.RemovePort(port);
        }

        
        /// <summary>
        /// Result code of the last send or receive
        /// </summary>
        /// <param name="port">the I2C port ID</param>
        /// <returns>the result code of the last operation on this port</returns>
        public I2CResult GetLastI2CResult(UInt16 port)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                return I2CResult.InvalidParameter;

            return (I2CResult)p.LastSample;
        }


        /// <summary>
        /// Send data to an I2C slave
        /// </summary>
        /// <remarks>
        /// <para>
        /// The operation performs a complete I2C transaction, starting with a START condition
        /// and ending with a STOP condition
        /// </para>
        /// <para>
        /// The request is executed sychnronously, i.e. the call blocks until the data
        /// has been transmitted or the transmission has failed.
        /// </para>
        /// <para>
        /// If less than the specified number of bytes are transmitted,
        /// <see cref="GetLastI2CResult(ushort)"/> returns the associated reason.
        /// </para>
        /// </remarks>
        /// <param name="port">the I2C port ID</param>
        /// <param name="data">the data to transmit</param>
        /// <param name="slave">the slave address</param>
        /// <returns>the number of sent bytes</returns>
        public int SendOnI2CPort(UInt16 port, byte[] data, UInt16 slave)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", port));

            UInt16 requestId = _ports.NextRequestId();
            SubmitI2CTx(port, data, slave, requestId);

            PortEvent response = _pendingResponses.WaitForResponse(requestId) as PortEvent;
            p.LastSample = response.EventAttribute1; // status code
            return response.EventAttribute2;
        }


        /// <summary>
        /// Submits data to be sent to an I2C slave
        /// </summary>
        /// <remarks>
        /// <para>
        /// The operation performs a complete I2C transaction, starting with a START condition
        /// and ending with a STOP condition
        /// </para>
        /// <para>
        /// The request is executed asychnronously, i.e. the call returns immediately. If the
        /// transaction fails, no error is reported.
        /// </para>
        /// </remarks>
        /// <param name="port">the I2C port ID</param>
        /// <param name="data">the data to transmit</param>
        /// <param name="slave">the slave address</param>
        public void SubmitOnI2CPort(UInt16 port, byte[] data, UInt16 slave)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", port));

            SubmitI2CTx(port, data, slave, 0);
        }


        /// <summary>
        /// Request data from an I2C slave
        /// </summary>
        /// <remarks>
        /// <para>
        /// The operation performs a complete I2C transaction, starting with a START condition
        /// and ending with a STOP condition.
        /// </para>
        /// <para>
        /// The operation is executed sychnronously, i.e. the call blocks until the
        /// transaction has been completed or has failed.If the transaction fails,
        /// use <see cref="GetLastI2CResult(ushort)"/> to retrieve the reason
        /// </para>
        /// </remarks>
        /// <param name="port">the I2C port ID</param>
        /// <param name="slave">the slave address</param>
        /// <param name="receiveLength">the number of bytes of data requested from the slave</param>
        /// <returns>the received data or <c>null</c> if it fails</returns>
        public byte[] RequestDataOnI2CPort(UInt16 port, UInt16 slave, UInt16 receiveLength)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", port));

            UInt16 requestId = _ports.NextRequestId();
            PortRequest request = new PortRequest
            {
                PortId = port,
                Action = Message.PortActionRxData,
                ActionAttribute2 = slave,
                RequestId = requestId,
                Value1 = receiveLength
            };

            WriteMessage(request);

            PortEvent response = _pendingResponses.WaitForResponse(requestId) as PortEvent;
            p.LastSample = response.EventAttribute1; // status code
            return response.Data;
        }


        /// <summary>
        /// Send data to and request data from an I2C slave in a single operation
        /// </summary>
        /// <remarks>
        /// <para>
        /// The operation performs a complete I2C transaction, starting with a START condition,
        /// a RESTART condition when switching from transmission to receipt, and ending with
        /// a STOP condition.
        /// </para>
        /// <para>
        /// The request is executed sychnronously, i.e. the call blocks until the data
        /// has been transmitted and received, or the transmission has failed.
        /// </para>
        /// <para>
        /// If less than the specified number of bytes are transmitted, `nil` is returned and
        /// <see cref="GetLastI2CResult(ushort)"/> returns the associated reason.
        /// </para>
        /// </remarks>
        /// <param name="port">the I2C port ID</param>
        /// <param name="data">the data to transmit</param>
        /// <param name="slave">the slave address</param>
        /// <param name="receiveLength">the number of bytes of data request from the slave</param>
        /// <returns>the received data or <c>null</c> if the transaction fails</returns>
        public byte[] SendAndRequestOnI2CPort(UInt16 port, byte[] data, UInt16 slave, UInt16 receiveLength)
        {
            Port p = _ports.GetPort(port);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", port));

            UInt16 requestId = _ports.NextRequestId();
            PortRequest request = new PortRequest
            {
                PortId = port,
                Action = Message.PortActionTxNRxData,
                Data = data,
                ActionAttribute2 = slave,
                RequestId = requestId,
                Value1 = receiveLength
            };

            WriteMessage(request);

            PortEvent response = _pendingResponses.WaitForResponse(requestId) as PortEvent;
            p.LastSample = response.EventAttribute1; // status code
            return response.Data;
        }

        private void SubmitI2CTx(UInt16 port, byte[] data, UInt16 slave, UInt16 requestId)
        {
            PortRequest request = new PortRequest
            {
                PortId = port,
                Action = Message.PortActionTxData,
                Data = data,
                ActionAttribute2 = slave,
                RequestId = requestId
            };

            WriteMessage(request);
        }


        private void HandleI2CEvent(PortEvent evt)
        {
            Port p = _ports.GetPort(evt.PortId);
            if (p == null)
                throw new WirekiteException(String.Format("Invalid port ID {0}", evt.PortId));

            if (evt.RequestId != 0)
                _pendingResponses.PutResponse(evt.RequestId, evt);
        }

    }
}

