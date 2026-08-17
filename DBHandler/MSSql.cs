using System;
using System.Data;
using System.Data.SqlClient;

namespace DBHandler
{
    #region[MSSQLDbAccess]
    /// <summary>
    /// MSSQLDbAccess에 대한 요약 설명입니다.
    /// </summary>
    public class MSSQLDbAccess
    {
        #region[MSSQLDbAccess]
        /// <summary>
        /// MSSQLDbAccess 생성자 입니다.
        /// </summary>
        public MSSQLDbAccess() { }
        #endregion

        #region[member]
        public static int COMMAND_TIMEOUT = 30;

        internal static string m_SQLTraceMode = "";//System.Configuration.ConfigurationManager.AppSettings["SQLTraceMode"].Trim().ToLower();
        internal static string m_SQLTracePath = "";//System.Configuration.ConfigurationManager.AppSettings["SQLTracePath"].Trim();


        private static string m_User = "";
        private static string m_Pass = "";
        private static string m_Alias = "";


        private static SqlConnection m_SqlConnection = null;
        private static SqlTransaction m_SqlTransaction = null;

        #endregion

        #region[ConnectionString]
        /// <summary>
        /// 완성차 ConnectionString 입니다.
        /// </summary>
        public static string strConnectionString = "user id= " + m_User + ";data source= " + m_Alias + ";password= " + m_Pass + ";Connection Lifetime=300; Max Pool Size = 3";
        //public static string m_strConnectingString;


        public static void ConnectionString(string strAlias, string strUser, string strPassword)
        {
            strConnectionString = "user id= " + strUser + ";data source= " + strAlias + ";password= " + strPassword + ";Connection Lifetime=300; Max Pool Size = 3";
        }

        public static void ConnectionString(string strAlias, string strUser, string strPassword, int iLifeTime, int iMaxPoolSize)
        {
            strConnectionString = "user id= " + strUser + ";data source= " + strAlias + ";password= " + strPassword + ";Connection Lifetime=" + iLifeTime.ToString() + "; Max Pool Size = " + iMaxPoolSize.ToString();
        }

        public static void ConnectionString(string m_strIP, string m_strID, string m_strPW, string m_strDB)
        {
            strConnectionString = "Data Source=" + m_strIP + ";Initial Catalog=" + m_strDB + ";Persist Security Info=True;User ID=" + m_strID + ";Password=" + m_strPW + ";Timeout=3;Persist Security Info=False";

        }
        #endregion

        #region[Parameter Helpers]
        private static SqlParameter[] CreateInputParameters(
            string strID,
            string strVAL,
            char split)
        {
            if (string.IsNullOrWhiteSpace(strID))
                return null;

            string[] paramIds = strID.Split(split);
            string[] paramValues = (strVAL ?? string.Empty).Split(split);

            if (paramIds.Length != paramValues.Length)
            {
                throw new ArgumentException(
                    "Parameter 이름과 값의 개수가 일치하지 않습니다.");
            }

            SqlParameter[] parameters =
                new SqlParameter[paramIds.Length];

            for (int i = 0; i < paramIds.Length; i++)
            {
                parameters[i] =
                    new SqlParameter(paramIds[i], paramValues[i]);

                parameters[i].Direction =
                    ParameterDirection.Input;
            }

            return parameters;
        }

        private static SqlParameter[] CreateProcedureResultParameters(
            string strID,
            string strVAL,
            char split)
        {
            if (string.IsNullOrWhiteSpace(strID))
            {
                throw new ArgumentException(
                    "Output Parameter 정보가 필요합니다.",
                    nameof(strID));
            }

            string[] paramIds = strID.Split(split);
            string[] paramValues = (strVAL ?? string.Empty).Split(split);

            if (paramIds.Length != paramValues.Length)
            {
                throw new ArgumentException(
                    "Parameter 이름과 값의 개수가 일치하지 않습니다.");
            }

            SqlParameter[] parameters =
                new SqlParameter[paramIds.Length];

            int lastIndex = paramIds.Length - 1;

            for (int i = 0; i < lastIndex; i++)
            {
                parameters[i] =
                    new SqlParameter(paramIds[i], paramValues[i]);

                parameters[i].Direction =
                    ParameterDirection.Input;
            }

            parameters[lastIndex] =
                new SqlParameter(
                    paramIds[lastIndex],
                    SqlDbType.VarChar,
                    500);

            parameters[lastIndex].Direction =
                ParameterDirection.Output;

            return parameters;
        }
        #endregion

        #region[ExecuteProcedure]
        /// <summary>
        /// 오라클 DB 프로시져 실행
        /// </summary>
        /// <param name="strSpName">프로시져명</param>
        /// <param name="strID">파라미터 명 (VARCHAR형만 사용해야함)</param>
        /// <param name="strVAL">파라미터 값</param>
        public static void ExecuteProcedure(string strSpName, string strID, string strVAL)
        {
            ExecuteProcedure(strSpName, strID, strVAL, '@');
        }
        public static void ExecuteProcedure(string strSpName, string strID, string strVAL, char split)
        {
            SqlParameter[] parameters =
                CreateInputParameters(strID, strVAL, split);

            Execute(
                strSpName,
                parameters,
                CommandType.StoredProcedure);
        }

        #endregion

        #region[ExecuteProcedureResult]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strSpName"></param>
        /// <param name="strID">파라미터 명 (VARCHAR형만 사용해야함)</param>
        /// <param name="strVAL"></param>
        /// <returns></returns>
        public static string ExecuteProcedureResult(string strSpName, string strID, string strVAL)
        {
            return ExecuteProcedureResult(
                strSpName,
                strID,
                strVAL,
                '@');
        }

        public static string ExecuteProcedureResult(string strSpName, string strID, string strVAL, char split)
        {
            SqlParameter[] parameters =
                CreateProcedureResultParameters(
                    strID,
                    strVAL,
                    split);

            int outputIndex = parameters.Length - 1;

            ExecuteScalar(
                strSpName,
                parameters,
                CommandType.StoredProcedure);

            object value = parameters[outputIndex].Value;

            return value == null || value == DBNull.Value
                ? string.Empty
                : value.ToString().Trim();
        }
        #endregion

        #region ==== SQL 문을 실행(Execute) ====

        /// <summary>
        /// SQL 문을 실행한다. 기본연결문으로 디비에 연결하며, 기본타임 아웃시간을 가진다.
        /// </summary>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열형식</param>
        /// <returns>실행된 행의 수</returns>
        public static int Execute(string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return Execute(COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// SQL 문을 실행한다. 기본연결문으로 디비에 연결한다.
        /// </summary>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열형식</param>
        /// <returns>실행된 행의 수</returns>
        public static int Execute(int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return Execute(strConnectionString, commandTimeout, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// SQL 문을 실행한다. 기본 타임아웃 시간을 가진다.
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열형식</param>
        /// <returns>실행된 행의 수</returns>
        public static int Execute(string p_strConnectionString, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return Execute(p_strConnectionString, COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }


        /// <summary>
        /// SQL 문을 실행한다
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열형식</param>
        /// <returns>실행된 행의 수</returns>
        public static int Execute(string p_strConnectionString, int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            WriteDebugCommand(commandText, oraParameters);

            using (SqlConnection con =
                new SqlConnection(p_strConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                ConfigureCommand(
                    cmd,
                    commandText,
                    commandType,
                    commandTimeout,
                    oraParameters);

                if (m_SQLTraceMode.Equals("on"))
                {
                    SQLTrace(m_SQLTracePath, cmd, true);
                }

                con.Open();
                cmd.Connection = con;

                return cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region ==== SQL 문을 실행(ExecuteScalar) ====
        /// <summary>
        /// ExecuteScalar 기본디비 연결문으로 디비에 연결하면, 기본 명령 타임 아웃시간을이용한다.
        /// </summary>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>첫번째행 리턴</returns>
        public static object ExecuteScalar(string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return ExecuteScalar(COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// ExecuteScalar 기본디비 연결문으로 디비에 연결한다.
        /// </summary>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>첫번째행 리턴</returns>
        public static object ExecuteScalar(int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return ExecuteScalar(strConnectionString, commandTimeout, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// ExecuteScalar, 기본 명령 타임 아웃시간을 가진다.
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>첫번째행 리턴</returns>
        public static object ExecuteScalar(string p_strConnectionString, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return ExecuteScalar(p_strConnectionString, COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }




        /// <summary>
        /// ExecuteScalar
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>첫번째행 리턴</returns>
        public static object ExecuteScalar(string p_strConnectionString, int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            WriteDebugCommand(commandText, oraParameters);

            using (SqlConnection con =
                new SqlConnection(p_strConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                ConfigureCommand(
                    cmd,
                    commandText,
                    commandType,
                    commandTimeout,
                    oraParameters);

                if (m_SQLTraceMode.Equals("on"))
                {
                    SQLTrace(m_SQLTracePath, cmd, true);
                }

                con.Open();
                cmd.Connection = con;

                return cmd.ExecuteScalar();
            }
        }

        #endregion

        #region ==== SQL 문을 실행(ExecuteMultiple) ====

        /// <summary>
        /// 동일 SQL 문을 다른 입력 파라미터값으로 여러번 실행한다.
        /// 디비기본연결문과 기본 명령타임아웃시간을 가진다.
        /// </summary>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="paramValues">명령매개변수값</param>
        /// <param name="commandType">명령문자열타입</param>
        /// <returns>적용행수</returns>
        public static int ExecuteMultiple(string commandText, SqlParameter[] oraParameters, string[,] paramValues, CommandType commandType)
        {
            return ExecuteMultiple(COMMAND_TIMEOUT, commandText, oraParameters, paramValues, commandType);
        }

        /// <summary>
        /// 동일 SQL 문을 다른 입력 파라미터값으로 여러번 실행한다.
        /// 디비기본연결문을 가진다.
        /// </summary>
        /// <param name="commandTimeout">명령문타임아웃</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="paramValues">명령매개변수값</param>
        /// <param name="commandType">명령문자열타입</param>
        /// <returns>적용행수</returns>
        public static int ExecuteMultiple(int commandTimeout, string commandText, SqlParameter[] oraParameters, string[,] paramValues, CommandType commandType)
        {
            return ExecuteMultiple(strConnectionString, commandTimeout, commandText, oraParameters, paramValues, commandType);
        }

        /// <summary>
        /// 동일 SQL 문을 다른 입력 파라미터값으로 여러번 실행한다.
        /// 기본 명령타임아웃시간을 가진다.
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="paramValues">명령매개변수값</param>
        /// <param name="commandType">명령문자열타입</param>
        /// <returns>적용행수</returns>
        public static int ExecuteMultiple(string p_strConnectionString, string commandText, SqlParameter[] oraParameters, string[,] paramValues, CommandType commandType)
        {
            return ExecuteMultiple(p_strConnectionString, COMMAND_TIMEOUT, commandText, oraParameters, paramValues, commandType);
        }

        /// <summary>
        /// 동일 SQL 문을 다른 입력 파라미터값으로 여러번 실행한다.
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandTimeout">명령문타임아웃</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="paramValues">명령매개변수값</param>
        /// <param name="commandType">명령문자열타입</param>
        /// <returns>적용행수</returns>
        public static int ExecuteMultiple(string p_strConnectionString, int commandTimeout, string commandText, SqlParameter[] oraParameters, string[,] paramValues, CommandType commandType)
        {
            if (oraParameters == null)
                throw new ArgumentNullException(nameof(oraParameters));

            if (paramValues == null)
                throw new ArgumentNullException(nameof(paramValues));

            int rowCount = paramValues.GetLength(0);
            int columnCount = paramValues.GetLength(1);

            if (columnCount != oraParameters.Length)
            {
                throw new ArgumentException(
                    "Parameter 개수와 Value 열 개수가 일치하지 않습니다.",
                    nameof(paramValues));
            }

            if (rowCount == 0)
                return 0;

            WriteDebugCommand(commandText, oraParameters);

            int affectedRows = 0;

            using (SqlConnection con =
                new SqlConnection(p_strConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                ConfigureCommand(
                    cmd,
                    commandText,
                    commandType,
                    commandTimeout,
                    oraParameters);

                con.Open();
                cmd.Connection = con;
                cmd.Prepare();

                for (int row = 0; row < rowCount; row++)
                {
                    for (int col = 0; col < columnCount; col++)
                    {
                        string value = paramValues[row, col];

                        cmd.Parameters[col].Value =
                            string.IsNullOrEmpty(value)
                                ? (object)DBNull.Value
                                : value;
                    }

                    if (m_SQLTraceMode.Equals("on"))
                    {
                        SQLTrace(
                            m_SQLTracePath,
                            cmd,
                            row == rowCount - 1);
                    }

                    affectedRows += cmd.ExecuteNonQuery();
                }
            }

            return affectedRows;
        }

        #endregion

        #region ==== SQL 문을 실행(GetDataSet) ====

        /// <summary>
        /// DataSet 형태로 데이터 반환
        /// 기본 디비연결문과 타임아웃시간을 가진다.
        /// </summary>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>dataset</returns>
        public static DataSet GetDataSet(string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return GetDataSet(COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// DataSet 형태로 데이터 반환
        /// 기본 디비연결문을 가진다.
        /// </summary>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>dataset</returns>
        public static DataSet GetDataSet(int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return GetDataSet(strConnectionString, commandTimeout, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// DataTable 형태로 데이터 반환
        /// 기본 디비연결문과 타임아웃시간을 가진다.
        /// </summary>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>DataTable</returns>
        public static DataTable GetDataTable(string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return GetDataTable(COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// DataTable 형태로 데이터 반환
        /// 기본 디비연결문을 가진다.
        /// </summary>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>DataTable</returns>
        public static DataTable GetDataTable(int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return GetDataTable(strConnectionString, commandTimeout, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// DataSet 형태로 데이터 반환
        /// 기본 타임아웃시간을 가진다.
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>dataset</returns>
        public static DataSet GetDataSet(string p_strConnectionString, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return GetDataSet(p_strConnectionString, COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }


        /// <summary>
        /// DB 커넥션 상태를 가져옵니다.
        /// </summary>
        /// <returns>DB 커넥션 상태</returns>
        public static bool DBConnectState()
        {
            try
            {
                using (SqlConnection con =
                    new SqlConnection(strConnectionString))
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT 1";
                    cmd.CommandTimeout = COMMAND_TIMEOUT;

                    con.Open();

                    object result = cmd.ExecuteScalar();

                    return result != null &&
                        result != DBNull.Value;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        public static DataTable GetDataTable(string p_strConnectionString, int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            WriteDebugCommand(commandText, oraParameters);

            DataTable dtReturn = new DataTable();

            using (SqlConnection con =
                new SqlConnection(p_strConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                ConfigureCommand(
                    cmd,
                    commandText,
                    commandType,
                    commandTimeout,
                    oraParameters);

                if (m_SQLTraceMode.Equals("on"))
                {
                    SQLTrace(m_SQLTracePath, cmd, true);
                }

                con.Open();
                cmd.Connection = con;

                using (SqlDataAdapter da =
                    new SqlDataAdapter(cmd))
                {
                    da.Fill(dtReturn);
                }
            }

            return dtReturn;
        }


        /// <summary>
        /// DataSet 형태로 데이터 반환
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>dataset</returns>
        public static DataSet GetDataSet(string p_strConnectionString, int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            WriteDebugCommand(commandText, oraParameters);

            DataSet dsReturn = new DataSet();

            using (SqlConnection con =
                new SqlConnection(p_strConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                ConfigureCommand(
                    cmd,
                    commandText,
                    commandType,
                    commandTimeout,
                    oraParameters);

                if (m_SQLTraceMode.Equals("on"))
                {
                    SQLTrace(m_SQLTracePath, cmd, true);
                }

                con.Open();
                cmd.Connection = con;

                using (SqlDataAdapter da =
                    new SqlDataAdapter(cmd))
                {
                    da.Fill(dsReturn);
                }
            }

            return dsReturn;
        }

        #endregion

        #region ==== SQL 문을 실행(SqlDataReader) ====

        /// <summary>
        /// SqlDataReader 형태로 데이터 반환
        /// 기본 연결문과 타임아웃시간을 가진다.
        /// </summary>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문 타입</param>
        /// <returns>SqlDataReader</returns>
        public static SqlDataReader GetSqlDataReader(string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return GetSqlDataReader(COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// SqlDataReader 형태로 데이터 반환
        /// 기본 연결문을 가진다.
        /// </summary>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문 타입</param>
        /// <returns>SqlDataReader</returns>
        public static SqlDataReader GetSqlDataReader(int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return GetSqlDataReader(strConnectionString, commandTimeout, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// SqlDataReader 형태로 데이터 반환
        /// 기본 타임아웃시간을 가진다.
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문 타입</param>
        /// <returns>SqlDataReader</returns>
        public static SqlDataReader GetSqlDataReader(string p_strConnectionString, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return GetSqlDataReader(p_strConnectionString, COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// SqlDataReader 형태로 데이터 반환
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문 타입</param>
        /// <returns>SqlDataReader</returns>
        public static SqlDataReader GetSqlDataReader(string p_strConnectionString, int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            SqlConnection con = null;
            SqlCommand cmd = null;

            try
            {
                WriteDebugCommand(commandText, oraParameters);

                con = new SqlConnection(p_strConnectionString);
                con.Open();

                cmd = con.CreateCommand();

                ConfigureCommand(
                    cmd,
                    commandText,
                    commandType,
                    commandTimeout,
                    oraParameters);

                if (m_SQLTraceMode.Equals("on"))
                {
                    SQLTrace(m_SQLTracePath, cmd, true);
                }

                return cmd.ExecuteReader(
                    CommandBehavior.CloseConnection);
            }
            catch
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }

                if (con != null)
                {
                    con.Dispose();
                }

                throw;
            }
            finally
            {
                // 기존 public API가 SqlDataReader만 반환하므로
                // 성공 경로에서는 Reader 사용 중 Command를 유지한다.
                // Connection은 Reader Close/Dispose 시 CloseConnection으로 닫힌다.
            }
        }

        #endregion

        #region ==== Command Helpers ====

        private static void ConfigureCommand(
            SqlCommand cmd,
            string commandText,
            CommandType commandType,
            int commandTimeout,
            SqlParameter[] parameters)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));

            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentException(
                    "CommandText가 필요합니다.",
                    nameof(commandText));
            }

            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = commandTimeout;

            if (parameters == null)
                return;

            foreach (SqlParameter parameter in parameters)
            {
                if (parameter == null)
                {
                    throw new ArgumentException(
                        "Parameter 배열에 null 항목이 있습니다.",
                        nameof(parameters));
                }

                AddParameter(cmd, parameter);
            }
        }

        private static void WriteDebugCommand(
            string commandText,
            SqlParameter[] parameters)
        {
            string strData =
                (commandText ?? string.Empty) + " : ";

            if (parameters != null)
            {
                foreach (SqlParameter parameter in parameters)
                {
                    strData +=
                        " , " +
                        string.Format(
                            "{0}",
                            parameter == null
                                ? null
                                : parameter.Value);
                }
            }

            System.Diagnostics.Debug.WriteLine(strData);
        }

        #endregion

        #region ==== SQLTrace ====

        private static void SQLTrace(string SQLTracePath, SqlCommand cmd, bool markEndLine)
        {
            System.Text.StringBuilder oBuilder = new System.Text.StringBuilder();

            oBuilder.Append(System.DateTime.Now.ToString());
            oBuilder.Append("\r\n");
            oBuilder.Append(cmd.CommandText);

            if (cmd.Parameters.Count != 0)
            {
                if (cmd.CommandType == CommandType.Text)
                {
                    for (int iElemCnt = 0; iElemCnt < cmd.Parameters.Count; iElemCnt++)
                    {
                        oBuilder.Replace(cmd.Parameters[iElemCnt].ParameterName, SqlParameterValue2String(cmd.Parameters[iElemCnt].SqlDbType, cmd.Parameters[iElemCnt].Value));
                    }
                }
                else
                {
                    for (int iElemCnt = 0; iElemCnt < cmd.Parameters.Count; iElemCnt++)
                    {
                        oBuilder.Append("\r\n");
                        oBuilder.Append(cmd.Parameters[iElemCnt].ParameterName);
                        oBuilder.Append(" = ");
                        oBuilder.Append(SqlParameterValue2String(cmd.Parameters[iElemCnt].SqlDbType, cmd.Parameters[iElemCnt].Value));
                    }
                }

                if (markEndLine) oBuilder.Append("\r\n-------------------------------------------------------------");
                string sContents = oBuilder.ToString();

                string sFileName = string.Format("{0}_SQLQueryTrc_{1}.log", System.Net.Dns.GetHostName().ToLower(),
                    System.DateTime.Now.ToShortDateString());

            }
            // File 로그 남기기
        }

        #endregion

        #region ==== AddParameter ====
        private static void AddParameter(SqlCommand cmd, SqlParameter param)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            if (param.Value == null ||
                (param.Value is string &&
                 ((string)param.Value).Length == 0))
            {
                param.Value = DBNull.Value;
            }

            cmd.Parameters.Add(param);
        }
        #endregion

        #region ==== SqlParameterValue2String ====
        private static string SqlParameterValue2String(SqlDbType tp, object parameterValue)
        {
            string strReturn = "NULL";

            if (parameterValue != null)
            {
                if (parameterValue == System.DBNull.Value)
                    strReturn = "NULL";
                else
                {
                    switch (tp)
                    {
                        case SqlDbType.Char:
                        case SqlDbType.VarChar:
                        case SqlDbType.NChar:
                        case SqlDbType.NVarChar:
                            strReturn = string.Concat(
                                "'",
                                parameterValue.ToString().Replace("'", "''"),
                                "'");
                            break;
                        //case SqlDbType.Blob:
                        //    strReturn = "<OracleBlob>";
                        //    break;
                        //case SqlDbType.Clob:
                        //    strReturn = "<CLOB>";
                        //    break;
                        //case SqlDbType.DateTime:
                        //    strReturn = "<OracleDate>";
                        //    break;
                        //case SqlDbType.Raw:
                        //    strReturn = "<OracleBinary>";
                        //    break;
                        //case SqlDbType.LongRaw:
                        //    strReturn = "<OracleBinary>";
                        //    break;
                        //case SqlDbType.Byte:
                        //    strReturn = "<Binary>";
                        //    break;
                        default:
                            strReturn = parameterValue.ToString();
                            break;
                    }
                }
            }

            return strReturn;
        }
        #endregion

        #region transaction
        private static void CleanupTransaction()
        {
            if (m_SqlTransaction != null)
            {
                m_SqlTransaction.Dispose();
                m_SqlTransaction = null;
            }

            if (m_SqlConnection != null)
            {
                m_SqlConnection.Dispose();
                m_SqlConnection = null;
            }
        }

        public static bool BeginTransaction()
        {
            if (m_SqlTransaction != null)
                return false;

            CleanupTransaction();

            try
            {
                m_SqlConnection =
                    new SqlConnection(strConnectionString);

                m_SqlConnection.Open();

                m_SqlTransaction =
                    m_SqlConnection.BeginTransaction();

                return true;
            }
            catch
            {
                CleanupTransaction();
                throw;
            }
        }


        public static bool Commit()
        {
            if (m_SqlTransaction == null)
                return false;

            try
            {
                m_SqlTransaction.Commit();
                return true;
            }
            finally
            {
                CleanupTransaction();
            }
        }

        public static bool ExecuteNonQuery(string query)
        {
            if (m_SqlConnection == null)
            {
                throw new InvalidOperationException(
                    "Transaction이 시작되지 않았습니다.");
            }

            if (m_SqlConnection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException(
                    "DB 연결이 열려 있지 않습니다.");
            }

            using (SqlCommand command = m_SqlConnection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = query;
                command.CommandTimeout = COMMAND_TIMEOUT;

                if (m_SqlTransaction != null)
                {
                    command.Transaction = m_SqlTransaction;
                }

                int affectedRows = command.ExecuteNonQuery();

                return affectedRows > 0;
            }
        }

        public static bool Rollback()
        {
            if (m_SqlTransaction == null)
                return false;

            try
            {
                m_SqlTransaction.Rollback();
                return true;
            }
            finally
            {
                CleanupTransaction();
            }
        }

        public static void DisConnect()
        {
            CleanupTransaction();
        }


        public static void Dispose()
        {
            DisConnect();
        }



        #endregion
    }

    #endregion

    #region[MSSQLDbAgent]
    /// <summary>
    /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    public class MSSQLDbAgent : IDisposable
    {
        // Member Variables
        protected SqlConnection m_DBCon;
        protected SqlCommand m_DBCmd;
        protected SqlDataReader m_DataReader, m_DataReader1;
        protected SqlTransaction m_DBTrans;

        //protected RsArray				cRsArray = new RsArray();

        protected int m_nRows;
        protected string m_strSQL;
        protected string m_strRET;



        public int COMMAND_TIMEOUT = 30;

        /// <summary>
        /// 생성자입니다.
        /// </summary>
        public MSSQLDbAgent()
        {
            m_nRows = 0;
        }

        #region -- DBConnectState/GetOracleConnection

        /// <summary>
        /// OracleConnection을 가져옵니다.
        /// </summary>
        /// <returns>OracleConnection</returns>
        public SqlConnection GetMsSqlConnection()
        {
            return m_DBCon;
        }


        /// <summary>
        /// DB 커넥션 상태를 가져옵니다.
        /// </summary>
        /// <returns>DB 커넥션 상태</returns>
        public bool DBConnectState()
        {
            if (!IsConnected())
                return false;

            try
            {
                using (SqlCommand cmd = CreateCommand(
                    "SELECT 1",
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null))
                {
                    object result = cmd.ExecuteScalar();

                    return result != null &&
                        result != DBNull.Value;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);

                try
                {
                    DBDisConnect();
                }
                catch (Exception disconnectException)
                {
                    System.Diagnostics.Debug.WriteLine(
                        disconnectException.Message);
                }

                return false;
            }
        }
        #endregion

        #region -- DBConnect / DBDisConnect
        /// <summary>
        /// DBConnect
        /// </summary>
        /// <param name="p_strConnectionString"></param>
        /// <returns></returns>
        public bool DBConnect(string p_strConnectionString)
        {
            if (string.IsNullOrWhiteSpace(p_strConnectionString))
            {
                throw new ArgumentException(
                    "Connection string이 필요합니다.",
                    nameof(p_strConnectionString));
            }

            DBDisConnect();

            try
            {
                m_DBCon =
                    new SqlConnection(p_strConnectionString);

                m_DBCon.Open();

                m_DBTrans =
                    m_DBCon.BeginTransaction();

                return true;
            }
            catch
            {
                CleanupConnection();
                throw;
            }
        }

        /// <summary>
        /// DBConnect
        /// </summary>
        /// <param name="p_strUSER">UserName</param>
        /// <param name="p_strPW">PassWord</param>
        /// <param name="p_strAlias">Alias</param>
        /// <returns></returns>
        public bool DBConnect(string p_strUSER, string p_strPW, string p_strAlias)
        {
            try
            {
                string connectionString =
                    "Pooling=false;user id=" +
                    (p_strUSER ?? string.Empty) +
                    ";data source=" +
                    (p_strAlias ?? string.Empty) +
                    ";password=" +
                    (p_strPW ?? string.Empty);

                return DBConnect(connectionString);
            }
            catch
            {
                return false;
            }
        }






        /// <summary>
        /// DBConnect
        /// </summary>
        /// <param name="p_strUSER">UserName</param>
        /// <param name="p_strPW">PassWord</param>
        /// <param name="p_strAlias">Alias</param>
        /// <param name="p_strErrCode">ErrorCode(out)</param>
        /// <param name="p_strErrText">ErrorText(out)</param>
        /// <returns></returns>
        public bool DBConnect(string p_strUSER, string p_strPW, string p_strAlias, ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                string connectionString =
                    "Pooling=false;user id=" +
                    (p_strUSER ?? string.Empty) +
                    ";data source=" +
                    (p_strAlias ?? string.Empty) +
                    ";password=" +
                    (p_strPW ?? string.Empty);

                DBConnect(connectionString);

                p_strErrCode = string.Empty;
                p_strErrText = string.Empty;

                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(
                    e,
                    ref p_strErrCode,
                    ref p_strErrText);

                return false;
            }
        }


        /// <summary>
        /// DB 연결을 끊습니다.
        /// </summary>
        public void DBDisConnect()
        {
            CleanupConnection();
        }


        /// <summary>
        /// DB 연결을 끊습니다.
        /// </summary>
        /// <param name="p_strErrCode">ErrorCode(out)</param>
        /// <param name="p_strErrText">ErrorText(out)</param>
        /// <returns></returns>
        public bool DBDisConnect(
            ref string p_strErrCode,
            ref string p_strErrText)
        {
            try
            {
                DBDisConnect();

                p_strErrCode = string.Empty;
                p_strErrText = string.Empty;

                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(
                    e,
                    ref p_strErrCode,
                    ref p_strErrText);

                return false;
            }
        }

        #endregion

        #region Commit / RollBack
        /// <summary>
        /// Commit
        /// </summary>
        public void Commit()
        {
            EnsureTransaction();

            m_DBTrans.Commit();
            m_DBTrans.Dispose();

            m_DBTrans =
                m_DBCon.BeginTransaction();
        }


        /// <summary>
        /// Commit
        /// </summary>
        /// <param name="p_strErrCode">ErrorCode(out)</param>
        /// <param name="p_strErrText">ErrorText(out)</param>
        /// <returns>성공여부</returns>
        public bool Commit(ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                Commit();

                p_strErrCode = string.Empty;
                p_strErrText = string.Empty;

                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(
                    e,
                    ref p_strErrCode,
                    ref p_strErrText);

                return false;
            }
        }


        /// <summary>
        /// RollBack
        /// </summary>
        public void RollBack()
        {
            EnsureTransaction();

            m_DBTrans.Rollback();
            m_DBTrans.Dispose();

            m_DBTrans =
                m_DBCon.BeginTransaction();
        }


        /// <summary>
        /// RollBack
        /// </summary>
        /// <param name="p_strErrCode">ErrorCode(out)</param>
        /// <param name="p_strErrText">ErrorText(out)</param>
        /// <returns>성공여부</returns>
        public bool RollBack(ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                RollBack();

                p_strErrCode = string.Empty;
                p_strErrText = string.Empty;

                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(
                    e,
                    ref p_strErrCode,
                    ref p_strErrText);

                return false;
            }
        }
        #endregion

        #region MssqlAgent : public void GetErrorCode(Exception e, ref string p_strErrCode, ref string p_strErrText)
        /// <summary>
        /// Exception에서 MSSQL 오류 코드와 메시지를 추출합니다.
        /// </summary>
        public void GetErrorCode(Exception e, ref string p_strErrCode, ref string p_strErrText)
        {
            SqlException sqlEx = e as SqlException;

            if (sqlEx != null)
            {
                p_strErrCode =
                    sqlEx.Number.ToString();

                p_strErrText =
                    sqlEx.Message;

                return;
            }

            p_strErrCode = "AC7901";
            p_strErrText =
                e == null
                    ? string.Empty
                    : e.Message;
        }

        #endregion

        #region MssqlAgent : public void MessageFormat(Exception e, string p_strTitle, string p_strAction, string p_strAdjust,

        /// <summary>
        /// 해당 에러 메시지를 형식에 맞게 변경시킨다. 
        /// </summary>
        /// <param name="e">Exception 변수</param>
        /// <param name="p_strTitle">Title</param>
        /// <param name="p_strAction">Action</param>
        /// <param name="p_strAdjust">Adjus</param>
        /// <param name="p_strErrCode">Error Code</param>
        /// <param name="p_strErrText">Error Text</param>
        /// <param name="p_strCondition">Condition</param>
        public void MessageFormat(Exception e, string p_strTitle, string p_strAction, string p_strAdjust,
            ref string p_strErrCode, ref string p_strErrText, string p_strCondition)
        {
            GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
            p_strErrText = string.Format("{0} {1} {2} {3}", p_strTitle, p_strAction, p_strAdjust, p_strCondition) + p_strErrText;
        }


        /// <summary>
        /// 해당 메시지를 형식에 맞게 변경시킨다.
        /// </summary>
        /// <param name="p_strTitle">Title</param>
        /// <param name="p_strAction">Action</param>
        /// <param name="p_strAdjust">Adjus</param>
        /// <param name="p_strErrText">Error Text</param>
        /// <param name="p_strCondition">Condition</param>
        public void MessageFormat(string p_strTitle, string p_strAction, string p_strAdjust, ref string p_strErrText, string p_strCondition)
        {
            p_strErrText = string.Format("{0} {1} {2} {3}", p_strTitle, p_strAction, p_strAdjust, p_strCondition) + p_strErrText;
        }
        #endregion

        #region Internal Helpers
        private bool IsConnected()
        {
            return m_DBCon != null &&
                m_DBCon.State == ConnectionState.Open;
        }

        private void EnsureConnected()
        {
            if (!IsConnected())
            {
                throw new InvalidOperationException(
                    "MSSQL database is not connected.");
            }
        }

        private void EnsureTransaction()
        {
            EnsureConnected();

            if (m_DBTrans == null)
            {
                throw new InvalidOperationException(
                    "MSSQL transaction is not active.");
            }
        }

        private SqlCommand CreateCommand(
            string commandText,
            CommandType commandType,
            int commandTimeout,
            SqlParameter[] parameters)
        {
            EnsureConnected();

            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentException(
                    "CommandText가 필요합니다.",
                    nameof(commandText));
            }

            SqlCommand cmd = m_DBCon.CreateCommand();

            try
            {
                cmd.CommandText = commandText;
                cmd.CommandType = commandType;
                cmd.CommandTimeout = commandTimeout;

                if (m_DBTrans != null)
                {
                    cmd.Transaction = m_DBTrans;
                }

                if (parameters != null)
                {
                    foreach (SqlParameter parameter in parameters)
                    {
                        AddParameter(cmd, parameter);
                    }
                }

                return cmd;
            }
            catch
            {
                cmd.Dispose();
                throw;
            }
        }

        private void DisposeCurrentReader()
        {
            if (m_DataReader != null)
            {
                m_DataReader.Dispose();
                m_DataReader = null;
            }

            if (m_DataReader1 != null)
            {
                m_DataReader1.Dispose();
                m_DataReader1 = null;
            }
        }

        private void DisposeCurrentCommand()
        {
            if (m_DBCmd != null)
            {
                m_DBCmd.Dispose();
                m_DBCmd = null;
            }
        }

        private void CleanupConnection()
        {
            DisposeCurrentReader();
            DisposeCurrentCommand();

            if (m_DBTrans != null)
            {
                m_DBTrans.Dispose();
                m_DBTrans = null;
            }

            if (m_DBCon != null)
            {
                m_DBCon.Dispose();
                m_DBCon = null;
            }
        }

        public void Dispose()
        {
            DBDisConnect();
            GC.SuppressFinalize(this);
        }
        #endregion

        #region[ExecuteNonQuery]
        /// <summary>
        /// SQL 문을 실행한다
        /// </summary>
        /// <param name="connectionHint"></param>
        /// <param name="commandText"></param>
        /// <param name="oraParameters"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return ExecuteNonQuery(COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// SQL 문을 실행한다
        /// </summary>
        /// <param name="connectionHint"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandText"></param>
        /// <param name="oraParameters"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            using (SqlCommand cmd = CreateCommand(
                commandText,
                commandType,
                commandTimeout,
                oraParameters))
            {
                return cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region MssqlAgent : public bool ExecuteNonQuery( string p_strSQL, ref string p_strErrCode, ref string p_strErrText)
        /// <summary>
        /// SQL문을 실행합니다.
        /// </summary>
        /// <param name="p_strSQL">SQL문</param>
        /// <param name="p_strErrCode">Error Code(out)</param>
        /// <param name="p_strErrText">Error Text(out)</param>
        /// <returns>성공여부</returns>
        public bool ExecuteNonQuery(string p_strSQL, ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                using (SqlCommand cmd = CreateCommand(
                    p_strSQL,
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null))
                {
                    m_nRows = cmd.ExecuteNonQuery();
                }

                p_strErrCode = string.Empty;
                p_strErrText = string.Empty;

                return m_nRows > 0;
            }
            catch (Exception e)
            {
                GetErrorCode(
                    e,
                    ref p_strErrCode,
                    ref p_strErrText);

                return false;
            }
        }
        /// <summary>
        /// SQL문을 실행합니다.
        /// </summary>
        /// <param name="p_strSQL">SQL문</param>
        /// <param name="p_nLongSize">InitialLONGFetchSize</param>
        /// <param name="p_strErrCode">Error Code(out)</param>
        /// <param name="p_strErrText">Error Text(out)</param>
        /// <returns>성공여부</returns>
        //public bool ExecuteNonQuery(string p_strSQL, int p_nLongSize, ref string p_strErrCode, ref string p_strErrText)
        //{
        //    try
        //    {
        //        m_DBCmd = m_DBCon.CreateCommand();
        //        m_DBCmd.CommandType = CommandType.Text;
        //        m_DBCmd.InitialLONGFetchSize = p_nLongSize;
        //        m_DBCmd.CommandText = p_strSQL;
        //        m_nRows = m_DBCmd.ExecuteNonQuery();

        //        if (m_nRows == 0)
        //        {
        //            p_strErrCode = OracleDBDef.ORAMID_NOFOUND;
        //            return false;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
        //        return false;
        //    }
        //    finally
        //    {
        //        m_DBCmd.Dispose();
        //    }

        //    return true;
        //}
        #endregion

        #region MssqlAgent : public SqlDataReader ExecuteReader( string p_strSQL, ref string p_strErrCode, ref string p_strErrText)
        /// <summary>
        /// SQL문을 실행합니다.
        /// SqlDataReader 형태로 데이터 반환합니다.
        /// </summary>
        /// <param name="p_strSQL">SQL문</param>
        /// <param name="p_strErrCode">Error Code(out)</param>
        /// <param name="p_strErrText">Error Text(out)</param>
        /// <returns>SqlDataReader</returns>
        public SqlDataReader ExecuteReader(string p_strSQL, ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                EnsureConnected();

                DisposeCurrentReader();
                DisposeCurrentCommand();

                m_DBCmd = CreateCommand(
                    p_strSQL,
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null);

                m_DataReader =
                    m_DBCmd.ExecuteReader();

                p_strErrCode = string.Empty;
                p_strErrText = string.Empty;

                return m_DataReader;
            }
            catch (Exception e)
            {
                DisposeCurrentReader();
                DisposeCurrentCommand();

                GetErrorCode(
                    e,
                    ref p_strErrCode,
                    ref p_strErrText);

                return null;
            }
        }
        /// <summary>
        /// SQL문을 실행합니다.
        /// OracleDataReader 형태로 데이터 반환합니다.
        /// </summary>
        /// <param name="p_strSQL">SQL문</param>
        /// <param name="p_nLongSize">InitialLONGFetchSize</param>
        /// <param name="p_strErrCode">Error Code(out)</param>
        /// <param name="p_strErrText">Error Text(out)</param>
        /// <returns></returns>
        //public OracleDataReader ExecuteReader(string p_strSQL, int p_nLongSize, ref string p_strErrCode, ref string p_strErrText)
        //{
        //    try
        //    {
        //        m_DBCmd = m_DBCon.CreateCommand();
        //        m_DBCmd.CommandType = CommandType.Text;
        //        m_DBCmd.InitialLONGFetchSize = p_nLongSize;
        //        m_DBCmd.CommandText = p_strSQL;
        //        m_DataReader = m_DBCmd.ExecuteReader();

        //        return m_DataReader;
        //    }
        //    catch (Exception e)
        //    {
        //        GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
        //        return null;
        //    }
        //    finally
        //    {
        //        m_DBCmd.Dispose();
        //    }
        //}

        #endregion

        #region SqlAgent : public bool ExecuteScalar(string p_strSQL, ref int p_nValue, ref string p_strErrCode, ref string p_strErrText)
        /// <summary>
        /// SQL문을 실행하고 int 값을 반환합니다.
        /// </summary>
        /// <param name="p_strSQL">SQL문</param>
        /// <param name="p_nValue">int Value(out)</param>
        /// <param name="p_strErrCode">Error Code(out)</param>
        /// <param name="p_strErrText">Error Text(out)</param>
        /// <returns>성공여부</returns>
        public bool ExecuteScalar(string p_strSQL, ref int p_nValue, ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                using (SqlCommand cmd = CreateCommand(
                    p_strSQL,
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null))
                {
                    object result = cmd.ExecuteScalar();

                    if (result == null ||
                        result == DBNull.Value)
                    {
                        p_nValue = 0;
                    }
                    else
                    {
                        int value;

                        p_nValue =
                            int.TryParse(
                                result.ToString(),
                                out value)
                                ? value
                                : 0;
                    }
                }

                p_strErrCode = string.Empty;
                p_strErrText = string.Empty;

                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(
                    e,
                    ref p_strErrCode,
                    ref p_strErrText);

                return false;
            }
        }

        /// <summary>
        /// SQL문을 실행합니다.
        /// </summary>
        /// <param name="p_strSQL">SQL문</param>
        /// <param name="p_lgValue">long Value(out)</param>
        /// <param name="p_strErrCode">Error Code(out)</param>
        /// <param name="p_strErrText">Error Text(out)</param>
        /// <returns>성공여부</returns>
        public bool ExecuteScalar(string p_strSQL, ref long p_lgValue, ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                using (SqlCommand cmd = CreateCommand(
                    p_strSQL,
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null))
                {
                    object result = cmd.ExecuteScalar();

                    if (result == null ||
                        result == DBNull.Value)
                    {
                        p_lgValue = 0L;
                    }
                    else
                    {
                        long value;

                        p_lgValue =
                            long.TryParse(
                                result.ToString(),
                                out value)
                                ? value
                                : 0L;
                    }
                }

                p_strErrCode = string.Empty;
                p_strErrText = string.Empty;

                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(
                    e,
                    ref p_strErrCode,
                    ref p_strErrText);

                return false;
            }
        }
        #endregion

        #region MssqlAgent : public bool ExecuteScalar(string p_strSQL, ref string p_strValue, ref string p_strErrCode, ref string p_strErrText)
        /// <summary>
        /// SQL문을 실행하고 string 값을 반환합니다.
        /// </summary>
        /// <param name="p_strSQL">SQL문</param>
        /// <param name="p_strValue">string Value(out)</param>
        /// <param name="p_strErrCode">Error Code(out)</param>
        /// <param name="p_strErrText">Error Text(out)</param>
        /// <returns>성공여부</returns>
        public bool ExecuteScalar(string p_strSQL, ref string p_strValue, ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                using (SqlCommand cmd = CreateCommand(
                    p_strSQL,
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null))
                {
                    object result = cmd.ExecuteScalar();

                    p_strValue =
                        result == null ||
                        result == DBNull.Value
                            ? string.Empty
                            : result.ToString();
                }

                p_strErrCode = string.Empty;
                p_strErrText = string.Empty;

                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(
                    e,
                    ref p_strErrCode,
                    ref p_strErrText);

                return false;
            }
        }
        #endregion


        #region ==== AddParameter ====
        private void AddParameter(SqlCommand cmd, SqlParameter param)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            if (param.Value == null ||
                (param.Value is string &&
                 ((string)param.Value).Length == 0))
            {
                param.Value = DBNull.Value;
            }

            cmd.Parameters.Add(param);
        }
        #endregion
    }
    #endregion
}