using System;
using System.Collections.Generic;
using System.Text;

using System.IO.Ports;
using System.Threading;
using System.Diagnostics;


namespace SerialHandler
{
    //20260817 최신소스 변경
    #region 이벤트 관련 클레스
    public class PortEventArgs : EventArgs
    {
        /// <summary>
        /// 이벤트에 사용할 문자 저장.
        /// </summary>
        private string strRecvData;

        public PortEventArgs()
        {
            this.strRecvData = "";
        }

        /// <summary>
        ///  이벤트에서 데이터 저장하기 위한 생성자.
        /// </summary>
        /// <param name="strData"></param>

        public PortEventArgs(string strData)
        {
            this.strRecvData = strData;
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


    public class SerialComProt
    {
        #region Field

        /// <summary>
        /// 통신 포트 
        /// </summary>
        private SerialPort m_Port = new SerialPort();
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

        private List<ComPortEventHandler> hdls = new List<ComPortEventHandler>();
        /// <summary>
        /// stx/etx 사용 여부
        /// </summary>
        private bool m_bSTX_ETX = false;
        /// <summary>
        /// 데이터 저장.
        /// </summary>
        private string m_strRecvDataSave = "";

        bool m_bAuto = false;
        string m_strErr;

        #endregion

        #region Constructor

        /// <summary>
        /// 데이터 자동으로 읽어 이벤트 발생할지...
        /// </summary>
        /// <param name="bStart"></param>
        public SerialComProt(bool bStart)
        {
            if (bStart)
                m_Port.DataReceived += new SerialDataReceivedEventHandler(m_Port_DataReceived);
        }

        /// <summary>
        /// 생성과 동시에 시리얼 포트의 이벤트를 등록 한다.
        /// </summary>
        public SerialComProt()
        {
            if (m_bAuto)
                // 데이터를 받을 이벤트 등록.
                m_Port.DataReceived += new SerialDataReceivedEventHandler(m_Port_DataReceived);
        }

        #endregion

        public void AddEvent(ComPortEventHandler h)
        {
            lock (hdls)
            {
                this.DataRecv += h;
                hdls.Add(h);
            }
        }

        public void ClearEvent()
        {
            lock (hdls)
            {
                foreach (ComPortEventHandler h in hdls)
                {
                    this.DataRecv -= h;   
                }
                hdls.Clear();
            }
        }

        /*********************************************************************************************************************************/
        #region Property
        /*********************************************************************************************************************************/

        /// <summary>
        /// 데이터 자동으로 읽기 여부(이벤트 발생 여부)
        /// </summary>
        public bool AutoReadEvent
        {
            set
            {
                m_bAuto = value;
            }
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
        ///  종료 비트 설정
        /// </summary>
        public int StopBit
        {
            get { return (int)m_Port.StopBits; }
            set
            {
                switch (value)
                {
                    case 0:
                        m_Port.StopBits = StopBits.None; //  정지 비트를 사용하지 않는다.
                        break;
                    case 1:
                        m_Port.StopBits = StopBits.One;  //  1비트의 정지 비트를 사용. 
                        break;
                    case 2:
                        m_Port.StopBits = StopBits.Two;  // 2비트의 정지 비트를 사용. 
                        break;
                    case 3:
                        m_Port.StopBits = StopBits.OnePointFive;  // 1.5비트의 정지 비트를 사용.
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 페리티 설정
        /// </summary>
        public int Paritys
        {
            get { return (int)m_Port.Parity; }
            set
            {
                switch (value)
                {
                    case 0:
                        m_Port.Parity = Parity.None;  // 패리티 검사를 수행하지 않는다. 
                        break;
                    case 1:
                        m_Port.Parity = Parity.Odd;  // 비트 집합의 비트 합계가 홀수가 되도록 패리티 비트를 설정.
                        break;
                    case 2:
                        m_Port.Parity = Parity.Even;  // 비트 집합의 비트 합계가 짝수가 되도록 패리티 비트를 설정.
                        break;
                    case 3:
                        m_Port.Parity = Parity.Mark;  // 패리티 비트를 1로 설정된 상태로 유지.
                        break;
                    case 4:
                        m_Port.Parity = Parity.Space;  // 패리티 비트를 0으로 설정된 상태로 유지.
                        break;
                    default:
                        break;
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
        /// </summary>
        public int Flow
        {
            get { return (int)m_Port.Handshake; }
            set
            {
                switch (value)
                {
                    case 0:
                        m_Port.Handshake = Handshake.None; // 핸드쉐이크를 사용하지 않는다.
                        break;
                    case 1:
                        m_Port.Handshake = Handshake.XOnXOff; // XON/XOFF 소프트웨어 제어 프로토콜을 사용.
                        break;
                    case 2:
                        m_Port.Handshake = Handshake.RequestToSend;  // RTS(Request to Send) 하드웨어 흐름 제어를 사용.
                        break;
                    case 3:
                        m_Port.Handshake = Handshake.RequestToSendXOnXOff; // RTS(Request to Send) 하드웨어 제어와 XON/XOFF 소프트웨어 제어를 모두 사용.
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 시작/종료 문자 사용 여부
        /// </summary>
        public bool STXETX
        {
            set { m_bSTX_ETX = value; }
            get { return m_bSTX_ETX; }
        }
        /// <summary>
        /// 통신할때 false 일때 에러 코드값
        /// </summary>
        public string ErrMsg
        {
            get { return m_strErr; }
            set { m_strErr = value; }
        }

        #endregion
        /*********************************************************************************************************************************/

        /*********************************************************************************************************************************/
        #region Metheds
        /*********************************************************************************************************************************/

        /// <summary>
        /// 포트 오픈
        /// </summary>
        /// <returns></returns>
        public bool PortOpen()
        {
            try
            {
                Debug.WriteLine("Port Open start");
                if (!m_Port.IsOpen)
                    m_Port.Open();
                Debug.WriteLine("Port Opend !");

                return true;
            }
            catch (System.UnauthorizedAccessException ex)
            {
                ErrMsg = ex.Message;
                return false;
            }
        }

        /*-------------------------------------------------------------------------------------------------*/

        /// <summary>
        /// 포트 종료
        /// </summary>
        public void PortClose()
        {
            Debug.WriteLine("Port Close start");
       
            try
            {
                m_Port.Close();

            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.StackTrace);
                throw;
            }

            Debug.WriteLine("Port Closed !");
        }

        /*-------------------------------------------------------------------------------------------------*/

        /// <summary>
        /// 연결 확인
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return m_Port.IsOpen;
            }
        }

        /*-------------------------------------------------------------------------------------------------*/

        /// <summary>
        /// 데이터 보내기
        /// </summary>
        /// <param name="strData"></param>
        /// <returns></returns>
        public bool DataSend(string strSendData)
        {
            try
            {
                m_Port.Write(strSendData);
                return true;
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                return false;
            }
        }

        /*-------------------------------------------------------------------------------------------------*/

        /// <summary>
        /// 데이터 보내기
        /// </summary>
        /// <param name="bData"></param>
        /// <returns></returns>
        public bool DataSend(byte[] bSendData)
        {
            try
            {
                m_Port.Write(bSendData, 0, bSendData.Length);
                return true;
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                return false;
            }
        }

        /*-------------------------------------------------------------------------------------------------*/

        /// <summary>
        /// 데이터 보내기
        /// </summary>
        /// <param name="cSendData"></param>
        /// <returns></returns>
        public bool DataSend(char[] cSendData)
        {
            try
            {
                m_Port.Write(cSendData, 0, cSendData.Length);
                return true;
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                return false;
            }
        }

        /*-------------------------------------------------------------------------------------------------*/

        /// <summary>
        /// 데이터 읽어오기.(이벤트 등록 안했을때...)
        /// </summary>
        /// <param name="strReadData"></param>
        /// <returns>false 이면 strReadData는 에러 메세지 등록</returns>
        public bool DataRead(out string strReadData)
        {
            try
            {
                strReadData = m_Port.ReadExisting();
                m_Port.DiscardInBuffer();
                return true;
            }
            catch (Exception ex)
            {
                ErrMsg = ex.Message;
                strReadData = "";
                return false;
            }

        }

        /*-------------------------------------------------------------------------------------------------*/

        /// <summary>
        /// 데이터 읽어오기.(이벤트 등록 안했을때...)
        /// </summary>
        /// <param name="bData"></param>
        /// <returns></returns>
        public bool DataRead(out byte[] bReadData)
        {
            try
            {
                string strData;
                strData = m_Port.ReadExisting();
                bReadData = Encoding.Default.GetBytes(strData);
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
        /*********************************************************************************************************************************/

        /*********************************************************************************************************************************/
        #region Events
        /*********************************************************************************************************************************/

        /// <summary>
        /// 시리얼에서 데이터를 받으면 검사하여 이밴트 발생
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string strData;
                if (m_bSTX_ETX)
                {
                    int nSTX, nETX;
                    nSTX = 2;
                    nETX = 3;
                    char cIndex;
                    cIndex = (char)nSTX;
                    Thread.Sleep(200);
                    strData = m_Port.ReadExisting();
                    int nStart = strData.IndexOf(cIndex);
                    if (nStart == -1)   // stx 가 없다...// etx 가 올때 까지 저장.
                    {
                        cIndex = (char)nETX;
                        int nEnd = strData.IndexOf(cIndex);
                        if (nEnd == -1) // etx 가 없으면 아직 데이터가 다 도착 하지 않았다.
                        {
                            //m_strRecvDataSave += strData;   // 데이터 저장 하고 나간다.
                            return;
                        }
                        else  // etx 가 있다.
                        {
                            //m_strRecvDataSave += strData;
                            //PortEventArgs a = new PortEventArgs(m_strRecvDataSave);     // 이벤트 발생하기 위한 데이터값 저장.
                            //DataRecv(this, a);                                          // 이벤트 발생.
                            //m_strRecvDataSave = "";
                            return;     // 정상적으로 데이터가 모였으니까...
                        }
                    }
                    else  // stx 가 있다. 
                    {
                        string strTemp1, strTemp2;
                        strTemp1 = strData.Substring(nStart + 1);
                        strData = strTemp1;
                        cIndex = (char)nETX;
                        int nEnd = strData.IndexOf(cIndex);
                        if (nEnd == -1) // etx 가 있는지 검사. 없으면 저장 하고 종료.
                        {
                            //m_strRecvDataSave += strData;
                            return;
                        }
                        else  // etx 가 있다.
                        {
                            strTemp2 = strData.Substring(0, nEnd);
                            strData = strTemp2;
                            //m_strRecvDataSave += strData;
                            PortEventArgs a = new PortEventArgs(strData);     // 이벤트 발생하기 위한 데이터값 저장.
                            DataRecv(this, a);                                          // 이벤트 발생.
                            m_strRecvDataSave = "";
                            return;
                        }
                    }
                }
                else
                {
                    Thread.Sleep(200);
                    strData = m_Port.ReadExisting();
                    PortEventArgs a = new PortEventArgs(strData);   // 이벤트 발생하기 위한 데이터값 저장.
                    DataRecv(this, a);                              // 이벤트 발생.
                }

                //throw new Exception("The method or operation is not implemented.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[YJ] "+ex.StackTrace);
            }

        }

        #endregion
        /*********************************************************************************************************************************/
        
    }
}
