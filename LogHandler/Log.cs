using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

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
        public const int DefaultStopTimeoutMilliseconds = 5000;
        public const int DefaultMaxQueueSize = 10000;

        #region Member

        private static readonly object s_instanceLock = new object();
        private static readonly object s_fileLock = new object();

        /// <summary>
        /// static 접근
        /// </summary>
        private static Log _instance = null;

        /// <summary>
        /// Log Queue
        /// </summary>
        private readonly Queue<LogEntry> m_queLog;

        /// <summary>
        /// Log Queue Lock
        /// </summary>
        private readonly object m_objQueueLock;

        /// <summary>
        /// AutoResetEvent (LogEvent)
        /// </summary>
        private readonly AutoResetEvent m_avtLogEvent;

        /// <summary>
        /// Log Thread
        /// </summary>
        private Thread m_threadLog;

        /// <summary>
        /// Path
        /// </summary>
        private readonly string m_strPath;

        /// <summary>
        /// FileName
        /// </summary>
        private readonly string m_strFile;

        /// <summary>
        /// Max custody Log Days
        /// </summary>
        private int m_iMaxLogDay = 30;

        private object m_objTag = new object();

        /// <summary>
        /// log 파일 삭제 주기(분)
        /// </summary>
        private int m_iDelTerm = 60;

        /// <summary>
        /// 마지막 로그 삭제 검사 시각
        /// </summary>
        private DateTime m_dtDelLastTime = DateTime.MinValue;

        private readonly bool m_bSourceInfo;

        /// <summary>
        /// Log thread 종료 요청 플래그
        /// </summary>
        private volatile bool m_bCloseHandler;

        /// <summary>
        /// Dispose 중복 실행 방지
        /// 0 = Active, 1 = Disposed
        /// </summary>
        private int m_disposeState;
        private int m_eventDisposeState;
        private int m_maxQueueSize = DefaultMaxQueueSize;
        private long m_droppedLogCount;

        #endregion

        #region Struct

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
                return m_iMaxLogDay;
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
                return m_iDelTerm;
            }
            set
            {
                m_iDelTerm = value;
            }
        }

        public int MaxQueueSize
        {
            get
            {
                lock (m_objQueueLock)
                {
                    return m_maxQueueSize;
                }
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                lock (m_objQueueLock)
                {
                    m_maxQueueSize = value;
                }
            }
        }

        public long DroppedLogCount
        {
            get { return Interlocked.Read(ref m_droppedLogCount); }
        }

        #endregion

        #region Class

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

        #region Properties

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

        #region 생성자 및 초기화, 리소스 제거

        /// <summary>
        /// Instance 호출
        /// </summary>
        /// <returns>LogHandler</returns>
        public static Log Instance()
        {
            lock (s_instanceLock)
            {
                return _instance;
            }
        }

        /// <summary>
        /// LogHandler 초기화
        /// </summary>
        /// <param name="strPath">Log 파일 폴더 경로</param>
        /// <param name="strFileID">Log 파일 이름</param>
        /// <param name="bSourceInfo">소스 정보 저장 여부</param>
        /// <param name="iLogDeleteDay">로그 보관 일수</param>
        /// <returns>LogHandler</returns>
        public static Log Init(
            string strPath,
            string strFileID,
            bool bSourceInfo,
            int iLogDeleteDay)
        {
            lock (s_instanceLock)
            {
                if (_instance != null)
                {
                    _instance.Dispose();
                    _instance = null;
                }

                _instance =
                    new Log(
                        strPath,
                        strFileID,
                        bSourceInfo,
                        iLogDeleteDay);

                return _instance;
            }
        }

        public Log(
            string strPath,
            string strFileID,
            bool bSourceInfo,
            int iLogDeleteDay)
        {
            if (string.IsNullOrWhiteSpace(strPath))
            {
                throw new ArgumentException(
                    "로그 경로가 필요합니다.",
                    nameof(strPath));
            }

            m_strPath = strPath;

            string convertedFileId =
                ConvertFileID(strFileID);

            m_strFile =
                string.IsNullOrWhiteSpace(convertedFileId)
                    ? "Log"
                    : convertedFileId;

            m_iMaxLogDay = iLogDeleteDay;
            m_bSourceInfo = bSourceInfo;

            m_objQueueLock = new object();
            m_queLog = new Queue<LogEntry>(100);
            m_avtLogEvent = new AutoResetEvent(false);

            try
            {
                Directory.CreateDirectory(m_strPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Log Directory Create Error : " +
                    ex.Message);

                m_avtLogEvent.Dispose();
                throw;
            }

            m_threadLog =
                new Thread(
                    new ThreadStart(LogWriteProcess));

            m_threadLog.Name = "LogHandler.LogWriteProcess";
            m_threadLog.IsBackground = true;
            m_threadLog.Start();

            // 시작 직후 Queue 확인 및 오래된 로그 정리를 수행하도록 깨운다.
            m_avtLogEvent.Set();
        }

        #region Dispose

        /// <summary>
        /// Dispose (스레드 종료)
        /// </summary>
        public void Dispose()
        {
            Stop(DefaultStopTimeoutMilliseconds);
            GC.SuppressFinalize(this);
        }

        public bool Stop(int timeoutMilliseconds)
        {
            if (timeoutMilliseconds < Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));

            if (Interlocked.Exchange(ref m_disposeState, 1) != 0)
            {
                Thread existingThread = m_threadLog;
                return existingThread == null || !existingThread.IsAlive;
            }

            m_bCloseHandler = true;

            try
            {
                // 대기 중인 로그 스레드를 깨움
                m_avtLogEvent.Set();
            }
            catch (ObjectDisposedException)
            {
                return true;
            }

            Thread thread = m_threadLog;

            if (thread != null &&
                thread != Thread.CurrentThread)
            {
                // Queue에 남아 있는 로그를 처리하되 종료를 무기한 기다리지 않는다.
                if (!thread.Join(timeoutMilliseconds))
                    return false;
            }

            m_threadLog = null;
            DisposeLogEvent();
            return true;
        }

        private void DisposeLogEvent()
        {
            if (Interlocked.Exchange(ref m_eventDisposeState, 1) == 0)
                m_avtLogEvent.Dispose();
        }

        #endregion

        #endregion

        #region Method

        #region ConvertFileID

        /// <summary>
        /// 파일명에 사용할 수 없는 특수 문자를 공백으로 변경
        /// </summary>
        /// <param name="strID">입력 문자열</param>
        /// <returns>파일명 사용 가능 문자열</returns>
        public static string ConvertFileID(string strID)
        {
            if (string.IsNullOrEmpty(strID))
                return string.Empty;

            char[] invalidChars =
                Path.GetInvalidFileNameChars();

            StringBuilder builder =
                new StringBuilder(strID.Length);

            for (int i = 0; i < strID.Length; i++)
            {
                char value = strID[i];

                builder.Append(
                    Array.IndexOf(
                        invalidChars,
                        value) >= 0
                        ? ' '
                        : value);
            }

            return builder.ToString();
        }

        #endregion

        #region Get Source info

        public clsSourceInfo GetSourceInfo()
        {
            clsSourceInfo sourceInfo =
                new clsSourceInfo();

            try
            {
                StackTrace stackTrace =
                    new StackTrace(true);

                StackFrame stackFrame =
                    stackTrace.GetFrame(2);

                if (stackFrame == null)
                    return sourceInfo;

                if (stackFrame.GetMethod() != null)
                {
                    sourceInfo.Method =
                        stackFrame.GetMethod().Name;
                }

                string fileName =
                    stackFrame.GetFileName();

                if (!string.IsNullOrEmpty(fileName))
                {
                    sourceInfo.File =
                        Path.GetFileName(fileName);
                }

                sourceInfo.LineNumber =
                    stackFrame.GetFileLineNumber();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "GetSourceInfo Error : " +
                    ex.Message);
            }

            return sourceInfo;
        }

        #endregion

        #region EnqueueLog

        /// <summary>
        /// 로그 입력
        /// </summary>
        /// <param name="strLogType">로그 타입</param>
        /// <param name="strFunName">함수명</param>
        /// <param name="iLineNum">라인번호</param>
        /// <param name="arrDatas">로그상세</param>
        public void EnqueueLog(
            string strLogType,
            string strFunName,
            int iLineNum,
            string[] arrDatas)
        {
            if (Volatile.Read(ref m_disposeState) != 0)
                return;

            try
            {
                LogEntry data =
                    new LogEntry
                    {
                        Messages =
                            arrDatas ?? new string[0],
                        Timestamp = DateTime.Now,
                        Id = string.Empty,
                        SourceInfo =
                            m_bSourceInfo
                                ? GetSourceInfo()
                                : null,
                        LineNumber = iLineNum,
                        LogType = strLogType,
                        FunctionName = strFunName
                    };

                Enqueue(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "EnqueueLog Error : " +
                    ex.Message);
            }
        }

        /// <summary>
        /// 로그 입력
        /// </summary>
        /// <param name="strID">저장할 특정 로그파일의 이름 header</param>
        /// <param name="arrDatas">로그상세</param>
        public void EnqueueLog(
            string strID,
            string[] arrDatas)
        {
            if (Volatile.Read(ref m_disposeState) != 0)
                return;

            try
            {
                LogEntry data =
                    new LogEntry
                    {
                        Messages =
                            arrDatas ?? new string[0],
                        Timestamp = DateTime.Now,
                        Id = strID,
                        SourceInfo =
                            m_bSourceInfo
                                ? GetSourceInfo()
                                : null,
                        LineNumber = 0,
                        LogType = string.Empty,
                        FunctionName = string.Empty
                    };

                Enqueue(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "EnqueueLog Error : " +
                    ex.Message);
            }
        }

        private void Enqueue(LogEntry data)
        {
            lock (m_objQueueLock)
            {
                if (Volatile.Read(
                        ref m_disposeState) != 0)
                {
                    return;
                }

                if (m_queLog.Count >= m_maxQueueSize)
                {
                    Interlocked.Increment(ref m_droppedLogCount);
                    return;
                }

                m_queLog.Enqueue(data);
            }

            m_avtLogEvent.Set();
        }

        #endregion

        #region LogWriteProcess

        private void LogWriteProcess()
        {
            try
            {
                while (true)
                {
                    DrainQueue();

                    if (m_bCloseHandler)
                    {
                        // Dispose 직전에 들어온 로그가 있으면 한 번 더 비운다.
                        if (GetQueueCount() == 0)
                        {
                            break;
                        }

                        continue;
                    }

                    TryDeleteLog();

                    m_avtLogEvent.WaitOne(
                        10000,
                        false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Log Thread Error : " +
                    ex.Message);

                // 예상하지 못한 오류가 발생해도 종료 직전 Queue는 최대한 비운다.
                try
                {
                    DrainQueue();
                }
                catch (Exception drainException)
                {
                    Debug.WriteLine(
                        "Log Final Drain Error : " +
                        drainException.Message);
                }
            }

            Debug.WriteLine(
                "LogWriteProcess exit!!");

            if (Volatile.Read(ref m_disposeState) != 0)
                DisposeLogEvent();
        }

        private void DrainQueue()
        {
            while (true)
            {
                LogEntry logEntry;

                lock (m_objQueueLock)
                {
                    if (m_queLog.Count == 0)
                    {
                        return;
                    }

                    logEntry =
                        m_queLog.Dequeue();
                }

                try
                {
                    WriteLog(logEntry);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Log Write Error : " +
                        ex.Message);
                }
            }
        }

        private int GetQueueCount()
        {
            lock (m_objQueueLock)
            {
                return m_queLog.Count;
            }
        }

        #endregion

        private void WriteLog(LogEntry logEntry)
        {
            string path =
                GetLogFilePath(logEntry);

            StringBuilder builder =
                new StringBuilder();

            builder.Append(
                logEntry.Timestamp.ToString(
                    "HH:mm:ss:ff"));

            if (m_bSourceInfo)
            {
                clsSourceInfo sourceInfo =
                    logEntry.SourceInfo;

                string sourceFile =
                    sourceInfo == null
                        ? string.Empty
                        : sourceInfo.File;

                string functionName =
                    !string.IsNullOrEmpty(
                        logEntry.FunctionName)
                        ? logEntry.FunctionName
                        : sourceInfo == null
                            ? string.Empty
                            : sourceInfo.Method;

                int lineNumber =
                    logEntry.LineNumber > 0
                        ? logEntry.LineNumber
                        : sourceInfo == null
                            ? 0
                            : sourceInfo.LineNumber;

                builder.Append(" | ");
                builder.Append(sourceFile);
                builder.Append(" | ");
                builder.Append(functionName);
                builder.Append("(");
                builder.Append(lineNumber);
                builder.Append(") ");
            }

            string[] messages =
                logEntry.Messages ??
                new string[0];

            for (int i = 0;
                 i < messages.Length;
                 i++)
            {
                builder.Append(" | ");
                builder.Append(
                    messages[i] ??
                    string.Empty);
            }

            lock (s_fileLock)
            {
                using (StreamWriter writer =
                    new StreamWriter(
                        path,
                        true,
                        Encoding.UTF8))
                {
                    writer.WriteLine(
                        builder.ToString());
                }
            }
        }

        private string GetLogFilePath(
            LogEntry logEntry)
        {
            Directory.CreateDirectory(
                m_strPath);

            string convertedId =
                ConvertFileID(logEntry.Id);

            string filePrefix =
                string.IsNullOrWhiteSpace(convertedId)
                    ? m_strFile
                    : convertedId;

            string fileName =
                string.Format(
                    "{0}_{1}.log",
                    filePrefix,
                    logEntry.Timestamp.ToString(
                        "yyyyMMddHH"));

            return Path.Combine(
                m_strPath,
                fileName);
        }

        #region 로그 삭제

        private void TryDeleteLog()
        {
            if (m_iDelTerm <= 0)
                return;

            DateTime now =
                DateTime.Now;

            if (m_dtDelLastTime !=
                    DateTime.MinValue &&
                now.Subtract(
                    m_dtDelLastTime)
                    .TotalMinutes <
                    m_iDelTerm)
            {
                return;
            }

            if (DeleteLog())
            {
                m_dtDelLastTime = now;
            }
        }

        private bool DeleteLog()
        {
            if (m_iMaxLogDay <= 0)
                return true;

            try
            {
                if (!Directory.Exists(m_strPath))
                    return true;

                string[] files =
                    Directory.GetFiles(
                        m_strPath,
                        "*.log",
                        SearchOption.TopDirectoryOnly);

                DateTime deleteBefore =
                    DateTime.Now.AddDays(
                        -m_iMaxLogDay);

                foreach (string filePath in files)
                {
                    try
                    {
                        DateTime lastWriteTime =
                            File.GetLastWriteTime(
                                filePath);

                        if (lastWriteTime <
                            deleteBefore)
                        {
                            File.Delete(filePath);
                        }
                    }
                    catch (Exception fileException)
                    {
                        Debug.WriteLine(
                            "Log Delete File Error : " +
                            fileException.Message);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Log Delete Error : " +
                    ex.Message);

                return false;
            }
        }

        #endregion

        #endregion
    }
}
