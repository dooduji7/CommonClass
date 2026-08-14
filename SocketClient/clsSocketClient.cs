using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketClient
{
    public class clsSocketClient : IDisposable
    {
        // Fields
        private bool m_bDisposed = false;
        private TcpClient m_Client;
        private int m_nPort;
        private NetworkStream m_Stream;
        private string m_strErrMessage;
        private string m_strIP;
        private int m_nReceiveTimeout = 3000;
        private bool m_bReceiveTimedOut = false;
        private bool m_bConnectionClosed = false;
        private ReceiveState m_receiveState = ReceiveState.None;

        public enum ReceiveState
        {
            None = 0,
            Success,
            Timeout,
            ConnectionClosed,
            Error
        }

        public ReceiveState LastReceiveState
        {
            get
            {
                return m_receiveState;
            }
        }

        public bool ReceiveTimedOut
        {
            get
            {
                return m_receiveState == ReceiveState.Timeout;
            }
        }

        public bool ConnectionClosed
        {
            get
            {
                return m_receiveState == ReceiveState.ConnectionClosed;
            }
        }

        public int ReceiveTimeout
        {
            get
            {
                return m_nReceiveTimeout;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                m_nReceiveTimeout = value;

                if (m_Stream != null)
                {
                    m_Stream.ReadTimeout = value;
                }
            }
        }

        // Properties
        public string ERROR_MESSAGE
        {
            get
            {
                return this.m_strErrMessage;
            }
        }

        public bool IsConnected
        {
            get
            {
                if (m_bDisposed)
                    return false;

                return m_Client != null &&
                       m_Client.Connected;
            }
        }

        // Methods
        public clsSocketClient()
        {
            this.m_Client = null;
            this.m_Stream = null;
            this.m_strIP = string.Empty;
            this.m_nPort = 0;
            this.m_strErrMessage = string.Empty;
        }

        public clsSocketClient(string p_strIp, int p_nPort)
        {
            this.m_Client = null;
            this.m_Stream = null;
            this.m_strIP = string.Empty;
            this.m_nPort = 0;
            this.m_strErrMessage = string.Empty;
            this.m_strIP = p_strIp;
            this.m_nPort = p_nPort;
            this.m_Client = new TcpClient();
        }

        public void Disposed()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_bDisposed)
                return;

            m_bDisposed = true;

            CloseConnection();

            GC.SuppressFinalize(this);
        }

        public bool ReadData(out byte[] p_byBuffer)
        {
            p_byBuffer = ReceiveInternal(0x800);

            return p_byBuffer != null &&
                   p_byBuffer.Length > 0;
        }

        public byte[] ReceiveData(int p_nLength)
        {
            return ReceiveInternal(p_nLength);
        }


        public bool SocketConnect()
        {
            try
            {
                m_strErrMessage = string.Empty;

                if (m_bDisposed)
                {
                    m_strErrMessage = "이미 Dispose된 SocketClient입니다.";
                    return false;
                }

                CloseConnection();

                m_Client = new TcpClient();

                m_Client.Connect(
                    IPAddress.Parse(m_strIP),
                    m_nPort);

                if (!m_Client.Connected)
                {
                    m_strErrMessage = "연결되지 않음";

                    CloseConnection();
                    return false;
                }

                m_Stream = m_Client.GetStream();
                m_Stream.ReadTimeout = m_nReceiveTimeout;

                return true;
            }
            catch (Exception ex)
            {
                m_strErrMessage = ex.Message;

                CloseConnection();

                return false;
            }
        }

        public void SocketDisconnect()
        {
            CloseConnection();
        }

        

        private bool SendInternal(string p_strData)
        {
            m_strErrMessage = string.Empty;

            if (m_bDisposed)
            {
                m_strErrMessage = "이미 Dispose된 SocketClient입니다.";
                return false;
            }

            if (m_Stream == null)
            {
                m_strErrMessage = "Socket이 연결되지 않았습니다.";
                return false;
            }

            try
            {
                byte[] data = Encoding.ASCII.GetBytes(p_strData);

                m_Stream.Write(data, 0, data.Length);
                m_Stream.Flush();

                return true;
            }
            catch (SocketException ex)
            {
                m_strErrMessage = ex.Message + "[Socket 오류]";
                return false;
            }
            catch (Exception ex)
            {
                m_strErrMessage = ex.Message;
                return false;
            }
        }

        private byte[] ReceiveInternal(int p_nLength)
        {
            m_strErrMessage = string.Empty;
            m_receiveState = ReceiveState.None;

            if (m_bDisposed)
            {
                m_receiveState = ReceiveState.Error;
                m_strErrMessage = "이미 Dispose된 SocketClient입니다.";
                return null;
            }

            if (m_Stream == null || m_Client == null)
            {
                m_receiveState = ReceiveState.Error;
                m_strErrMessage = "Socket이 연결되지 않았습니다.";
                return null;
            }

            if (p_nLength <= 0)
            {
                m_receiveState = ReceiveState.Error;
                m_strErrMessage = "수신 길이는 1 이상이어야 합니다.";
                return null;
            }

            try
            {
                byte[] buffer = new byte[p_nLength];

                int readLength = m_Stream.Read(
                    buffer,
                    0,
                    buffer.Length);

                if (readLength == 0)
                {
                    m_receiveState = ReceiveState.ConnectionClosed;
                    m_strErrMessage = "상대방에서 연결을 종료했습니다.";

                    return new byte[0];
                }

                m_receiveState = ReceiveState.Success;

                if (readLength == buffer.Length)
                    return buffer;

                byte[] result = new byte[readLength];

                Buffer.BlockCopy(
                    buffer,
                    0,
                    result,
                    0,
                    readLength);

                return result;
            }
            catch (IOException ex)
            {
                SocketException socketEx =
                    ex.InnerException as SocketException;

                if (socketEx != null &&
                    socketEx.SocketErrorCode == SocketError.TimedOut)
                {
                    m_receiveState = ReceiveState.Timeout;
                    m_strErrMessage = "수신 시간이 초과되었습니다.";
                }
                else
                {
                    m_receiveState = ReceiveState.Error;
                    m_strErrMessage = ex.Message;
                }

                return null;
            }
            catch (SocketException ex)
            {
                m_receiveState = ReceiveState.Error;
                m_strErrMessage = ex.Message + "[Socket 오류]";

                return null;
            }
            catch (Exception ex)
            {
                m_receiveState = ReceiveState.Error;
                m_strErrMessage = ex.Message;

                return null;
            }
        }

        public bool SendData(string p_strData)
        {
            return SendInternal(p_strData);
        }

        public bool Write(string p_strData)
        {
            return SendInternal(p_strData);
        }


        private void CloseConnection()
        {
            if (m_Stream != null)
            {
                try
                {
                    m_Stream.Close();
                }
                catch
                {
                }
                finally
                {
                    m_Stream = null;
                }
            }

            if (m_Client != null)
            {
                try
                {
                    m_Client.Close();
                }
                catch
                {
                }
                finally
                {
                    m_Client = null;
                }
            }
        }
    }
}
