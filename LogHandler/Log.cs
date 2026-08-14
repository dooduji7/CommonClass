using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections;
 
namespace LogHandler
{
    public enum LogType
    {
        Normal = 0,
        Data,
        Error,
    }


    /// <summary>
    /// LogHandler 
    /// </summary>
    public class Log : IDisposable
    {
        #region[Member]

        /// <summary>
        /// static 접근
        /// </summary>
        private static Log _instance = null;
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
        /// Log File Last Delete DateTime
        /// </summary>
        //private DateTime m_dtDelLastTime;

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
        private struct LogEntry
        {
            public string Id;
            public string[] Messages;
            public DateTime Timestamp;
            public clsSourceInfo SourceInfo;
            public int LineNumber;
            public string LogType;
            public string FunctionName;
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
        public static Log Instance()
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
        public static Log Init(string strPath, string strFileID, bool bSourceInfo, int iLogDeleteDay)
        {
            if (_instance != null)
            {
                _instance = null;
            }
            _instance = new Log(strPath, strFileID, bSourceInfo, iLogDeleteDay);
            return _instance;
        }

        public Log(string strPath, string strFileID, bool bSourceInfo, int iLogDeleteDay)
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
            if (m_threadLog == null)
                return;

            m_bCloseHandler = true;

            // 대기 중인 로그 스레드를 깨움
            m_avtLogEvent.Set();

            // Queue에 남아 있는 로그까지 처리한 후 종료
            m_threadLog.Join();

            m_threadLog = null;
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
                    LogEntry data = new LogEntry();
                    data.Messages = arrDatas;
                    data.Timestamp = DateTime.Now;
                    data.Id = "";
                    data.SourceInfo = GetSourceInfo();
                    data.LineNumber = iLineNum;
                    data.LogType = strLogType;
                    data.FunctionName = strFunName;
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
                    LogEntry data = new LogEntry();
                    data.Messages = arrDatas;
                    data.Timestamp = DateTime.Now;
                    data.Id = strID;
                    data.SourceInfo = GetSourceInfo();
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
                while (true)
                {
                    try
                    {
                        while (true)
                        {
                            LogEntry objData;

                            lock (m_objQueueLock)
                            {
                                if (m_queLog.Count == 0)
                                {
                                    break;
                                }

                                objData = (LogEntry)m_queLog.Dequeue();
                            }

                            try
                            {
                                WriteLog(objData);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    "Log Write Error : " + ex.Message);
                            }
                        }

                        if (m_bCloseHandler)
                        {
                            break;
                        }

                        DeleteLog();

                        m_avtLogEvent.WaitOne(10000, false);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "Log Process Error : " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Log Thread Error : " + ex.Message);
            }

            System.Diagnostics.Debug.WriteLine(
                "LogWriteProcess exit!!");
        }
        #endregion

        private void WriteLog(LogEntry objData)
        {
            string path = GetLogFilePath(objData);

            StringBuilder builder = new StringBuilder();

            builder.Append(
                objData.Timestamp.ToString("HH:mm:ss:ff"));

            if (m_bSourceInfo)
            {
                builder.Append(" | ");
                builder.Append(objData.SourceInfo.File);
                builder.Append(" | ");
                builder.Append(objData.FunctionName);
                builder.Append("(");
                builder.Append(objData.LineNumber);
                builder.Append(") ");
            }

            for (int i = 0; i < objData.Messages.Length; i++)
            {
                builder.Append(" | ");
                builder.Append(objData.Messages[i]);
            }

            lock (m_objFileLock)
            {
                using (StreamWriter writer = File.AppendText(path))
                {
                    writer.WriteLine(builder.ToString());
                }
            }
        }

        private string GetLogFilePath(LogEntry logEntry)
        {
            if (!Directory.Exists(m_strPath))
            {
                Directory.CreateDirectory(m_strPath);
            }

            string fileName;

            if (string.IsNullOrEmpty(logEntry.Id))
            {
                fileName = string.Format(
                    "{0}_{1}.log",
                    m_strFile,
                    DateTime.Now.ToString("yyyyMMddHH"));
            }
            else
            {
                fileName = string.Format(
                    "{0}_{1}.log",
                    logEntry.Id,
                    DateTime.Now.ToString("yyyyMMddHH"));
            }

            return Path.Combine(m_strPath, fileName);
        }

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
