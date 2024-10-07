using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections;

/***************************************************************************
// NAME         :   LogHandler
// Description  :   PTC.MES.LOG 로그 저장 
// *************************************************************************/
/*  History     :
 ***************************************************************************
 * * v0.1 2009-10-21  JKCHOI 신규 
 *                    
 *      
 *          
****************************************************************************/
namespace KFA.DBHANDLER
{


    /// <summary>
    /// LogHandler 
    /// </summary>
    public class LogHandler : IDisposable
    {
        #region[Member]

        /// <summary>
        /// static 접근
        /// </summary>
        private static LogHandler _instance = null;
        /// <summary>
        /// Log Queue
        /// </summary>
        private Queue m_queLog;
        /// <summary>
        /// Log Queue Lock
        /// </summary>
        private object m_objQueueLock = null;
        /// <summary>
        /// Log File Lock
        /// </summary>
        private object m_objFileLock = null;
        /// <summary>
        /// AutoResetEvent (LogEvent)
        /// </summary>
        private AutoResetEvent m_avtLogEvent = new AutoResetEvent(false);
        /// <summary>
        /// Log Thread
        /// </summary>
        private Thread m_threadLog;
        /// <summary>
        /// Path
        /// </summary>
        private string m_strPath = "";
        /// <summary>
        /// FileName
        /// </summary>
        private string m_strFile = "";
        /// <summary>
        /// Max custody Log Days
        /// </summary>
        private int m_iMaxLogDay = 30;
        private object m_objTag = new object();


        /// <summary>
        /// log파일 삭제 term
        /// </summary>
        private int m_iDelTerm = 60; //60분

        private bool m_bSourceInfo;
        private bool m_bCloseHandler = false;
        #endregion

        #region[Struct]
        /// <summary>
        /// Log Struct
        /// </summary>
        private struct STUC_LOG
        {
            public string strID;
            public string[] arrLog;
            public DateTime datLog;
            public clsSourceInfo stucSorceInfo;
            public int iLineNum;
            public string strLogType;
            public string strFunName;
        }

        public int MaxLogDays
        {
            get
            {
                return (m_iMaxLogDay);
            }
            set
            {
                m_iMaxLogDay = value;
            }
        }

        public int LogDeleteTerm
        {
            get
            {
                return (m_iDelTerm);
            }
            set
            {
                m_iDelTerm = value;
            }
        }


        #endregion

        #region[class]

        /// <summary>
        /// 소스 info class
        /// </summary>
        public class clsSourceInfo
        {
            public string File = "";
            public string Method = "";
            public int LineNumber = 0;

        }
        #endregion

        #region[Properties]
        /// <summary>
        /// Max custody Log Days Set
        /// </summary>
        public int MaxLogDay
        {
            set
            {
                m_iMaxLogDay = value;
            }
        }
        /// <summary>
        /// Tag(object) Set & Get
        /// </summary>
        public object Tag
        {
            get
            {
                return m_objTag;
            }
            set
            {
                m_objTag = value;
            }
        }
        #endregion

        #region[생성자 및 초기화, 리소스 제거]
        /// <summary>
        /// Instance호출
        /// </summary>
        /// <returns>LogHandler</returns>
        public static LogHandler Instance()
        {
            return _instance;
        }


        /// <summary>
        /// LogHandler 초기화 
        /// </summary>
        /// <param name="strPath">Log 파일 폴더 경로</param>
        /// <param name="strFileID">Log 파일 이름</param>
        /// <param name="bSourceInfo">소스 정보 저장 여부</param>
        /// <returns>LogHandler</returns>
        public static LogHandler Init(string strPath, string strFileID, bool bSourceInfo, int iLogDeleteDay)
        {
            if (_instance != null)
            {
                _instance = null;
            }
            _instance = new LogHandler(strPath, strFileID, bSourceInfo, iLogDeleteDay);
            return _instance;
        }

        public LogHandler(string strPath, string strFileID, bool bSourceInfo, int iLogDeleteDay)
        {
            m_strPath = strPath;
            m_strFile = ConvertFileID(strFileID);
            m_iMaxLogDay = iLogDeleteDay;

            try
            {
                if (!Directory.Exists(m_strPath))
                {
                    Directory.CreateDirectory(m_strPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
            }
            m_bSourceInfo = bSourceInfo;

            m_objQueueLock = new object();
            m_objFileLock = new object();

            m_queLog = new Queue(100);



            m_threadLog = new Thread(new ThreadStart(LogWriteProcess));

            m_threadLog.Start();
            m_avtLogEvent.Set();

        }
        #region[Dispose]
        /// <summary>
        /// Dispose (스레드 종료)
        /// </summary>
        public void Dispose()
        {
            if (m_threadLog != null)
            {
                m_bCloseHandler = true;
                m_avtLogEvent.Set();
                m_threadLog.Join();
                m_threadLog.Abort();
                m_threadLog = null;
            }
        }
        #endregion
        #endregion

        #region[Method]

        #region[ConvertFileID]
        /// <summary>
        /// 특수 문자 제거
        /// </summary>
        /// <param name="strID">입력 문자열</param>
        /// <returns>특수 문자 제거 문자열</returns>
        public static string ConvertFileID(string strID)
        {
            strID = strID.Replace("\\", " ");
            strID = strID.Replace("/", " ");
            strID = strID.Replace(":", " ");
            strID = strID.Replace("*", " ");
            strID = strID.Replace("?", " ");
            strID = strID.Replace("<", " ");
            strID = strID.Replace(">", " ");
            strID = strID.Replace("|", " ");
            return strID;
        }
        #endregion

        #region[Get Source info]
        public clsSourceInfo GetSourceInfo()
        {
            clsSourceInfo SrcInfo = null;
            try
            {
                StackTrace st = new StackTrace(true);
                StackFrame sf = st.GetFrame(2);
                SrcInfo = new clsSourceInfo();
                try
                {
                    SrcInfo.Method = sf.GetMethod().Name;
                    string[] paths = sf.GetFileName().Split('\\');
                    SrcInfo.File = paths[paths.Length - 1];
                    SrcInfo.LineNumber = sf.GetFileLineNumber();
                }
                catch (Exception ex)
                {
                    SrcInfo.File = string.Empty;
                    SrcInfo.LineNumber = 0;
                }
                //string[] paths = sf.GetFileName().Split('\\');
                //SrcInfo.File = paths[paths.Length - 1];
                //SrcInfo.Method = sf.GetMethod().Name;
                //SrcInfo.LineNumber = sf.GetFileLineNumber();
                return SrcInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
                return SrcInfo;
            }

        }
        #endregion

        #region[EnqueueLog]
        /// <summary>
        /// 로그입력
        /// </summary>
        /// <param name="arrDatas">로그상세</param>
        public void EnqueueLog(string strLogType, string strFunName, int iLineNum, string[] arrDatas)
        {
            try
            {
                lock (m_objQueueLock)
                {
                    STUC_LOG data = new STUC_LOG();
                    data.arrLog = arrDatas;
                    data.datLog = DateTime.Now;
                    data.strID = "";
                    data.stucSorceInfo = GetSourceInfo();
                    data.iLineNum = iLineNum;
                    data.strLogType = strLogType;
                    data.strFunName = strFunName;
                    m_queLog.Enqueue(data);
                    m_avtLogEvent.Set();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
            }
        }
        /// <summary>
        /// 로그입력
        /// </summary>
        /// <param name="strID">저장할 특정 로그파일의 이름 header</param>
        /// <param name="arrDatas">로그상세</param>
        public void EnqueueLog(string strID, string[] arrDatas)
        {
            try
            {
                lock (m_objQueueLock)
                {
                    STUC_LOG data = new STUC_LOG();
                    data.arrLog = arrDatas;
                    data.datLog = DateTime.Now;
                    data.strID = strID;
                    data.stucSorceInfo = GetSourceInfo();
                    m_queLog.Enqueue(data);
                    m_avtLogEvent.Set();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
            }
        }
        #endregion

        #region[LogWriteProcess]
        private void LogWriteProcess()
        {
            try
            {
                while (!m_bCloseHandler)
                {
                    try
                    {
                        while (m_queLog.Count != 0)
                        {
                            try
                            {
                                STUC_LOG objData;

                                lock (m_objQueueLock)
                                {
                                    objData = (STUC_LOG)m_queLog.Peek();
                                }
                                string path = "";

                                if (!Directory.Exists(m_strPath))
                                {
                                    Directory.CreateDirectory(m_strPath);
                                }

                                if (objData.strID.Length == 0)
                                    path = string.Format("{0}\\{1}_{2}", m_strPath, m_strFile, DateTime.Now.ToString("yyyyMMddHH") + ".log");
                                else
                                    path = string.Format("{0}\\{1}_{2}", m_strPath, objData.strID, DateTime.Now.ToString("yyyyMMddHH") + ".log");

                                string strData = string.Empty;



                                strData += objData.datLog.ToString("HH:mm:ss:ff");


                                if (m_bSourceInfo)
                                {
                                    strData += " | " + objData.stucSorceInfo.File + " | " + objData.strFunName + "(" + objData.iLineNum + ") ";
                                }

                                for (int i = 0; i < objData.arrLog.Length; i++)
                                {
                                    strData += " | " + objData.arrLog[i];
                                }

                                lock (m_objFileLock)
                                {
                                    using (StreamWriter pFileWriter = File.AppendText(path))
                                    {
                                        pFileWriter.WriteLine(strData);
                                    }

                                }

                                lock (m_objQueueLock)
                                {
                                    m_queLog.Dequeue();
                                }
                            }
                            catch (Exception e)
                            {
                                System.Diagnostics.Debug.WriteLine("Log " + e.Message);
                            }
                        }
                        DeleteLog();

                        m_avtLogEvent.WaitOne(10000, false);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Log " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Log " + ex.Message);
            }

            System.Diagnostics.Debug.WriteLine("LogWriteProcess exit!!");
        }
        #endregion

        #region[로그 삭제]
        private void DeleteLog()
        {

            int DaleteDay = 0;
            string[] files = Directory.GetFiles(m_strPath, "*.*");

            DateTime dt = DateTime.Now;
            try
            {


                foreach (string strfile in files)
                {

                    dt = File.GetCreationTime(strfile);

                    // 여기서는 하루 전날 이하인 파일을 삭제하므로 -1

                    DaleteDay = Convert.ToInt32("-" + m_iMaxLogDay);
                    if (DateTime.Compare((DateTime.Now.AddDays(DaleteDay)), dt) > 0)
                    {
                        File.Delete(strfile);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return;
            }
        }

        #endregion
        #endregion

    }




}
