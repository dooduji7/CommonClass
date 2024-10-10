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
    public class clsSocketClient
    {
        // Fields
        public static bool bDisposed = false;
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
            if (!bDisposed)
            {
                bDisposed = true;
                if (this.m_Client != null)
                {
                    this.m_Client.Close();
                    this.m_Client = null;
                }
            }
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

        public byte[] ReceiveDataa(int p_nLength)
        {
            byte[] buffer = null;
            try
            {
                this.m_Socket.Receive(buffer);
            }
            catch (Exception)
            {
                return null;
            }
            return buffer;
        }

        public bool SendData(string p_strData)
        {
            BinaryWriter writer = new BinaryWriter(this.m_Stream, Encoding.ASCII);
            try
            {
                writer.Write(Encoding.ASCII.GetBytes(p_strData));
                writer.Flush();
            }
            catch (SocketException ex1)
            {
                this.m_strErrMessage = ex1.Message + "[Socket 오류]";
                writer = null;
                return false;
            }
            catch (Exception ex)
            {
                this.m_strErrMessage = ex.Message;
                writer = null;
                return false;
            }
            writer = null;
            return true;
        }

        public bool SocketConnect()
        {
            try
            {
                if (this.m_Client == null)
                {
                    this.m_Client = new TcpClient();
                }
                else
                {
                    this.m_Client = new TcpClient();
                }
                this.m_Client.Connect(IPAddress.Parse(this.m_strIP), this.m_nPort);
                if (this.m_Client.Connected)
                {
                    this.m_Stream = this.m_Client.GetStream();
                }
                else
                {
                    this.m_strErrMessage = "연결되지 않음";
                    return false;
                }
            }
            catch (Exception exception)
            {
                this.m_strErrMessage = exception.Message;
                return false;
            }
            return true;
        }

        public void SocketDisconnect()
        {
            this.m_Stream.Close();
            this.m_Client.Close();
            this.m_Client = null;
        }

        public bool Write(string p_strData)
        {
            this.m_strErrMessage = string.Empty;
            BinaryWriter writer = new BinaryWriter(this.m_Stream, Encoding.ASCII);
            try
            {
                writer.Write(Encoding.ASCII.GetBytes(p_strData));
                writer.Flush();
            }
            catch (SocketException ex1)
            {
                this.m_strErrMessage = ex1.Message + "[Socket 오류]";
                writer = null;
                return false;
            }
            catch (Exception ex)
            {
                this.m_strErrMessage = ex.Message;
                writer = null;
                return false;
            }
            return true;
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
    }
}
