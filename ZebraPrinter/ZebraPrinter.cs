using SerialHandler;
using SocketClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ZebraPrinter
{
    /// <summary>
    /// Zebra Printer Ethernet / RS232 통신 클래스
    /// </summary>
    public class ZebraPrinter : IDisposable
    {
        private readonly ZebraPrinterOptions _options;

        private clsSocketClient _socketClient;
        private SerialComPort _serialPort;

        private const string STATUS_COMMAND = "~HS";
        private bool _disposed;

        /// <summary>
        /// 현재 프린터 연결 상태
        /// </summary>
        public bool IsConnected
        {
            get
            {
                switch (_options.ConnectionType)
                {
                    case ZebraConnectionType.Ethernet:
                        return _socketClient != null &&
                               _socketClient.IsConnected;

                    case ZebraConnectionType.Serial:
                        return _serialPort != null &&
                               _serialPort.IsOpen;

                    default:
                        return false;
                }
            }
        }

        public ZebraPrinter(ZebraPrinterOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options;
        }

        /// <summary>
        /// 설정된 통신 방식으로 프린터 연결
        /// </summary>
        public bool Connect()
        {
            ThrowIfDisposed();

            Disconnect();

            switch (_options.ConnectionType)
            {
                case ZebraConnectionType.Ethernet:
                    return ConnectEthernet();

                case ZebraConnectionType.Serial:
                    return ConnectSerial();

                default:
                    return false;
            }
        }

        /// <summary>
        /// 프린터 연결 해제
        /// </summary>
        public void Disconnect()
        {
            DisconnectEthernet();
            DisconnectSerial();
        }

        private bool ConnectEthernet()
        {
            if (string.IsNullOrWhiteSpace(_options.IpAddress))
                return false;

            try
            {
                _socketClient = new clsSocketClient(
                    _options.IpAddress,
                    _options.Port);

                _socketClient.ReceiveTimeout =
                    _options.ReceiveTimeout;

                if (!_socketClient.SocketConnect())
                {
                    _socketClient.Dispose();
                    _socketClient = null;

                    return false;
                }

                return true;
            }
            catch
            {
                DisconnectEthernet();
                return false;
            }
        }

        private bool ConnectSerial()
        {
            if (string.IsNullOrWhiteSpace(_options.PortName))
                return false;

            try
            {
                _serialPort = new SerialComPort();

                _serialPort.Name = _options.PortName;
                _serialPort.BaudRate = _options.BaudRate;
                _serialPort.DataBit = _options.DataBits;

                _serialPort.Paritys = GetSerialParity(_options.Parity);
                _serialPort.StopBit = GetSerialStopBits(_options.StopBits);

                // Zebra 상태 조회는 동기 DataRead() 방식으로 처리할 예정
                _serialPort.AutoReadEvent = false;

                // ~HS 자체가 STX/ETX 패킷을 반환하지만,
                // ZebraPrinter에서 전체 응답을 직접 조립/파싱할 예정이므로
                // SerialComPort의 STX/ETX 자동 프레임 기능은 사용하지 않음
                _serialPort.STXETX = false;

                if (!_serialPort.PortOpen())
                {
                    _serialPort.Dispose();
                    _serialPort = null;

                    return false;
                }

                return true;
            }
            catch
            {
                DisconnectSerial();
                return false;
            }
        }

        private int GetSerialParity(System.IO.Ports.Parity parity)
        {
            switch (parity)
            {
                case System.IO.Ports.Parity.Odd:
                    return 1;

                case System.IO.Ports.Parity.Even:
                    return 2;

                case System.IO.Ports.Parity.Mark:
                    return 3;

                case System.IO.Ports.Parity.Space:
                    return 4;

                case System.IO.Ports.Parity.None:
                default:
                    return 0;
            }
        }

        private int GetSerialStopBits(System.IO.Ports.StopBits stopBits)
        {
            switch (stopBits)
            {
                case System.IO.Ports.StopBits.Two:
                    return 2;

                case System.IO.Ports.StopBits.OnePointFive:
                    return 3;

                case System.IO.Ports.StopBits.One:
                default:
                    return 1;
            }
        }

        private void DisconnectEthernet()
        {
            if (_socketClient == null)
                return;

            try
            {
                _socketClient.SocketDisconnect();
            }
            catch
            {
            }
            finally
            {
                try
                {
                    _socketClient.Dispose();
                }
                catch
                {
                }

                _socketClient = null;
            }
        }

        private void DisconnectSerial()
        {
            if (_serialPort == null)
                return;

            try
            {
                _serialPort.PortClose();
            }
            catch
            {
            }
            finally
            {
                try
                {
                    _serialPort.Dispose();
                }
                catch
                {
                }

                _serialPort = null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Disconnect();

            _disposed = true;

            GC.SuppressFinalize(this);
        }

        #region 송/수신 메서드 
        /// <summary>
        /// 프린터로 문자열 데이터 전송
        /// </summary>
        public bool Send(string data)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(data))
                return false;

            if (!IsConnected)
                return false;

            switch (_options.ConnectionType)
            {
                case ZebraConnectionType.Ethernet:
                    return SendEthernet(data);

                case ZebraConnectionType.Serial:
                    return SendSerial(data);

                default:
                    return false;
            }
        }

        private bool SendEthernet(string data)
        {
            if (_socketClient == null ||
                !_socketClient.IsConnected)
            {
                return false;
            }

            return _socketClient.SendData(data);
        }

        private bool SendSerial(string data)
        {
            if (_serialPort == null ||
                !_serialPort.IsOpen)
            {
                return false;
            }

            try
            {
                return _serialPort.DataSend(data);
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 상태체크
        public ZebraPrinterStatus GetStatus()
        {
            ThrowIfDisposed();

            switch (_options.ConnectionType)
            {
                case ZebraConnectionType.Ethernet:
                    return GetEthernetStatus();

                case ZebraConnectionType.Serial:
                    return GetSerialStatus();

                default:
                    return CreateErrorStatus("지원하지 않는 통신 방식입니다.");
            }
        }

        private ZebraPrinterStatus GetEthernetStatus()
        {
            if (_socketClient == null ||
                !_socketClient.IsConnected)
            {
                return CreateErrorStatus(
                    "프린터가 연결되어 있지 않습니다.");
            }

            int oldReceiveTimeout = _socketClient.ReceiveTimeout;

            try
            {
                if (!_socketClient.SendData(STATUS_COMMAND))
                {
                    return CreateErrorStatus(
                        _socketClient.ERROR_MESSAGE);
                }

                StringBuilder rawBuilder = new StringBuilder();
                DateTime startTime = DateTime.Now;

                while (true)
                {
                    int elapsed =
                        (int)(DateTime.Now - startTime).TotalMilliseconds;

                    int remaining =
                        _options.StatusTimeout - elapsed;

                    if (remaining <= 0)
                        break;

                    _socketClient.ReceiveTimeout =
                        Math.Max(1, remaining);

                    byte[] receivedData =
                        _socketClient.ReceiveData(2048);

                    if (receivedData != null &&
                        receivedData.Length > 0)
                    {
                        rawBuilder.Append(
                            Encoding.ASCII.GetString(receivedData));

                        List<string> packets =
                            ExtractHsPackets(rawBuilder.ToString());

                        if (packets.Count >= 3)
                        {
                            return ParseStatus(
                                rawBuilder.ToString(),
                                packets);
                        }
                    }

                    if (_socketClient.ConnectionClosed ||
                        _socketClient.ReceiveTimedOut)
                    {
                        break;
                    }
                }

                return CreateErrorStatus(
                    "프린터 상태 응답이 없습니다.",
                    rawBuilder.ToString());
            }
            catch (Exception ex)
            {
                return CreateErrorStatus(ex.Message);
            }
            finally
            {
                if (_socketClient != null)
                {
                    try
                    {
                        _socketClient.ReceiveTimeout =
                            oldReceiveTimeout;
                    }
                    catch
                    {
                    }
                }
            }
        }

        private ZebraPrinterStatus GetSerialStatus()
        {
            if (_serialPort == null ||
                !_serialPort.IsOpen)
            {
                return CreateErrorStatus(
                    "프린터가 연결되어 있지 않습니다.");
            }

            try
            {
                if (!_serialPort.DataSend(STATUS_COMMAND))
                {
                    return CreateErrorStatus(
                        _serialPort.ErrMsg);
                }

                StringBuilder rawBuilder = new StringBuilder();
                DateTime startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalMilliseconds
                       < _options.StatusTimeout)
                {
                    string receivedData;

                    if (_serialPort.DataRead(out receivedData))
                    {
                        if (!string.IsNullOrEmpty(receivedData))
                        {
                            rawBuilder.Append(receivedData);

                            List<string> packets =
                                ExtractHsPackets(
                                    rawBuilder.ToString());

                            if (packets.Count >= 3)
                            {
                                return ParseStatus(
                                    rawBuilder.ToString(),
                                    packets);
                            }
                        }
                    }

                    Thread.Sleep(20);
                }

                return CreateErrorStatus(
                    "프린터 상태 응답이 없습니다.",
                    rawBuilder.ToString());
            }
            catch (Exception ex)
            {
                return CreateErrorStatus(ex.Message);
            }
        }

        private List<string> ExtractHsPackets(string raw)
        {
            List<string> packets =
                new List<string>();

            if (string.IsNullOrEmpty(raw))
                return packets;

            int searchIndex = 0;

            while (searchIndex < raw.Length)
            {
                int stxIndex =
                    raw.IndexOf((char)0x02, searchIndex);

                if (stxIndex < 0)
                    break;

                int etxIndex =
                    raw.IndexOf(
                        (char)0x03,
                        stxIndex + 1);

                if (etxIndex < 0)
                    break;

                string body =
                    raw.Substring(
                        stxIndex + 1,
                        etxIndex - stxIndex - 1);

                packets.Add(body);

                searchIndex = etxIndex + 1;
            }

            return packets;
        }

        private ZebraPrinterStatus ParseStatus(string raw, List<string> packets)
        {
            ZebraPrinterStatus result =
                new ZebraPrinterStatus();

            result.HasResponse = true;
            result.RawResponse =
                ToVisibleControlText(raw);

            if (packets.Count > 0)
                ParseString1(result, packets[0]);

            if (packets.Count > 1)
                ParseString2(result, packets[1]);

            if (packets.Count > 2)
                ParseString3(result, packets[2]);

            result.IsReady =
                string.IsNullOrWhiteSpace(result.ErrorMessage) &&
                !result.IsPaperOut &&
                !result.IsHeadOpen &&
                !(result.IsThermalTransferMode && result.IsRibbonOut) &&
                !result.IsPaused &&
                !result.IsHeadUnderTemperature &&
                !result.IsHeadOverTemperature &&
                !result.IsBufferFull &&
                !(
                    string.Equals(
                        result.PrintModeCode,
                        "1",
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    result.IsLabelWaiting);

            return result;
        }

        private void ParseString1(ZebraPrinterStatus result, string packet)
        {
            if (string.IsNullOrWhiteSpace(packet))
                return;

            string[] parts = packet.Split(',');

            if (parts.Length < 12)
            {
                result.ErrorMessage =
                    AppendMessage(
                        result.ErrorMessage,
                        "상태 응답 String1 형식 오류");

                return;
            }

            result.IsPaperOut =
                ToFlag(parts[1]);

            result.IsPaused =
                ToFlag(parts[2]);

            result.IsBufferFull =
                ToFlag(parts[5]);

            result.IsHeadUnderTemperature =
                ToFlag(parts[10]);

            result.IsHeadOverTemperature =
                ToFlag(parts[11]);
        }

        private void ParseString2(ZebraPrinterStatus result, string packet)
        {
            if (string.IsNullOrWhiteSpace(packet))
                return;

            string[] parts = packet.Split(',');

            if (parts.Length < 11)
            {
                result.ErrorMessage =
                    AppendMessage(
                        result.ErrorMessage,
                        "상태 응답 String2 형식 오류");

                return;
            }

            result.IsHeadOpen =
                ToFlag(parts[2]);

            result.IsRibbonOut =
                ToFlag(parts[3]);

            result.IsThermalTransferMode =
                ToFlag(parts[4]);

            result.PrintModeCode =
                SafeTrim(parts[5]);

            result.PrintModeText =
                GetPrintModeText(
                    result.PrintModeCode);

            result.IsLabelWaiting =
                ToFlag(parts[7]);
        }

        private void ParseString3(ZebraPrinterStatus result, string packet)
        {
            if (string.IsNullOrWhiteSpace(packet))
                return;

            string[] parts = packet.Split(',');

            if (parts.Length < 2)
            {
                result.ErrorMessage =
                    AppendMessage(
                        result.ErrorMessage,
                        "상태 응답 String3 형식 오류");
            }
        }


        private string GetPrintModeText(string code)
        {
            switch (SafeTrim(code).ToUpper())
            {
                case "0":
                    return "Rewind";

                case "1":
                    return "Peel-Off";

                case "2":
                    return "Tear-Off";

                case "3":
                    return "Cutter";

                case "4":
                    return "Applicator";

                case "5":
                    return "Delayed Cut";

                case "6":
                    return "Linerless Peel";

                case "7":
                    return "Linerless Rewind";

                case "8":
                    return "Partial Cutter";

                case "9":
                    return "RFID";

                case "K":
                    return "Kiosk";

                case "S":
                case "A":
                    return "Kiosk CutStream";

                default:
                    return "Unknown(" + code + ")";
            }
        }

        private string ToVisibleControlText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            StringBuilder sb =
                new StringBuilder();

            foreach (char ch in input)
            {
                switch (ch)
                {
                    case (char)0x02:
                        sb.Append("<STX>");
                        break;

                    case (char)0x03:
                        sb.Append("<ETX>");
                        break;

                    case '\r':
                        sb.Append("<CR>");
                        break;

                    case '\n':
                        sb.Append("<LF>");
                        break;

                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }
        #endregion

        #region 보조함수
        private bool ToFlag(string value)
        {
            return string.Equals(
                SafeTrim(value),
                "1",
                StringComparison.OrdinalIgnoreCase);
        }

        private string SafeTrim(string value)
        {
            return value == null
                ? string.Empty
                : value.Trim();
        }

        private string AppendMessage(string original, string append)
        {
            if (string.IsNullOrWhiteSpace(original))
                return append;

            return original + " / " + append;
        }

        private ZebraPrinterStatus CreateErrorStatus(string message, string raw = "")
        {
            ZebraPrinterStatus result =
                new ZebraPrinterStatus();

            result.HasResponse = false;
            result.IsReady = false;
            result.ErrorMessage =
                message ?? string.Empty;

            result.RawResponse =
                ToVisibleControlText(raw);

            return result;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    nameof(ZebraPrinter));
        }

        #endregion 
    }
}