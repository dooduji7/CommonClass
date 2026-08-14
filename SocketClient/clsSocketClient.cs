using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketClient
{
    public class clsSocketClient : IDisposable
    {
        // Fields
        private bool m_bDisposed = false;
        private TcpClient m_Client;
        private int m_nPort;
        private Socket m_Socket;
        private NetworkStream m_Stream;
        private string m_strErrMessage;
        private string m_strIP;

        // Methods
        public clsSocketClient()
        {
            this.m_Client = null;
            this.m_Stream = null;
            this.m_Socket = null;
            this.m_strIP = string.Empty;
            this.m_nPort = 0;
            this.m_strErrMessage = string.Empty;
        }

        public clsSocketClient(string p_strIp, int p_nPort)
        {
            this.m_Client = null;
            this.m_Stream = null;
            this.m_Socket = null;
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
            int num = 0;
            this.m_strErrMessage = string.Empty;
            while (!this.m_Stream.DataAvailable)
            {
                if (this.m_Stream.DataAvailable)
                {
                    break;
                }
                Thread.Sleep(10);
                num++;
                if (num > 10)
                {
                    break;
                }
            }
            if (num > 9)
            {
                p_byBuffer = null;
                return false;
            }
            BinaryReader reader = new BinaryReader(this.m_Stream, Encoding.ASCII);
            try
            {
                byte[] buffer = new byte[0x800];
                int num2 = this.m_Client.Client.Receive(buffer);
                if (num2 > 0)
                {
                    p_byBuffer = new byte[num2];
                    for (int i = 0; i < num2; i++)
                    {
                        p_byBuffer[i] = buffer[i];
                    }
                }
                else
                {
                    p_byBuffer = new byte[0];
                }
            }
            catch (Exception exception)
            {
                p_byBuffer = null;
                this.m_strErrMessage = exception.Message;
                return false;
            }
            return true;
        }

        public byte[] ReceiveData(int p_nLength)
        {
            byte[] buffer = null;
            BinaryReader reader = new BinaryReader(this.m_Stream, Encoding.ASCII);
            try
            {
                buffer = reader.ReadBytes(p_nLength);
            }
            catch (SocketException ex1)
            {
                this.m_strErrMessage = ex1.Message + "[Socket 오류]";
                reader = null;
                return null;
            }
            catch (Exception ex)
            {
                this.m_strErrMessage = ex.Message;
                reader = null;
                return null;
            }
            reader = null;
            return buffer;
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
                return ((this.m_Client != null) && this.m_Client.Connected);
            }
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

            m_Socket = null;
        }
    }
}
