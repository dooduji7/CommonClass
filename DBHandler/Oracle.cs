using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
namespace DBHandler
{
    #region[OracleDbAccess]
    /// <summary>
    /// OracleDbAccess에 대한 요약 설명입니다.
    /// </summary>
    public class OracleDbAccess
    {
        #region[OracleDbAccess]
        /// <summary>
        /// OracleDbAccess 생성자 입니다.
        /// </summary>
        public OracleDbAccess() { }
        #endregion

        #region[member]
        public static int COMMAND_TIMEOUT = 30;

        internal static string m_SQLTraceMode = "";//System.Configuration.ConfigurationManager.AppSettings["SQLTraceMode"].Trim().ToLower();
        internal static string m_SQLTracePath = "";//System.Configuration.ConfigurationManager.AppSettings["SQLTracePath"].Trim();


        private static string m_User = "";
        private static string m_Pass = "";
        private static string m_Alias = "";
        #endregion

        #region[ConnectionString]
        /// <summary>
        /// 완성차 ConnectionString 입니다.
        /// </summary>
        private static string strConnectionString = "user id= " + m_User + ";data source= " + m_Alias + ";password= " + m_Pass + ";Connection Lifetime=300; Max Pool Size = 3";

        public static void ConnectionString(string strAlias, string strUser, string strPassword)
        {
            strConnectionString = "user id= " + strUser + ";data source= " + strAlias + ";password= " + strPassword + ";Connection Lifetime=300; Max Pool Size = 3";
        }

        public static void ConnectionString(string strAlias, string strUser, string strPassword, int iLifeTime, int iMaxPoolSize)
        {
            strConnectionString = "user id= " + strUser + ";data source= " + strAlias + ";password= " + strPassword + ";Connection Lifetime=" + iLifeTime.ToString() + "; Max Pool Size = " + iMaxPoolSize.ToString();
        }
        #endregion

        #region[Parameter Helpers]
        private static OracleParameter[] CreateInputParameters(string strID, string strVAL, char split)
        {
            if (string.IsNullOrWhiteSpace(strID))
                return null;

            string[] paramIds = strID.Split(split);
            string[] paramValues = (strVAL ?? string.Empty).Split(split);

            if (paramIds.Length != paramValues.Length)
                throw new ArgumentException("Parameter name/value count does not match.");

            OracleParameter[] parameters = new OracleParameter[paramIds.Length];

            for (int i = 0; i < paramIds.Length; i++)
            {
                parameters[i] = new OracleParameter(paramIds[i], paramValues[i])
                {
                    Direction = ParameterDirection.Input
                };
            }

            return parameters;
        }

        private static OracleParameter[] CreateInputParametersWithCursor(string strID, string strVAL, char split)
        {
            OracleParameter[] inputs = CreateInputParameters(strID, strVAL, split);
            int inputCount = inputs == null ? 0 : inputs.Length;
            OracleParameter[] parameters = new OracleParameter[inputCount + 1];

            if (inputs != null)
                Array.Copy(inputs, parameters, inputCount);

            parameters[inputCount] = new OracleParameter("P_CURSOR", OracleDbType.RefCursor)
            {
                Direction = ParameterDirection.Output
            };

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
            OracleParameter[] parameters = CreateInputParameters(strID, strVAL, split);
            Execute(strSpName, parameters, CommandType.StoredProcedure);
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
        public static DataSet ExecuteProcedureResult(string strSpName, string strID, string strVAL)
        {
            return ExecuteProcedureResult(strSpName, strID, strVAL, '@');
        }

        public static DataSet ExecuteProcedureResult(string strSpName, string strID, string strVAL, char split)
        {
            OracleParameter[] parameters = CreateInputParametersWithCursor(strID, strVAL, split);
            return GetDataSet(strSpName, parameters, CommandType.StoredProcedure);
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
        public static int Execute(string commandText, OracleParameter[] oraParameters, CommandType commandType)
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
        public static int Execute(int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
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
        public static int Execute(string p_strConnectionString, string commandText, OracleParameter[] oraParameters, CommandType commandType)
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
        public static int Execute(string p_strConnectionString, int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            WriteDebugCommand(commandText, oraParameters);

            using (OracleConnection con = new OracleConnection(p_strConnectionString))
            using (OracleCommand cmd = new OracleCommand())
            {
                ConfigureCommand(cmd, commandText, commandType, commandTimeout, oraParameters);

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
        public static object ExecuteScalar(string commandText, OracleParameter[] oraParameters, CommandType commandType)
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
        public static object ExecuteScalar(int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
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
        public static object ExecuteScalar(string p_strConnectionString, string commandText, OracleParameter[] oraParameters, CommandType commandType)
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
        public static object ExecuteScalar(string p_strConnectionString, int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            WriteDebugCommand(commandText, oraParameters);

            using (OracleConnection con = new OracleConnection(p_strConnectionString))
            using (OracleCommand cmd = new OracleCommand())
            {
                ConfigureCommand(cmd, commandText, commandType, commandTimeout, oraParameters);

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
        public static int ExecuteMultiple(string commandText, OracleParameter[] oraParameters, string[,] paramValues, CommandType commandType)
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
        public static int ExecuteMultiple(int commandTimeout, string commandText, OracleParameter[] oraParameters, string[,] paramValues, CommandType commandType)
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
        public static int ExecuteMultiple(string p_strConnectionString, string commandText, OracleParameter[] oraParameters, string[,] paramValues, CommandType commandType)
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
        public static int ExecuteMultiple(string p_strConnectionString, int commandTimeout, string commandText, OracleParameter[] oraParameters, string[,] paramValues, CommandType commandType)
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
                    "Parameter count and value column count do not match.",
                    nameof(paramValues));
            }

            if (rowCount == 0)
                return 0;

            WriteDebugCommand(commandText, oraParameters);

            int affectedRows = 0;

            using (OracleConnection con = new OracleConnection(p_strConnectionString))
            using (OracleCommand cmd = new OracleCommand())
            {
                ConfigureCommand(cmd, commandText, commandType, commandTimeout, oraParameters);

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
        public static DataSet GetDataSet(string commandText, OracleParameter[] oraParameters, CommandType commandType)
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
        public static DataSet GetDataSet(int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return GetDataSet(strConnectionString, commandTimeout, commandText, oraParameters, commandType);
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
        public static DataSet GetDataSet(string p_strConnectionString, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return GetDataSet(p_strConnectionString, COMMAND_TIMEOUT, commandText, oraParameters, commandType);
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
        public static DataSet GetDataSet(string p_strConnectionString, int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            WriteDebugCommand(commandText, oraParameters);

            DataSet dsReturn = new DataSet();

            using (OracleConnection con = new OracleConnection(p_strConnectionString))
            using (OracleCommand cmd = new OracleCommand())
            {
                ConfigureCommand(cmd, commandText, commandType, commandTimeout, oraParameters);

                if (m_SQLTraceMode.Equals("on"))
                {
                    SQLTrace(m_SQLTracePath, cmd, true);
                }

                con.Open();
                cmd.Connection = con;

                using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                {
                    da.Fill(dsReturn);

                }
            }

            return dsReturn;
        }

        #endregion

        #region ==== SQL 문을 실행(GetDataTable) ====

        /// <summary>
        /// DataTable 형태로 데이터 반환
        /// 기본 디비연결문과 타임아웃시간을 가진다.
        /// </summary>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>DataTable</returns>
        public static DataTable GetDataTable(string commandText, OracleParameter[] oraParameters, CommandType commandType)
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
        public static DataTable GetDataTable(int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return GetDataTable(strConnectionString, commandTimeout, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// DataTable 형태로 데이터 반환
        /// 기본 타임아웃시간을 가진다.
        /// </summary>
        /// <param name="pStrConnectionString">연결문</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>DataTable</returns>
        public static DataTable GetDataTable(string pStrConnectionString, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return GetDataTable(pStrConnectionString, COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// DataTable 형태로 데이터 반환
        /// </summary>
        /// <param name="pStrConnectionString">연결문</param>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문자열 타입</param>
        /// <returns>DataTable</returns>
        public static DataTable GetDataTable(string pStrConnectionString, int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            WriteDebugCommand(commandText, oraParameters);

            DataTable dtReturn = new DataTable();

            using (OracleConnection con = new OracleConnection(pStrConnectionString))
            using (OracleCommand cmd = new OracleCommand())
            {
                ConfigureCommand(cmd, commandText, commandType, commandTimeout, oraParameters);

                if (m_SQLTraceMode.Equals("on"))
                {
                    SQLTrace(m_SQLTracePath, cmd, true);
                }

                con.Open();
                cmd.Connection = con;

                using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                {
                    da.Fill(dtReturn);
                }
            }

            return dtReturn;
        }

        #endregion

        #region ==== SQL 문을 실행(OracleDataReader) ====

        /// <summary>
        /// OracleDataReader 형태로 데이터 반환
        /// 기본 연결문과 타임아웃시간을 가진다.
        /// </summary>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문 타입</param>
        /// <returns>OracleDataReader</returns>
        public static OracleDataReader GetOracleDataReader(string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return GetOracleDataReader(COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// OracleDataReader 형태로 데이터 반환
        /// 기본 연결문을 가진다.
        /// </summary>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문 타입</param>
        /// <returns>OracleDataReader</returns>
        public static OracleDataReader GetOracleDataReader(int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return GetOracleDataReader(strConnectionString, commandTimeout, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// OracleDataReader 형태로 데이터 반환
        /// 기본 타임아웃시간을 가진다.
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문 타입</param>
        /// <returns>OracleDataReader</returns>
        public static OracleDataReader GetOracleDataReader(string p_strConnectionString, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return GetOracleDataReader(p_strConnectionString, COMMAND_TIMEOUT, commandText, oraParameters, commandType);
        }

        /// <summary>
        /// OracleDataReader 형태로 데이터 반환
        /// </summary>
        /// <param name="p_strConnectionString">연결문</param>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문 타입</param>
        /// <returns>OracleDataReader</returns>
        public static OracleDataReader GetOracleDataReader(string p_strConnectionString, int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            OracleConnection con = null;
            OracleCommand cmd = null;

            try
            {
                WriteDebugCommand(commandText, oraParameters);

                con = new OracleConnection(p_strConnectionString);
                con.Open();

                cmd = con.CreateCommand();
                ConfigureCommand(cmd, commandText, commandType, commandTimeout, oraParameters);

                if (m_SQLTraceMode.Equals("on"))
                {
                    SQLTrace(m_SQLTracePath, cmd, true);
                }

                // 호출자는 반환된 OracleDataReader를 반드시 Dispose/Close해야 한다.
                // CloseConnection에 의해 Reader 종료 시 Connection도 닫힌다.
                return cmd.ExecuteReader(CommandBehavior.CloseConnection);
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
                // Reader가 반환된 뒤에도 Reader가 Command를 사용할 수 있으므로
                // 성공 경로에서는 Command를 여기서 Dispose하지 않는다.
                // Connection은 CommandBehavior.CloseConnection으로 Reader 종료 시 정리된다.
            }
        }

        #endregion

        #region ==== Command Helpers ====

        private static void ConfigureCommand(
            OracleCommand cmd,
            string commandText,
            CommandType commandType,
            int commandTimeout,
            OracleParameter[] oraParameters)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));

            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentException("CommandText is required.", nameof(commandText));

            cmd.BindByName = true;
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            cmd.CommandTimeout = commandTimeout;

            if (oraParameters == null)
                return;

            foreach (OracleParameter param in oraParameters)
            {
                if (param == null)
                    throw new ArgumentException("Parameter array contains null.", nameof(oraParameters));

                AddParameter(cmd, param);
            }
        }

        private static void WriteDebugCommand(
            string commandText,
            OracleParameter[] oraParameters)
        {
            string strData = (commandText ?? string.Empty) + " : ";

            if (oraParameters != null)
            {
                foreach (OracleParameter param in oraParameters)
                {
                    strData += " , " +
                        string.Format(
                            "{0}",
                            param == null ? null : param.Value);
                }
            }

            System.Diagnostics.Debug.WriteLine(strData);
        }

        #endregion

        #region ==== SQLTrace ====

        private static void SQLTrace(string SQLTracePath, OracleCommand cmd, bool markEndLine)
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
                        oBuilder.Replace(cmd.Parameters[iElemCnt].ParameterName, SqlParameterValue2String(cmd.Parameters[iElemCnt].OracleDbType, cmd.Parameters[iElemCnt].Value));
                    }
                }
                else
                {
                    for (int iElemCnt = 0; iElemCnt < cmd.Parameters.Count; iElemCnt++)
                    {
                        oBuilder.Append("\r\n");
                        oBuilder.Append(cmd.Parameters[iElemCnt].ParameterName);
                        oBuilder.Append(" = ");
                        oBuilder.Append(SqlParameterValue2String(cmd.Parameters[iElemCnt].OracleDbType, cmd.Parameters[iElemCnt].Value));
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
        private static void AddParameter(OracleCommand cmd, OracleParameter param)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            if (param.Value == null ||
                (param.Value is string && ((string)param.Value).Length == 0))
            {
                param.Value = DBNull.Value;
            }

            cmd.Parameters.Add(param);
        }
        #endregion

        #region ==== SqlParameterValue2String ====
        private static string SqlParameterValue2String(OracleDbType tp, object parameterValue)
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
                        case OracleDbType.Char:
                        case OracleDbType.Varchar2:
                        case OracleDbType.NChar:
                        case OracleDbType.NVarchar2:
                            strReturn = string.Concat("'", parameterValue.ToString().Replace("'", "''"), "'");
                            break;
                        case OracleDbType.BFile:
                            strReturn = "<OracleBFile>";
                            break;
                        case OracleDbType.Blob:
                            strReturn = "<OracleBlob>";
                            break;
                        case OracleDbType.Clob:
                            strReturn = "<CLOB>";
                            break;
                        case OracleDbType.Date:
                            strReturn = "<OracleDate>";
                            break;
                        case OracleDbType.Raw:
                            strReturn = "<OracleBinary>";
                            break;
                        case OracleDbType.LongRaw:
                            strReturn = "<OracleBinary>";
                            break;
                        case OracleDbType.Byte:
                            strReturn = "<Binary>";
                            break;
                        default:
                            strReturn = parameterValue.ToString();
                            break;
                    }
                }
            }

            return strReturn;
        }
        #endregion
    }

    #endregion

    #region[OracleDbAgent]
    /// <summary>
    /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    public class OracleDbAgent : IDisposable
    {
        // Member Variables
        protected OracleConnection m_DBCon;
        protected OracleCommand m_DBCmd;
        protected OracleDataReader m_DataReader, m_DataReader1;
        protected OracleTransaction m_DBTrans;

        //protected RsArray				cRsArray = new RsArray();

        protected int m_nRows;
        protected string m_strSQL;
        protected string m_strRET;


        public int COMMAND_TIMEOUT = 30;

        /// <summary>
        /// 생성자입니다.
        /// </summary>
        public OracleDbAgent()
        {
            m_nRows = 0;
        }

        #region -- DBConnectState/GetOracleConnection

        /// <summary>
        /// OracleConnection을 가져옵니다.
        /// </summary>
        /// <returns>OracleConnection</returns>
        public OracleConnection GetOracleConnection()
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
                using (OracleCommand cmd = CreateCommand(
                    "SELECT 1 FROM DUAL",
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null))
                {
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value;
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
                    System.Diagnostics.Debug.WriteLine(disconnectException.Message);
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
                throw new ArgumentException("Connection string is required.", nameof(p_strConnectionString));

            DBDisConnect();

            try
            {
                m_DBCon = new OracleConnection(p_strConnectionString);
                m_DBCon.Open();
                m_DBTrans = m_DBCon.BeginTransaction();

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
                string connectionString = BuildAgentConnectionString(
                    p_strUSER,
                    p_strPW,
                    p_strAlias);

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
                string connectionString = BuildAgentConnectionString(
                    p_strUSER,
                    p_strPW,
                    p_strAlias);

                DBConnect(connectionString);

                SetSuccess(ref p_strErrCode, ref p_strErrText);
                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }
        }


        /// <summary>
        /// DB 연결을 끊습니다.
        /// </summary>
        public void DBDisConnect()
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


        /// <summary>
        /// DB 연결을 끊습니다.
        /// </summary>
        /// <param name="p_strErrCode">ErrorCode(out)</param>
        /// <param name="p_strErrText">ErrorText(out)</param>
        /// <returns></returns>
        public bool DBDisConnect(ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                DBDisConnect();
                SetSuccess(ref p_strErrCode, ref p_strErrText);
                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
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
            m_DBTrans = m_DBCon.BeginTransaction();
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
                SetSuccess(ref p_strErrCode, ref p_strErrText);
                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
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
            m_DBTrans = m_DBCon.BeginTransaction();
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
                SetSuccess(ref p_strErrCode, ref p_strErrText);
                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }
        }
        #endregion

        #region OraAgent : public void GetErrorCode(Exception e, ref string p_strErrCode, ref string p_strErrText)
        /// <summary>
        /// GetErrorCode
        /// </summary>
        /// <param name="e">Exception</param>
        /// <param name="p_strErrCode">ErrorCode(out)</param>
        /// <param name="p_strErrText">ErrorText(out)</param>
        public void GetErrorCode(Exception e, ref string p_strErrCode, ref string p_strErrText)
        {
            OracleException oracleException = e as OracleException;

            if (oracleException != null)
            {
                p_strErrCode = "O" + Math.Abs(oracleException.Number).ToString("D5");
                p_strErrText = oracleException.Message;
                return;
            }

            p_strErrCode = "AC7901";
            p_strErrText = e == null ? string.Empty : e.Message;
        }

        /// <summary>
        /// IsDBNoConnErrCode
        /// </summary>
        /// <param name="p_strErrCode">ErrorCode</param>
        /// <returns></returns>
        public bool IsDBNoConnErrCode(string p_strErrCode)
        {
            if (p_strErrCode == OracleDBDef.ORAMID_NOCONN1 ||
                p_strErrCode == OracleDBDef.ORAMID_NOCONN2 ||
                p_strErrCode == OracleDBDef.ORAMID_NOCONN3 ||
                p_strErrCode == OracleDBDef.ORAMID_NOCONN4 ||
                p_strErrCode == OracleDBDef.ORAMID_NOCONN5 ||
                p_strErrCode == OracleDBDef.ORAMID_NOCONN6 ||
                p_strErrCode == OracleDBDef.ORAMID_NOCONN7) return true;
            return false;
        }

        #endregion

        #region OraAgent : public void MessageFormat(Exception e, string p_strTitle, string p_strAction, string p_strAdjust,

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
                    "Oracle database is not connected.");
            }
        }

        private void EnsureTransaction()
        {
            EnsureConnected();

            if (m_DBTrans == null)
            {
                throw new InvalidOperationException(
                    "Oracle transaction is not active.");
            }
        }

        private OracleCommand CreateCommand(
            string commandText,
            CommandType commandType,
            int commandTimeout,
            OracleParameter[] oraParameters)
        {
            EnsureConnected();

            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentException(
                    "CommandText is required.",
                    nameof(commandText));
            }

            OracleCommand cmd = m_DBCon.CreateCommand();

            try
            {
                cmd.BindByName = true;
                cmd.CommandText = commandText;
                cmd.CommandType = commandType;
                cmd.CommandTimeout = commandTimeout;

                if (m_DBTrans != null)
                {
                    cmd.Transaction = m_DBTrans;
                }

                if (oraParameters != null)
                {
                    foreach (OracleParameter param in oraParameters)
                    {
                        if (param == null)
                        {
                            throw new ArgumentException(
                                "Parameter array contains null.",
                                nameof(oraParameters));
                        }

                        AddParameter(cmd, param);
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

        private static string BuildAgentConnectionString(
            string user,
            string password,
            string alias)
        {
            return "Pooling=false;user id=" + (user ?? string.Empty) + ";" +
                "data source=" + (alias ?? string.Empty) + ";" +
                "password=" + (password ?? string.Empty);
        }

        private static void SetSuccess(
            ref string p_strErrCode,
            ref string p_strErrText)
        {
            p_strErrCode = OracleDBDef.ORAMID_GOOD;
            p_strErrText = string.Empty;
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
        public int ExecuteNonQuery(string commandText, OracleParameter[] oraParameters, CommandType commandType)
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
        public int ExecuteNonQuery(int commandTimeout, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            using (OracleCommand cmd = CreateCommand(
                commandText,
                commandType,
                commandTimeout,
                oraParameters))
            {
                return cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region OraAgent : public bool ExecuteNonQuery( string p_strSQL, ref string p_strErrCode, ref string p_strErrText)
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
                using (OracleCommand cmd = CreateCommand(
                    p_strSQL,
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null))
                {
                    m_nRows = cmd.ExecuteNonQuery();
                }

                if (m_nRows == 0)
                {
                    p_strErrCode = OracleDBDef.ORAMID_NOFOUND;
                    p_strErrText = string.Empty;
                    return false;
                }

                SetSuccess(ref p_strErrCode, ref p_strErrText);
                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
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

        #region OraAgent : public OracleDataReader ExecuteReader( string p_strSQL, ref string p_strErrCode, ref string p_strErrText)
        /// <summary>
        /// SQL문을 실행합니다.
        /// OracleDataReader 형태로 데이터 반환합니다.
        /// </summary>
        /// <param name="p_strSQL">SQL문</param>
        /// <param name="p_strErrCode">Error Code(out)</param>
        /// <param name="p_strErrText">Error Text(out)</param>
        /// <returns>OracleDataReader</returns>
        public OracleDataReader ExecuteReader(string p_strSQL, ref string p_strErrCode, ref string p_strErrText)
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

                m_DataReader = m_DBCmd.ExecuteReader();

                SetSuccess(ref p_strErrCode, ref p_strErrText);
                return m_DataReader;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);

                DisposeCurrentReader();
                DisposeCurrentCommand();

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

        #region OraAgent : ExecuteScalar int / long
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
                using (OracleCommand cmd = CreateCommand(
                    p_strSQL,
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null))
                {
                    object value = cmd.ExecuteScalar();

                    if (value == null || value == DBNull.Value)
                    {
                        p_nValue = 0;
                    }
                    else
                    {
                        int parsedValue;
                        p_nValue = int.TryParse(
                            value.ToString(),
                            out parsedValue)
                                ? parsedValue
                                : 0;
                    }
                }

                SetSuccess(ref p_strErrCode, ref p_strErrText);
                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }
        }

        /// <summary>
        /// SQL문을 실행하고 long 값을 반환합니다.
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
                using (OracleCommand cmd = CreateCommand(
                    p_strSQL,
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null))
                {
                    object value = cmd.ExecuteScalar();

                    if (value == null || value == DBNull.Value)
                    {
                        p_lgValue = 0L;
                    }
                    else
                    {
                        long parsedValue;
                        p_lgValue = long.TryParse(
                            value.ToString(),
                            out parsedValue)
                                ? parsedValue
                                : 0L;
                    }
                }

                SetSuccess(ref p_strErrCode, ref p_strErrText);
                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }
        }
        #endregion

        #region OraAgent : ExecuteScalar string
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
                using (OracleCommand cmd = CreateCommand(
                    p_strSQL,
                    CommandType.Text,
                    COMMAND_TIMEOUT,
                    null))
                {
                    object value = cmd.ExecuteScalar();

                    p_strValue =
                        value == null || value == DBNull.Value
                            ? string.Empty
                            : value.ToString();
                }

                SetSuccess(ref p_strErrCode, ref p_strErrText);
                return true;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }
        }
        #endregion

        #region ==== AddParameter ====
        private void AddParameter(OracleCommand cmd, OracleParameter param)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            if (param.Value == null ||
                (param.Value is string && ((string)param.Value).Length == 0))
            {
                param.Value = DBNull.Value;
            }

            cmd.Parameters.Add(param);
        }
        #endregion
    }
    #endregion

    #region[OracleDBDef]
    public class OracleDBDef
    {
        /// <summary>
        /// OracleDBDef 생성자 입니다.
        /// </summary>
        public OracleDBDef() { }

        //------------------------------------------------------------------------------
        // Oracle DB Error Code
        //------------------------------------------------------------------------------        
        #region DEFINE : Oracle DB Error Code
        /// <summary>
        /// DB error code : Normal
        /// </summary>
        public const string ORAMID_GOOD = "O00000";
        /// <summary>
        ///  DB error code : Disconnect
        /// </summary>
        public const string ORAMID_NOCONN = "O03114";
        /// <summary>
        /// DB error code : Disconnect1
        /// </summary>
        public const string ORAMID_NOCONN1 = "O01012";
        /// <summary>
        /// DB error code : Disconnect2
        /// </summary>
        public const string ORAMID_NOCONN2 = "O01089";
        /// <summary>
        /// DB error code : Disconnect3
        /// </summary>
        public const string ORAMID_NOCONN3 = "O03113";
        /// <summary>
        /// DB error code : Disconnect4
        /// </summary>
        public const string ORAMID_NOCONN4 = "O03114";
        /// <summary>
        /// DB error code : Disconnect5
        /// </summary>
        public const string ORAMID_NOCONN5 = "O12152";
        /// <summary>
        /// DB error code : Disconnect6
        /// </summary>
        public const string ORAMID_NOCONN6 = "O12560";
        /// <summary>
        /// DB error code : Disconnect7
        /// </summary>
        public const string ORAMID_NOCONN7 = "O12571";
        /// <summary>
        /// DB error code : No data found
        /// </summary>
        public const string ORAMID_NOFOUND = "O01403";
        /// <summary>
        /// DB error code : No data found(AQ)
        /// </summary>
        public const string ORAMID_QUENODATA = "O25228";
        /// <summary>
        /// DB error code : Unique constraint 
        /// </summary>
        public const string ORAMID_OVERLAP = "O00001";
        /// <summary>
        /// DB error code : Database lock 
        /// </summary>
        public const string ORAMID_LOCK = "O00054";

        //		public const string ORAMID_ETC		= "AC8199";	    // DB error code : etc...
        //		public const string USRMID_ETC		= "AC7901";	    // DB error code : etc...
        #endregion
    }
    #endregion
}
