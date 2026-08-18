using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace SerialHandler
{
    #region 이벤트 관련 클래스

    public class PortEventArgs : EventArgs
    {
        /// <summary>
        /// 이벤트에 사용할 문자 저장.
        /// </summary>
        private readonly string strRecvData;

        public PortEventArgs()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// 이벤트에서 데이터 저장하기 위한 생성자.
        /// </summary>
        /// <param name="strData"></param>
        public PortEventArgs(string strData)
        {
            this.strRecvData = strData ?? string.Empty;
        }

        /// <summary>
        /// 이벤트 발생시 데이터 가져오기.
        /// </summary>
        /// <returns></returns>
        public string GetRecvData()
        {
            return this.strRecvData;
        }
    }

    #endregion

    public class SerialComProt : IDisposable
    {
        #region Field

        private const char STX = (char)0x02;
        private const char ETX = (char)0x03;

        /// <summary>
        /// 통신 포트
        /// </summary>
        private readonly SerialPort m_Port = new SerialPort();

        /// <summary>
        /// 이벤트에 사용될 델리게이트
        /// </summary>
        /// <param name="send"></param>
        /// <param name="args"></param>
        public delegate void ComPortEventHandler(object send, PortEventArgs args);

        /// <summary>
        /// 이벤트 등록
        /// </summary>
        public event ComPortEventHandler DataRecv;

        private readonly List<ComPortEventHandler> hdls = new List<ComPortEventHandler>();
        private readonly object m_eventLock = new object();
        private readonly object m_recvLock = new object();
        private readonly StringBuilder m_recvBuffer = new StringBuilder();

        /// <summary>
        /// STX/ETX 사용 여부
        /// </summary>
        private bool m_bSTX_ETX;

        private bool m_bAuto;
        private bool m_isDataReceivedRegistered;
        private bool m_disposed;
        private string m_strErr = string.Empty;

        #endregion

        #region Constructor

        /// <summary>
        /// 데이터 자동으로 읽어 이벤트 발생할지 설정한다.
        /// </summary>
        /// <param name="bStart"></param>
        public SerialComProt(bool bStart)
        {
            SetAutoReadEvent(bStart);
        }

        /// <summary>
        /// 기본 생성자.
        /// 기존 동작과 동일하게 자동 수신 이벤트는 등록하지 않는다.
        /// </summary>
        public SerialComProt()
        {
            SetAutoReadEvent(false);
        }

        #endregion

        #region Event Management

        public void AddEvent(ComPortEventHandler h)
        {
            if (h == null)
                return;

            lock (hdls)
            {
                DataRecv += h;
                hdls.Add(h);
            }
        }

        public void ClearEvent()
        {
            lock (hdls)
            {
                foreach (ComPortEventHandler h in hdls)
                {
                    DataRecv -= h;
                }

                hdls.Clear();
            }
        }

        #endregion

        #region Property

        /// <summary>
        /// 데이터 자동으로 읽기 여부(이벤트 발생 여부)
        /// 설정값 변경 시 실제 SerialPort.DataReceived 이벤트도 등록/해제한다.
        /// </summary>
        public bool AutoReadEvent
        {
            get { return m_bAuto; }
            set { SetAutoReadEvent(value); }
        }

        /// <summary>
        /// 포트에 사용할 이름.
        /// </summary>
        public string Name
        {
            get { return m_Port.PortName; }
            set { m_Port.PortName = value; }
        }

        /// <summary>
        /// 통신 속도 설정
        /// </summary>
        public int BaudRate
        {
            get { return m_Port.BaudRate; }
            set { m_Port.BaudRate = value; }
        }

        /// <summary>
        /// 종료 비트 설정
        /// 0=None, 1=One, 2=Two, 3=OnePointFive
        /// </summary>
        public int StopBit
        {
            get { return (int)m_Port.StopBits; }
            set
            {
                switch (value)
                {
                    case 0:
                        m_Port.StopBits = StopBits.None;
                        break;
                    case 1:
                        m_Port.StopBits = StopBits.One;
                        break;
                    case 2:
                        m_Port.StopBits = StopBits.Two;
                        break;
                    case 3:
                        m_Port.StopBits = StopBits.OnePointFive;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(value),
                            value,
                            "StopBit은 0~3 범위여야 합니다.");
                }
            }
        }

        /// <summary>
        /// 패리티 설정
        /// 0=None, 1=Odd, 2=Even, 3=Mark, 4=Space
        /// </summary>
        public int Paritys
        {
            get { return (int)m_Port.Parity; }
            set
            {
                switch (value)
                {
                    case 0:
                        m_Port.Parity = Parity.None;
                        break;
                    case 1:
                        m_Port.Parity = Parity.Odd;
                        break;
                    case 2:
                        m_Port.Parity = Parity.Even;
                        break;
                    case 3:
                        m_Port.Parity = Parity.Mark;
                        break;
                    case 4:
                        m_Port.Parity = Parity.Space;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(value),
                            value,
                            "Paritys는 0~4 범위여야 합니다.");
                }
            }
        }

        /// <summary>
        /// 데이터 비트 설정
        /// </summary>
        public int DataBit
        {
            get { return m_Port.DataBits; }
            set { m_Port.DataBits = value; }
        }

        /// <summary>
        /// 통신 제어 설정
        /// 0=None, 1=XOnXOff, 2=RequestToSend, 3=RequestToSendXOnXOff
        /// </summary>
        public int Flow
        {
            get { return (int)m_Port.Handshake; }
            set
            {
                switch (value)
                {
                    case 0:
                        m_Port.Handshake = Handshake.None;
                        break;
                    case 1:
                        m_Port.Handshake = Handshake.XOnXOff;
                        break;
                    case 2:
                        m_Port.Handshake = Handshake.RequestToSend;
                        break;
                    case 3:
                        m_Port.Handshake = Handshake.RequestToSendXOnXOff;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(value),
                            value,
                            "Flow는 0~3 범위여야 합니다.");
                }
            }
        }

        /// <summary>
        /// 시작/종료 문자 사용 여부
        /// </summary>
        public bool STXETX
        {
            get { return m_bSTX_ETX; }
            set
            {
                if (m_bSTX_ETX == value)
                    return;

                m_bSTX_ETX = value;

                lock (m_recvLock)
                {
                    m_recvBuffer.Clear();
                }
            }
        }

        /// <summary>
        /// 통신 실패 시 마지막 에러 메시지
        /// </summary>
        public string ErrMsg
        {
            get { return m_strErr; }
            set { m_strErr = value ?? string.Empty; }
        }

        /// <summary>
        /// 연결 확인
        /// </summary>
        public bool IsOpen
        {
            get { return m_Port.IsOpen; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 포트 오픈
        /// </summary>
        /// <returns></returns>
        public bool PortOpen()
        {
            try
            {
                ThrowIfDisposed();

                Debug.WriteLine("Port Open start");

                if (!m_Port.IsOpen)
                    m_Port.Open();

                ErrMsg = string.Empty;
                Debug.WriteLine("Port Opened !");

                return true;
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                Debug.WriteLine("[SerialHandler] PortOpen Error : " + ex);
                return false;
            }
        }

        /// <summary>
        /// 포트 종료
        /// </summary>
        public void PortClose()
        {
            Debug.WriteLine("Port Close start");

            try
            {
                if (m_Port.IsOpen)
                    m_Port.Close();

                lock (m_recvLock)
                {
                    m_recvBuffer.Clear();
                }

                ErrMsg = string.Empty;
                Debug.WriteLine("Port Closed !");
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                Debug.WriteLine("[SerialHandler] PortClose Error : " + ex);
            }
        }

        /// <summary>
        /// 문자열 데이터 보내기
        /// </summary>
        public bool DataSend(string strSendData)
        {
            if (strSendData == null)
            {
                ErrMsg = "전송할 문자열 데이터가 null 입니다.";
                return false;
            }

            try
            {
                ThrowIfDisposed();

                m_Port.Write(strSendData);
                ErrMsg = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Byte 배열 데이터 보내기
        /// </summary>
        public bool DataSend(byte[] bSendData)
        {
            if (bSendData == null)
            {
                ErrMsg = "전송할 Byte 데이터가 null 입니다.";
                return false;
            }

            try
            {
                ThrowIfDisposed();

                m_Port.Write(bSendData, 0, bSendData.Length);
                ErrMsg = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Char 배열 데이터 보내기
        /// </summary>
        public bool DataSend(char[] cSendData)
        {
            if (cSendData == null)
            {
                ErrMsg = "전송할 Char 데이터가 null 입니다.";
                return false;
            }

            try
            {
                ThrowIfDisposed();

                m_Port.Write(cSendData, 0, cSendData.Length);
                ErrMsg = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 데이터 읽어오기.(이벤트 등록 안했을 때)
        /// </summary>
        /// <returns>false이면 ErrMsg에 오류 메시지가 저장된다.</returns>
        public bool DataRead(out string strReadData)
        {
            try
            {
                ThrowIfDisposed();

                strReadData = m_Port.ReadExisting();
                ErrMsg = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                strReadData = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Byte 데이터 읽어오기.(이벤트 등록 안했을 때)
        /// 문자열 Encoding 변환 없이 실제 수신 Byte를 직접 읽는다.
        /// </summary>
        public bool DataRead(out byte[] bReadData)
        {
            try
            {
                ThrowIfDisposed();

                int bytesToRead = m_Port.BytesToRead;

                if (bytesToRead <= 0)
                {
                    bReadData = new byte[0];
                    ErrMsg = string.Empty;
                    return true;
                }

                byte[] buffer = new byte[bytesToRead];
                int read = m_Port.Read(buffer, 0, buffer.Length);

                if (read == buffer.Length)
                {
                    bReadData = buffer;
                }
                else
                {
                    bReadData = new byte[read];

                    if (read > 0)
                        Buffer.BlockCopy(buffer, 0, bReadData, 0, read);
                }

                ErrMsg = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                bReadData = null;
                ErrMsg = ex.Message;
                return false;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// 시리얼 데이터 수신 이벤트.
        /// DataReceived 이벤트 1회가 패킷 1개라는 가정을 하지 않는다.
        /// STX/ETX 모드에서는 내부 버퍼에 누적 후 완성된 프레임만 전달한다.
        /// </summary>
        private void m_Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (m_disposed || !m_Port.IsOpen)
                    return;

                string receivedData = m_Port.ReadExisting();

                if (string.IsNullOrEmpty(receivedData))
                    return;

                if (!m_bSTX_ETX)
                {
                    RaiseDataRecv(receivedData);
                    return;
                }

                List<string> frames = ExtractFrames(receivedData);

                foreach (string frame in frames)
                {
                    RaiseDataRecv(frame);
                }
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                Debug.WriteLine("[SerialHandler] DataReceived Error : " + ex);
            }
        }

        /// <summary>
        /// STX/ETX 기반으로 완성된 프레임을 모두 추출한다.
        /// 완성되지 않은 프레임은 다음 DataReceived까지 내부 버퍼에 유지한다.
        /// </summary>
        private List<string> ExtractFrames(string receivedData)
        {
            List<string> frames = new List<string>();

            lock (m_recvLock)
            {
                m_recvBuffer.Append(receivedData);

                while (m_recvBuffer.Length > 0)
                {
                    string bufferText = m_recvBuffer.ToString();
                    int startIndex = bufferText.IndexOf(STX);

                    if (startIndex < 0)
                    {
                        // STX 이전의 잡음 데이터는 프레임으로 사용할 수 없으므로 제거한다.
                        m_recvBuffer.Clear();
                        break;
                    }

                    if (startIndex > 0)
                    {
                        // STX 이전의 불필요한 데이터를 제거한다.
                        m_recvBuffer.Remove(0, startIndex);
                        bufferText = m_recvBuffer.ToString();
                    }

                    int endIndex = bufferText.IndexOf(ETX, 1);

                    if (endIndex < 0)
                    {
                        // ETX가 아직 도착하지 않았으므로 다음 수신까지 보관한다.
                        break;
                    }

                    string frame = bufferText.Substring(1, endIndex - 1);
                    frames.Add(frame);

                    // 처리 완료한 STX ~ ETX 영역을 버퍼에서 제거한다.
                    m_recvBuffer.Remove(0, endIndex + 1);
                }
            }

            return frames;
        }

        private void RaiseDataRecv(string data)
        {
            ComPortEventHandler handler = DataRecv;

            if (handler == null)
                return;

            PortEventArgs args = new PortEventArgs(data);

            foreach (ComPortEventHandler subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, args);
                }
                catch (Exception ex)
                {
                    // 한 구독자의 예외가 다른 구독자의 이벤트 전달을 막지 않도록 한다.
                    Debug.WriteLine("[SerialHandler] DataRecv Handler Error : " + ex);
                }
            }
        }

        #endregion

        #region Private Methods

        private void SetAutoReadEvent(bool enabled)
        {
            lock (m_eventLock)
            {
                m_bAuto = enabled;

                if (enabled)
                {
                    if (!m_isDataReceivedRegistered)
                    {
                        m_Port.DataReceived += m_Port_DataReceived;
                        m_isDataReceivedRegistered = true;
                    }
                }
                else
                {
                    if (m_isDataReceivedRegistered)
                    {
                        m_Port.DataReceived -= m_Port_DataReceived;
                        m_isDataReceivedRegistered = false;
                    }
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(SerialComProt));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_disposed)
                return;

            try
            {
                ClearEvent();
                SetAutoReadEvent(false);

                if (m_Port.IsOpen)
                    m_Port.Close();

                m_Port.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SerialHandler] Dispose Error : " + ex);
            }
            finally
            {
                lock (m_recvLock)
                {
                    m_recvBuffer.Clear();
                }

                m_disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}