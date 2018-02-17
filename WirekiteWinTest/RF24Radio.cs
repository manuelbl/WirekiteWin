/*
 * Wirekite for Windows 
 * Copyright (c) 2017 Manuel Bleichenbacher
 * Licensed under MIT License
 * https://opensource.org/licenses/MIT
 */

using Codecrete.Wirekite.Device;
using System;
using System.Diagnostics;
using System.Threading;

namespace Codecrete.Wirekite.Test.UI
{
    /// <summary>
    /// RF transceiver with NRF24L01(+) chip
    /// </summary>
    public class RF24Radio
    {
        public enum RFDataRate
        {
            _1mbps,
            _2mbps,
            _250kbps
        }

        public enum RFOutputPower
        {
            Min = 0,
            Low = 1,
            High = 2,
            Max = 3
        }

        public delegate void PacketReadCallback(RF24Radio radio, int pipe, byte[] packet);

        private const byte SetContrast = 0x81;
        private const byte OutputRAMToDisplay = 0xA4;
        private const byte SetDisplayOn = 0xA5;
        private const byte SetNormalDisplay = 0xA6;
        private const byte SetInvertedDisplay = 0xA7;
        private const byte DisplayOff = 0xAE;
        private const byte DisplayOn = 0xAF;
        private const byte SetDisplayOffset = 0xD3;
        private const byte SetComPin = 0xDA;
        private const byte SetVCOMH = 0xDB;
        private const byte SetClockDivideRatio = 0xD5;
        private const byte SetPrecharge = 0xD9;
        private const byte SetMultiplexRatio = 0xA8;
        private const byte SetColumnAddressLow = 0x00;
        private const byte SetColumnAddressHigh = 0x10;
        private const byte SetPageAddress = 0xb0;
        private const byte SetStartLineBase = 0x40;
        private const byte PageAddressingMode = 0x20;
        private const byte ScanDirectionIncreasing = 0xC0;
        private const byte ScanDirectionDecreasing = 0xC8;
        private const byte SegmentRampBase = 0xA0;
        private const byte ChargePump = 0x8D;
        private const byte DeactivateScroll = 0x2E;

        private WirekiteDevice device;
        private int spiPort;
        private int cePort;
        private int csnPort;
        private int irqPort = WirekiteDevice.InvalidPortId;

        // shadow registers
        private byte regConfig = RF24.CONFIG.EN_CRC;
        private byte regSetupRetr = 3;
        private byte regRFSetup = RF24.RF_SETUP.RF_DR_HIGH | (3 << 1);
        private byte regSetupAW = 3;
        private byte regRFCh = 2;
        private byte regEnAA = 0x3f;
        private byte regEnRXAddr = 3;
        private byte regFeature = 0;

        private bool isPlusModel = false;
        private bool dynamicPayloadEnabled = true;
        private int payloadSize = 32;
        private UInt64 pipe0ReadingAddress = 0;
        private object irqLock = new object();
        private int txQueueCount = 0;
        private object txQueueLock = new object();

        private PacketReadCallback packetReadCallback;
        private int expectedPayloadSize = 32;


        public RF24Radio(WirekiteDevice device, int spiPort, int cePin, int csnPin)
        {
            this.device = device;
            this.spiPort = spiPort;
            cePort = device.ConfigureDigitalOutputPin(cePin, DigitalOutputPinAttributes.Default, false);
            csnPort = device.ConfigureDigitalOutputPin(csnPin, DigitalOutputPinAttributes.Default, true);
        }


        ~RF24Radio()
        {
            device.ReleaseDigitalPin(cePort);
            device.ReleaseDigitalPin(csnPort);
            if (irqPort != WirekiteDevice.InvalidPortId)
                device.ReleaseDigitalPin(irqPort);
        }


        public void InitModule()
        {
            DebugRegisters();

            // Reset CONFIG and enable 16-bit CRC.
            regConfig = RF24.CONFIG.EN_CRC | RF24.CONFIG.CRCO;
            WriteRegister(regConfig, Register.CONFIG);

            SetRetransmissions(15, 5);

            // check for connected module and if this is a p nRF24l01 variant
            DataRate = RFDataRate._250kbps;
            isPlusModel = GetDataRate(ReadRegister(Register.RF_SETUP)) == RFDataRate._250kbps;

            // Default speed
            DataRate = RFDataRate._1mbps;

            // Disable dynamic payloads, to match dynamic_payloads_enabled setting - Reset value is 0
            ToggleFeatures();
            regFeature = 0;
            WriteRegister(regFeature, Register.FEATURE);
            WriteRegister(0, Register.DYNPD);
            dynamicPayloadEnabled = false;

            // Reset current status
            // Notice reset and flush is the last thing we do
            WriteRegister(RF24.STATUS.RX_DR | RF24.STATUS.TX_DS | RF24.STATUS.MAX_RT, Register.STATUS);

            // Set up default configuration.  Callers can always change it later.
            // This channel should be universally safe and not bleed over into adjacent
            // spectrum.
            RFChannel = 76;

            // Flush buffers
            DiscardReceivedPackets();
            DiscardQueuedTransmitPackets();

            PowerUp(); // Power up by default when begin() is called

            // Enable PTX, do not write CE high so radio will remain in standby I mode
            // (130us max to transition to RX or TX instead of 1500us from powerUp)
            // PTX should use only 22uA of power
            regConfig = (byte)(regConfig & ~RF24.CONFIG.PRIM_RX);
            WriteRegister(regConfig, Register.CONFIG);

            DebugRegisters();
        }


        #region Configuration


        public void ConfigureIRQPin(int irqPin, int payloadSize, PacketReadCallback callback)
        {
            packetReadCallback = callback;
            expectedPayloadSize = payloadSize;
            irqPort = device.ConfigureDigitalInputPin(irqPin, DigitalInputPinAttributes.TriggerFalling, IrqPinTriggered);
        }

        public int AddressWidth
        {
            get
            {
                return regSetupAW + 2;
            }
            set
            {
                regSetupAW = (byte)(Clamp(value, 3, 5) - 2);
                WriteRegister(regSetupAW, Register.SETUP_AW);
            }
        }

        public int PayloadSize
        {
            get
            {
                return payloadSize;
            }
            set
            {
                payloadSize = Clamp(value, 1, 32);
            }
        }

        public void GetRetransmissions(out int count, out int delay)
        {
            count = regSetupRetr & 0x0f;
            delay = ((regSetupRetr >> 4) - 1) *250;
        }

        public void SetRetransmissions(int count, int delay)
        {
            int delayCode = Clamp((delay + 124) / 250 - 1, 0, 15);
            int retransmissions = Clamp(count, 0, 15);
            regSetupRetr = (byte)((delayCode << 4) | retransmissions);
            WriteRegister(regSetupRetr, Register.SETUP_RETR);
        }

        public RFDataRate DataRate
        {
            get
            {
                return GetDataRate(regRFSetup);
            }
            set
            {
                regRFSetup = (byte)(regRFSetup & ~(RF24.RF_SETUP.RF_DR_LOW | RF24.RF_SETUP.RF_DR_HIGH));
                regRFSetup = (byte)(value == RFDataRate._250kbps ? RF24.RF_SETUP.RF_DR_LOW : (value == RFDataRate._2mbps ? RF24.RF_SETUP.RF_DR_HIGH : 0));
                WriteRegister(regRFSetup, Register.RF_SETUP);
            }
        }

        private RFDataRate GetDataRate(byte regValue)
        {
            RFDataRate rate;
            if ((regValue & RF24.RF_SETUP.RF_DR_LOW) != 0) {
                rate = RFDataRate._250kbps;
            } else if ((regValue & RF24.RF_SETUP.RF_DR_HIGH) != 0) {
                rate = RFDataRate._2mbps;
            } else {
                rate = RFDataRate._1mbps;
            }
            return rate;
        }

        public RFOutputPower OutputPower
        {
            get
            {
                int value = (regRFSetup >> 1) & 0x03;
                return (RFOutputPower)value;
            }
            set
            {
                regRFSetup = (byte)(regRFSetup & ~RF24.RF_SETUP.RF_PWR_MASK);
                regRFSetup |= (byte)((int)value << 1);
                WriteRegister(regRFSetup, Register.RF_SETUP);
            }
        }

        public byte StatusRegister
        {
            get
            {
                byte[] data = device.RequestOnSPIPort(spiPort, 1, csnPort, RF24.CMD.NOP);
                return data[0];
            }
        }

        public bool IsConnected
        {
            get
            {
                byte value = ReadRegister(Register.SETUP_AW);
                return value >= 1 && value <= 3;
            }
        }

        public int RFChannel
        {
            get
            {
                return regRFCh;
            }
            set
            {
                if (value >= 0 && value <= 125)
                {
                    regRFCh = (byte)value;
                    WriteRegister(regRFCh, Register.RF_CH);
                }
            }
        }

        public bool AutoAck
        {
            get
            {
                return regEnAA == 0x3f;
            }
            set
            {
                regEnAA = value ? (byte)0x3f : (byte)0;
                WriteRegister(regEnAA, Register.EN_AA);
            }
        }

        public bool IsPlusModel
        {
            get
            {
                return isPlusModel;
            }
        }

        public void PowerDown()
        {
            if ((regConfig & RF24.CONFIG.PWR_UP) == 0)
                return;

            SetCE(false);
            regConfig = (byte)(regConfig & ~RF24.CONFIG.PWR_UP);
            WriteRegister(regConfig, Register.CONFIG);
        }

        public void PowerUp()
        {
            if ((regConfig & RF24.CONFIG.PWR_UP) != 0)
                return;

            regConfig |= RF24.CONFIG.PWR_UP;
            WriteRegister(regConfig, Register.CONFIG);
            Thread.Sleep(5);
        }


        #endregion


        #region Receiving

        public void OpenReceivePipe(int pipe, UInt64 address)
        {
            if (pipe < 0 || pipe > 6)
                return;

            if (pipe == 0)
                pipe0ReadingAddress = address;

            Register addrRegister = RegisterOffset(Register.RX_ADDR_P0, pipe);
            if (pipe < 2)
            {
                WriteAddress(address, addrRegister);
            }
            else
            {
                WriteRegister((byte)(address & 0xff), addrRegister);
            }

            Register payloadRegister = RegisterOffset(Register.RX_PW_P0, pipe);
            WriteRegister((byte)payloadSize, payloadRegister);

            regEnRXAddr |= (byte)(1 << pipe);
            WriteRegister(regEnRXAddr, Register.EN_RXADDR);
        }


        public void CloseReceivePipe(int pipe)
        {
            regEnRXAddr = (byte)(regEnRXAddr & ~(1 << pipe));
            WriteRegister(regEnRXAddr, Register.EN_RXADDR);
        }


        public bool IsPacketAvailable
        {
            get
            {
                byte status = ReadRegister(Register.FIFO_STATUS);
                return (status & RF24.FIFO_STATUS.RX_EMPTY) == 0;
            }
        }


        public byte[] FetchPacket(int packetLength)
        {
            byte[] data = ReadQueuedPacket(packetLength);
            WriteRegister(RF24.STATUS.RX_DR, Register.STATUS);
            return data;
        }


        private byte[] ReadQueuedPacket(int numBytes)
        {
            int plSize = Math.Min(numBytes, payloadSize);
            int padSize = dynamicPayloadEnabled ? 0 : payloadSize - plSize;

            byte[] txData = new byte[plSize + padSize + 1];
            txData[0] = RF24.CMD.R_RX_PAYLOAD;
            for (int i = 1; i < txData.Length; i++)
                txData[i] = RF24.CMD.NOP;
            byte[] rxData = TransmitAndRequest(txData);
            byte[] result = new byte[plSize];
            Array.Copy(rxData, 1, result, 0, plSize);
            return result;
        }


        public void DiscardReceivedPackets()
        {
            WriteCommand(RF24.CMD.FLUSH_RX);
        }


        public void StartListening()
        {
            PowerUp();

            lock (txQueueLock)
            {
                while (txQueueCount > 0)
                    Monitor.Wait(txQueueLock);
            }

            regConfig |= RF24.CONFIG.PRIM_RX;
            WriteRegister(regConfig, Register.CONFIG);

            SetCE(true);

            if ((pipe0ReadingAddress & 0xff) != 0)
                WriteAddress(pipe0ReadingAddress, Register.RX_ADDR_P0);
            else
                CloseReceivePipe(pipe: 0);

            if ((regFeature & RF24.FEATURE.EN_ACK_PAY) != 0)
                DiscardQueuedTransmitPackets();
        }


        public void StopListening()
        {
            SetCE(false);

            if ((regFeature & RF24.FEATURE.EN_ACK_PAY) != 0)
                DiscardQueuedTransmitPackets();

            regConfig = (byte)(regConfig & ~RF24.CONFIG.PRIM_RX);
            WriteRegister(regConfig, Register.CONFIG);


            regEnRXAddr |= 1;
            WriteRegister(regEnRXAddr, Register.EN_RXADDR);
        }


        #endregion


        #region Transmission

        public void OpenTransmitPipe(UInt64 address)
        {
            WriteAddress(address, Register.RX_ADDR_P0);
            WriteAddress(address, Register.TX_ADDR);
            WriteRegister((byte)payloadSize, Register.RX_PW_P0);
        }



        public void Transmit(byte[] packet, bool multicast = false)
        {
            lock (txQueueLock)
            {
                while (txQueueCount == 3)
                    Monitor.Wait(txQueueLock);

                if (txQueueCount == 0)
                    SetCE(true);

                QueuePacket(packet, multicast);
                txQueueCount += 1;
            }
        }


        private void QueuePacket(byte[] packet, bool multicast)
        {
            int plSize = Math.Min(packet.Length, payloadSize);
            int padSize = dynamicPayloadEnabled ? 0 : payloadSize - plSize;
            byte[] data = new byte[plSize + padSize + 1];
            data[0] = multicast ? RF24.CMD.W_TX_PAYLOAD_NOACK : RF24.CMD.W_TX_PAYLOAD;
            Array.Copy(packet, 0, data, 1, plSize);
            for (int i = plSize + 1; i < payloadSize - 1; i++)
                data[i] = 0;
            Transmit(data);
        }


        public void DiscardQueuedTransmitPackets()
        {
            WriteCommand(RF24.CMD.FLUSH_TX);
        }


        #endregion


        #region Low Level


        private void IrqPinTriggered(int port, bool value)
        {
            lock (irqLock)
            {
                while (true)
                {
                    byte status = ReadRegister(Register.STATUS);
                    if ((status & RF24.STATUS.RX_DR) != 0)
                    {
                        while (true)
                        {
                            // read packet
                            byte[] data;
                            if (expectedPayloadSize > 0)
                                data = FetchPacket(expectedPayloadSize);
                            else
                                data = null;

                            // callback
                            int pipe = (status >> 1) & 0x07;
                            packetReadCallback(this, pipe, data);

                            // clear RX_DR
                            WriteRegister(RF24.STATUS.TX_DS, Register.STATUS);

                            byte fifoStatus = ReadRegister(Register.FIFO_STATUS);
                            if ((fifoStatus & RF24.FIFO_STATUS.RX_EMPTY) != 0)
                                break;
                        }
                    }
                    else if ((status & RF24.STATUS.TX_DS) != 0)
                    {
                        WriteRegister(RF24.STATUS.TX_DS, Register.STATUS);
                        lock (txQueueLock)
                        {
                            txQueueCount -= 1;
                            if (txQueueCount == 0)
                                SetCE(false);
                            Monitor.Pulse(txQueueCount);
                        }
                    }
                    else if ((status & RF24.STATUS.MAX_RT) != 0)
                    {
                        DiscardQueuedTransmitPackets();
                        WriteRegister(RF24.STATUS.MAX_RT, Register.STATUS);
                        lock (txQueueLock)
                        {
                            Debug.WriteLine("Maximum number of TX retransmissions reached, flushing {0} packets", txQueueCount);
                            txQueueCount = 0;
                            SetCE(false);
                            Monitor.Pulse(txQueueCount);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    


        private byte[] AddressToByteArray(UInt64 address) {
            // convert to byte array, LSB first
            byte[] bytes = new byte[AddressWidth];
            for (int i = 0; i < AddressWidth; i++) {
                bytes[i] = (byte)(address & 0xff);
                address >>= 8;
            }
            return bytes;
        }


        private void ToggleFeatures()
        {
            byte[] data = { 0x50, 0x73 };
            Transmit(data);
        }


        private void WriteRegister(byte value, Register register)
        {
            byte[] data = { WriteCode(register), value };
            Transmit(data);
        }
        
        private void WriteAddress(UInt64 address, Register register)
        {
            byte[] addressBytes = AddressToByteArray(address);
            byte[] data = new byte[addressBytes.Length + 1];
            data[0] = WriteCode(register);
            Array.Copy(addressBytes, 0, data, 1, addressBytes.Length);
            Transmit(data);
        }

        private byte WriteCommand(byte command)
        {
            byte[] txData = { command };
            byte[] rxData = TransmitAndRequest(txData);
            return rxData[0];
        }

        private byte ReadRegister(Register register)
        {
            byte[] txData = { ReadCode(register), RF24.CMD.NOP };
            byte[] rxData = TransmitAndRequest(txData);
            return rxData[1];
        }

        private byte[] ReadAddress(Register register, int length)
        {
            byte[] txData = new byte[length + 1];
            txData[0] = ReadCode(register);
            for (int i = 1; i < length + 1; i++)
                txData[i] = RF24.CMD.NOP;
            byte[] rxData = TransmitAndRequest(txData);
            byte[] result = new byte[length];
            Array.Copy(rxData, 1, result, 0, length);
            return result;
        }

        private byte WriteCode(Register register)
        {
            return (byte)(RF24.CMD.W_REGISTER | (int)register);
        }

        private byte ReadCode(Register register)
        {
            return (byte)(RF24.CMD.R_REGISTER | (int)register);
        }

        private void Transmit(byte[] txData)
        {
            device.TransmitOnSPIPort(spiPort, txData, csnPort);
        }

        private byte[] TransmitAndRequest(byte[] txData)
        {
            return device.TransmitAndRequestOnSPIPort(spiPort, txData, csnPort);
        }
        
        private void SetCE(bool high)
        {
            device.WriteDigitalPinSynchronizedWithSPI(cePort, high, spiPort);
        }


        #endregion



        #region Debugging

        public void DebugRegisters()
        {
            // status
            DebugStatus(StatusRegister);

            // addresses
            DebugAddressRegister(Register.RX_ADDR_P0, AddressWidth);
            DebugAddressRegister(Register.RX_ADDR_P1, AddressWidth);
            DebugByteRegisters(Register.RX_ADDR_P2, 4, "RX_ADDR_P2..5");
            DebugAddressRegister(Register.TX_ADDR, AddressWidth);


            DebugByteRegisters(Register.RX_PW_P0, 6, "RX_PW_P0..5");
            DebugByteRegister(Register.EN_AA);
            DebugByteRegister(Register.EN_RXADDR);
            DebugByteRegister(Register.RF_CH);
            DebugByteRegister(Register.RF_SETUP);
            DebugByteRegister(Register.SETUP_AW);
            DebugByteRegister(Register.CONFIG);
            DebugByteRegister(Register.DYNPD);
            DebugByteRegister(Register.FEATURE);
    }

        private void DebugStatus(byte status)
        {
            Debug.WriteLine("STATUS: RX_DR = {0}, TX_DS = {1}, MAX_RT = {2}, RX_P_NO = {3}, RX_FULL = {4}",
                   (status & RF24.STATUS.RX_DR) == 0 ? 0 : 1,
                   (status & RF24.STATUS.TX_DS) == 0 ? 0 : 1,
                   (status & RF24.STATUS.MAX_RT) == 0 ? 0 : 1,
                   (status & 0x0e) >> 1,
                   (status & RF24.STATUS.TX_FULL) == 0 ? 0 : 1);
        }


        private void DebugByteRegister(Register register)
        {
            string valueStr = ReadRegister(register).ToString("X2");
            Debug.WriteLine("{0}: {1}", register.ToString(), valueStr);
        }

        private void DebugByteRegisters(Register register, int count, string label)
        {
            string dataStr = "";
            for (int i = 0; i < count; i++)
            {
                byte value = ReadRegister(RegisterOffset(register, i));
                dataStr += " " + value.ToString("X2");
            }
            Debug.WriteLine("{0}: {1}", label, dataStr);
        }


        private void DebugAddressRegister(Register register, int addressLength)
        {
            byte[] address = ReadAddress(register, addressLength);
            string addressStr = "";
            for (int i = 0; i < addressLength; i++)
            {
                addressStr += address[addressLength - i - 1].ToString("X2");
            }
            Debug.WriteLine("{0}: {1}", register.ToString(), addressStr);
        }


        private void DebugAddressRegisters(Register register, int length = 1)
        {
            string dataStr = "";
            for (int i = 0; i < length; i++)
            {
                byte value = ReadRegister(RegisterOffset(register, i));
                dataStr += " " + value.ToString("X2");
            }
            Debug.WriteLine("{0}:{1}", register.ToString(), dataStr);
        }

        #endregion


        private static int Clamp(int value, int minValue, int maxValue)
        {
            if (value < minValue)
                return minValue;
            if (value > maxValue)
                return maxValue;
            return value;
        }

        private static Register RegisterOffset(Register register, int offset)
        {
            return (Register)((int)register + offset);
        }

        internal enum Register
        {
            CONFIG = 0x00,
            EN_AA = 0x01,
            EN_RXADDR = 0x02,
            SETUP_AW = 0x03,
            SETUP_RETR = 0x04,
            RF_CH = 0x05,
            RF_SETUP = 0x06,
            STATUS = 0x07,
            OBSERVE_TX = 0x08,
            RPD = 0x09,
            RX_ADDR_P0 = 0x0A,
            RX_ADDR_P1 = 0x0B,
            RX_ADDR_P2 = 0x0C,
            RX_ADDR_P3 = 0x0D,
            RX_ADDR_P4 = 0x0E,
            RX_ADDR_P5 = 0x0F,
            TX_ADDR = 0x10,
            RX_PW_P0 = 0x11,
            RX_PW_P1 = 0x12,
            RX_PW_P2 = 0x13,
            RX_PW_P3 = 0x14,
            RX_PW_P4 = 0x15,
            RX_PW_P5 = 0x16,
            FIFO_STATUS = 0x17,
            DYNPD = 0x1C,
            FEATURE = 0x1D
        }

        internal class RF24
        {
            internal class CONFIG
            {
                internal const byte MASK_RX_DR = 0x40;
                internal const byte MASK_TX_DS = 0x20;
                internal const byte MASK_MAX_RT = 0x10;
                internal const byte EN_CRC = 0x08;
                internal const byte CRCO = 0x04;
                internal const byte PWR_UP = 0x02;
                internal const byte PRIM_RX = 0x01;
            }
            internal class RF_SETUP
            {
                internal const byte CONT_WAVE = 0x80;
                internal const byte RF_DR_LOW = 0x20;
                internal const byte PLL_LOCK = 0x10;
                internal const byte RF_DR_HIGH = 0x08;
                internal const byte RF_PWR_MASK = 0x06;
            }
            internal class STATUS
            {
                internal const byte RX_DR = 0x40;
                internal const byte TX_DS = 0x20;
                internal const byte MAX_RT = 0x10;
                internal const byte TX_FULL = 0x01;
            }
            internal class FEATURE
            {
                internal const byte EN_DPL = 0x04;
                internal const byte EN_ACK_PAY = 0x02;
                internal const byte EN_DYN_ACK = 0x01;
            }
            internal class FIFO_STATUS
            {
                internal const byte RX_REUSE = 0x40;
                internal const byte TX_FULL = 0x20;
                internal const byte TX_EMPTY = 0x10;
                internal const byte RX_FULL = 0x02;
                internal const byte RX_EMPTY = 0x01;
            }
            internal class CMD
            {
                internal const byte R_REGISTER = 0x00;
                internal const byte W_REGISTER = 0x20;
                internal const byte R_RX_PAYLOAD = 0x61;
                internal const byte W_TX_PAYLOAD = 0xA0;
                internal const byte FLUSH_TX = 0xE1;
                internal const byte FLUSH_RX = 0xE2;
                internal const byte REUSE_TX_PL = 0xE3;
                internal const byte R_RX_PL_WID = 0x60;
                internal const byte W_ACK_PAYLOAD = 0xA8;
                internal const byte W_TX_PAYLOAD_NOACK = 0xB0;
                internal const byte NOP = 0xFF;
            }
        }
    }
}
