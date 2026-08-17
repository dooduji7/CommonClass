using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using Oracle.ManagedDataAccess.Client;

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
        /// OracleDbAccess 생성자 입니다.
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

        private static string m_strID = "";
        private static string m_strPW = "";
        private static string m_strIP = "";
        private static string m_strDB = "";

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

        #region[ExecuteProcedure]
        /// <summary>
        /// 오라클 DB 프로시져 실행
        /// </summary>
        /// <param name="strSpName">프로시져명</param>
        /// <param name="strID">파라미터 명 (VARCHAR형만 사용해야함)</param>
        /// <param name="strVAL">파라미터 값</param>
        public static void ExecuteProcedure(string strSpName, string strID, string strVAL)
        {
            try
            {
                string[] strParamID = null;
                string[] strParamVAL = null;

                if (strID.Trim().Length != 0)
                {
                    strParamID = strID.Split('@');
                    strParamVAL = strVAL.Split('@');

                    SqlParameter[] Params = new SqlParameter[strParamID.Length];


                    for (int i = 0; i < strParamID.Length; i++)
                    {
                        Params[i] = new SqlParameter(strParamID[i], strParamVAL[i]);
                        Params[i].Direction = ParameterDirection.Input;
                    }
                    MSSQLDbAccess.Execute(strSpName, Params, CommandType.StoredProcedure);
                }
                else
                {
                    MSSQLDbAccess.Execute(strSpName, null, CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                throw;
            }
        }
        public static void ExecuteProcedure(string strSpName, string strID, string strVAL, char split)
        {
            try
            {
                string[] strParamID = null;
                string[] strParamVAL = null;

                if (strID.Trim().Length != 0)
                {
                    strParamID = strID.Split(split);
                    strParamVAL = strVAL.Split(split);

                    SqlParameter[] Params = new SqlParameter[strParamID.Length];

                    for (int i = 0; i < strParamID.Length; i++)
                    {
                        Params[i] = new SqlParameter(strParamID[i], strParamVAL[i]);
                        Params[i].Direction = ParameterDirection.Input;
                    }
                    MSSQLDbAccess.Execute(strSpName, Params, CommandType.StoredProcedure);
                }
                else
                {
                    MSSQLDbAccess.Execute(strSpName, null, CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                throw;
            }
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
            // DataSet ds = null;
            string strError = string.Empty;
            int i = 0;
            try
            {
                string[] strParamID = null;
                string[] strParamVAL = null;

                if (strID.Trim().Length != 0)
                {
                    strParamID = strID.Split('@');
                    strParamVAL = strVAL.Split('@');

                    SqlParameter[] Params = new SqlParameter[strParamID.Length];

                    for (i = 0; i < strParamID.Length - 1; i++)
                    {
                        Params[i] = new SqlParameter(strParamID[i], strParamVAL[i]);
                        Params[i].Direction = ParameterDirection.Input;
                    }

                    Params[i] = new SqlParameter(strParamID[i], SqlDbType.VarChar, 500);
                    Params[i].Direction = ParameterDirection.Output;

                    //ds = MSSQLDbAccess.GetDataSet(strSpName, Params, CommandType.StoredProcedure);
                    MSSQLDbAccess.ExecuteScalar(strSpName, Params, CommandType.StoredProcedure);
                    strError = Params[i].Value.ToString().Trim();
                }
                else
                {
                    SqlParameter[] Params = new SqlParameter[1];
                    Params[0] = new SqlParameter(strParamID[i], SqlDbType.VarChar, 500);
                    Params[0].Direction = ParameterDirection.Output;
                    //return MSSQLDbAccess.GetDataSet(strSpName, Params, CommandType.StoredProcedure);
                    MSSQLDbAccess.ExecuteScalar(strSpName, Params, CommandType.StoredProcedure);
                    strError = Params[i].Value.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                throw;
            }

            return strError;
            //return ds;
        }

        public static string ExecuteProcedureResult(string strSpName, string strID, string strVAL, char split)
        {
            string strError = string.Empty;
            int i = 0;
            //DataSet ds = null;
            try
            {
                string[] strParamID = null;
                string[] strParamVAL = null;

                if (strID.Trim().Length != 0)
                {
                    strParamID = strID.Split(split);
                    strParamVAL = strVAL.Split(split);

                    SqlParameter[] Params = new SqlParameter[strParamID.Length];
                    for (i = 0; i < strParamID.Length - 1; i++)
                    {
                        Params[i] = new SqlParameter(strParamID[i], strParamVAL[i]);
                        Params[i].Direction = ParameterDirection.Input;
                    }

                    Params[i] = new SqlParameter(strParamID[i], OracleDbType.RefCursor);
                    Params[i].Direction = ParameterDirection.Output;

                    //return MSSQLDbAccess.GetDataSet(strSpName, Params, CommandType.StoredProcedure);
                    MSSQLDbAccess.ExecuteScalar(strSpName, Params, CommandType.StoredProcedure);
                    strError = Params[i].Value.ToString().Trim();
                }
                else
                {
                    SqlParameter[] Params = new SqlParameter[1];
                    Params[0] = new SqlParameter(strParamID[i], OracleDbType.RefCursor);
                    Params[0].Direction = ParameterDirection.Output;
                    // return MSSQLDbAccess.GetDataSet(strSpName, Params, CommandType.StoredProcedure);
                    MSSQLDbAccess.ExecuteScalar(strSpName, Params, CommandType.StoredProcedure);
                    strError = Params[i].Value.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                throw;
            }
            return strError;
            //return ds;
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
            int iReturn = 0;

            SqlConnection con = null;
            SqlCommand cmd = null;

            try
            {
                #region JSBANG
                string strData = commandText + " : ";
                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        strData += " , " + string.Format("{0}", param.Value);
                    }
                }
                System.Diagnostics.Debug.WriteLine(strData);
                #endregion


                cmd = new SqlCommand();
                cmd.CommandText = commandText;
                cmd.CommandType = commandType;
                cmd.CommandTimeout = commandTimeout;

                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        AddParameter(cmd, param);
                    }
                }

                if (m_SQLTraceMode.Equals("on")) SQLTrace(m_SQLTracePath, cmd, true);

                con = new SqlConnection(p_strConnectionString);
                con.Open();

                cmd.Connection = con;
                iReturn = cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (con != null)
                {
                    if (con.State == ConnectionState.Open)
                        con.Close();
                    con.Dispose();
                    con = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
            }

            return iReturn;
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
            SqlConnection con = null;
            SqlCommand cmd = null;
            object oReturn = null;

            try
            {
                #region JSBANG
                string strData = commandText + " : ";
                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        strData += " , " + string.Format("{0}", param.Value);
                    }
                }
                System.Diagnostics.Debug.WriteLine(strData);
                #endregion

                cmd = new SqlCommand();
                cmd.CommandText = commandText;
                cmd.CommandType = commandType;
                cmd.CommandTimeout = commandTimeout;

                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        AddParameter(cmd, param);
                    }
                }

                if (m_SQLTraceMode.Equals("on")) SQLTrace(m_SQLTracePath, cmd, true);

                con = new SqlConnection(p_strConnectionString);
                con.Open();

                cmd.Connection = con;
                oReturn = cmd.ExecuteScalar();

            }
            finally
            {
                if (con != null)
                {
                    if (con.State == ConnectionState.Open)
                        con.Close();
                    con.Dispose();
                    con = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
            }
            return oReturn;
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
            if (oraParameters == null) throw new System.Exception("");//에러 메시지 어떻게 가져올 것인가?

            #region JSBANG
            string strData = commandText + " : ";
            if (oraParameters != null)
            {
                foreach (SqlParameter param in oraParameters)
                {
                    strData += " , " + string.Format("{0}", param.Value);
                }
            }
            System.Diagnostics.Debug.WriteLine(strData);
            #endregion

            int iReturn = 0;

            if (paramValues.Length > 0)
            {
                SqlConnection con = null;
                SqlCommand cmd = null;

                int iColumnCount = paramValues.GetUpperBound(1) + 1;
                int iRowCount = paramValues.GetUpperBound(0) + 1;
                int iCol = 0;
                int iRow = 0;

                string[] paramVals = new string[iColumnCount];
                for (iCol = 0; iCol < iColumnCount; iCol++)
                    paramVals[iCol] = paramValues[iRow, iCol];

                try
                {
                    cmd = new SqlCommand();
                    cmd.CommandText = commandText;
                    cmd.CommandType = commandType;
                    cmd.CommandTimeout = commandTimeout;

                    foreach (SqlParameter param in oraParameters)
                    {
                        AddParameter(cmd, param);
                    }

                    if (cmd != null)
                    {
                        con = new SqlConnection(p_strConnectionString);
                        con.Open();

                        cmd.Connection = con;
                        cmd.Prepare();

                        for (iRow = 0; iRow < iRowCount; iRow++)
                        {
                            for (iCol = 0; iCol < iColumnCount; iCol++)
                            {
                                string strValue = paramValues[iRow, iCol];
                                if ((strValue == null) || (strValue.Length == 0)) cmd.Parameters[iCol].Value = System.DBNull.Value;
                                else cmd.Parameters[iCol].Value = strValue;
                            }
                            if (m_SQLTraceMode.Equals("on"))
                            {
                                if (iRow == (iRowCount - 1)) SQLTrace(m_SQLTracePath, cmd, true);
                                else SQLTrace(m_SQLTracePath, cmd, false);
                            }
                            iReturn += cmd.ExecuteNonQuery();
                        }
                    }
                }
                finally
                {
                    if (con != null)
                    {
                        if (con.State == ConnectionState.Open)
                            con.Close();
                        con.Dispose();
                        con = null;
                    }
                    if (cmd != null)
                    {
                        cmd.Dispose();
                        cmd = null;
                    }
                }
            }

            return iReturn;
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

            SqlConnection con = null;
            SqlCommand cmd = null;
            SqlDataReader DataReader = null;
            bool bConnStat = false;
            try
            {
                con = new SqlConnection(strConnectionString);
                con.Open();
                cmd = new SqlCommand();
                cmd.Connection = con;

                string sDate = "";
                //cmd = con.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT dbo.GET_DATE()";
                DataReader = cmd.ExecuteReader();
                while (DataReader.Read())
                {
                    sDate = DataReader[0].ToString();
                    bConnStat = true;
                    break;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                bConnStat = false;
            }
            finally
            {
                if (con != null)
                {
                    if (con.State == ConnectionState.Open)
                        con.Close();
                    con.Dispose();
                    con = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
                if (DataReader != null)
                {
                    DataReader.Dispose();
                    DataReader = null;
                }
            }


            return bConnStat;
        }

        public static DataTable GetDataTable(string p_strConnectionString, int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            SqlConnection con = null;
            SqlCommand cmd = null;
            SqlDataAdapter da = null;
#if PTC
            clsDataSet dsReturn = null;
#else
            DataTable dtReturn;
#endif



            try
            {
                #region JSBANG
                string strData = commandText + " : ";

                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        strData += " , " + string.Format("{0}", param.Value);
                    }
                }
                System.Diagnostics.Debug.WriteLine(strData);
                #endregion

                cmd = new SqlCommand();
                cmd.CommandText = commandText;
                cmd.CommandType = commandType;
                cmd.CommandTimeout = commandTimeout;
                //cmd.InitialLONGFetchSize = -1; 

                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        AddParameter(cmd, param);
                    }
                }

                if (m_SQLTraceMode.Equals("on")) SQLTrace(m_SQLTracePath, cmd, true);

                con = new SqlConnection(p_strConnectionString);
                con.Open();

                cmd.Connection = con;

#if PTC
                dsReturn = new clsDataSet();
#else
                dtReturn = new DataTable();
#endif
                da = new SqlDataAdapter(cmd);
                da.Fill(dtReturn);

#if PTC
                string strParam = "";

                foreach (OracleParameter param in da.SelectCommand.Parameters)
                {
                    if (param.OracleType != OracleType.Cursor)
                    {
                        strParam += param.Value.ToString() + " , ";
                    }
                }

                dsReturn.P_SELECT_COMMAND = da.SelectCommand.CommandText;
                dsReturn.P_PARAMETERS = strParam.Length > 0 ? strParam.Substring(0, strParam.Length - 2) : "";
#endif


            }
            finally
            {
                if (con != null)
                {
                    if (con.State == ConnectionState.Open)
                        con.Close();
                    con.Dispose();
                    con = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
                if (da != null)
                {
                    da.Dispose();
                    da = null;
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
            SqlConnection con = null;
            SqlCommand cmd = null;
            SqlDataAdapter da = null;
#if PTC
            clsDataSet dsReturn = null;
#else
            DataSet dsReturn = null;
#endif



            try
            {
                #region JSBANG
                string strData = commandText + " : ";

                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        strData += " , " + string.Format("{0}", param.Value);
                    }
                }
                System.Diagnostics.Debug.WriteLine(strData);
                #endregion

                cmd = new SqlCommand();
                cmd.CommandText = commandText;
                cmd.CommandType = commandType;
                cmd.CommandTimeout = commandTimeout;
                //cmd.InitialLONGFetchSize = -1; 

                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        AddParameter(cmd, param);
                    }
                }

                if (m_SQLTraceMode.Equals("on")) SQLTrace(m_SQLTracePath, cmd, true);

                con = new SqlConnection(p_strConnectionString);
                con.Open();

                cmd.Connection = con;

#if PTC
                dsReturn = new clsDataSet();
#else
                dsReturn = new DataSet();
#endif
                da = new SqlDataAdapter(cmd);
                da.Fill(dsReturn);

#if PTC
                string strParam = "";

                foreach (OracleParameter param in da.SelectCommand.Parameters)
                {
                    if (param.OracleType != OracleType.Cursor)
                    {
                        strParam += param.Value.ToString() + " , ";
                    }
                }

                dsReturn.P_SELECT_COMMAND = da.SelectCommand.CommandText;
                dsReturn.P_PARAMETERS = strParam.Length > 0 ? strParam.Substring(0, strParam.Length - 2) : "";
#endif


            }
            finally
            {
                if (con != null)
                {
                    if (con.State == ConnectionState.Open)
                        con.Close();
                    con.Dispose();
                    con = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
                if (da != null)
                {
                    da.Dispose();
                    da = null;
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
        /// OracleDataReader 형태로 데이터 반환
        /// 기본 연결문을 가진다.
        /// </summary>
        /// <param name="commandTimeout">명령타임아웃시간</param>
        /// <param name="commandText">명령문자열</param>
        /// <param name="oraParameters">명령매개변수</param>
        /// <param name="commandType">명령문 타입</param>
        /// <returns>OracleDataReader</returns>
        public static SqlDataReader GetSqlDataReader(int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return GetSqlDataReader(strConnectionString, commandTimeout, commandText, oraParameters, commandType);
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
        public static SqlDataReader GetSqlDataReader(string p_strConnectionString, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return GetSqlDataReader(p_strConnectionString, COMMAND_TIMEOUT, commandText, oraParameters, commandType);
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
        public static SqlDataReader GetSqlDataReader(string p_strConnectionString, int commandTimeout, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            SqlConnection con = null;
            SqlCommand cmd = null;
            SqlDataReader dr = null;


            try
            {
                #region JSBANG
                string strData = commandText + " : ";
                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        strData += " , " + string.Format("{0}", param.Value);
                    }
                }
                System.Diagnostics.Debug.WriteLine(strData);
                #endregion

                cmd = new SqlCommand();
                cmd.CommandText = commandText;
                cmd.CommandType = commandType;
                cmd.CommandTimeout = commandTimeout;

                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        AddParameter(cmd, param);
                    }
                }

                if (m_SQLTraceMode.Equals("on")) SQLTrace(m_SQLTracePath, cmd, true);

                con = new SqlConnection(p_strConnectionString);
                con.Open();

                cmd.Connection = con;
                dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

            }
            finally
            {
                if (con != null)
                {
                    con.Dispose();
                    con = null;
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                    cmd = null;
                }
            }

            return dr;
        }

        #endregion

        #region ==== SQLTrace ====

        private static void SQLTrace(string SQLTracePath, SqlCommand cmd, bool markEndLine)
        {
            System.Text.StringBuilder oBuilder = new System.Text.StringBuilder();

            oBuilder.Append(System.DateTime.Now.ToString());
            oBuilder.Append("\r\n");
            oBuilder.Append(cmd.CommandText);

            if (cmd.Parameters.Count == 0)
            {
                oBuilder.Append(cmd.CommandText);
            }
            else
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
            if ((param.Value == null) || ((param.Value.GetType().ToString().Equals("System.String")) && ((string)param.Value).Length == 0))
                param.Value = System.DBNull.Value;
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
                            strReturn = string.Concat("'", (string)parameterValue, "'");
                            break;
                        //case SqlDbType.BFile:
                        //    strReturn = "<OracleBFile>";
                        //    break;
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
                        //						case OracleType.Cursor:
                        //							strReturn = "<OracleCursor>";
                        //							break;
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
        public static bool BeginTransaction()
        {
            m_SqlConnection = new SqlConnection(strConnectionString);
            if (m_SqlTransaction != null)
            {
                return false;
            }
            try
            {

                if (m_SqlConnection.State == ConnectionState.Closed)
                {
                    try
                    {
                        m_SqlConnection.Open();
                    }
                    catch (Exception exception)
                    {
                        throw new Exception(exception.Message);
                    }
                }

                m_SqlTransaction = m_SqlConnection.BeginTransaction();
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            return true;
        }


        public static bool Commit()
        {
            if (m_SqlTransaction == null)
            {
                return false;
            }
            try
            {
                m_SqlTransaction.Commit();
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            finally
            {
                m_SqlTransaction.Dispose();
                m_SqlTransaction = null;
                Dispose();
            }
            return true;
        }

        public static bool ExecuteNonQuery(string query)
        {
            int num;
            SqlCommand command = m_SqlConnection.CreateCommand();
            if (m_SqlTransaction != null)
            {
                command.Transaction = m_SqlTransaction;
            }
            command.CommandType = CommandType.Text;
            command.CommandText = query;
            try
            {
                num = command.ExecuteNonQuery();
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            finally
            {
                command.Dispose();
                command = null;
            }
            return (num > 0);
        }

        public static bool Rollback()
        {
            if (m_SqlTransaction == null)
            {
                return false;
            }
            try
            {
                m_SqlTransaction.Rollback();
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
            finally
            {
                m_SqlTransaction.Dispose();
                m_SqlTransaction = null;
                Dispose();
            }
            return true;
        }

        public static void DisConnect()
        {
            try
            {
                if (m_SqlTransaction != null)
                {
                    m_SqlTransaction.Dispose();
                    m_SqlTransaction = null;
                }
                if ((m_SqlConnection != null) && (m_SqlConnection.State == ConnectionState.Open))
                {
                    m_SqlConnection.Close();
                    m_SqlConnection = null;
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }


        public static void Dispose()
        {
            try
            {
                DisConnect();
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }



        #endregion
    }

    #endregion

    #region[MSSQLDbAgent]
    /// <summary>
    /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    public class MSSQLDbAgent
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
        protected string m_strOraErr = "ORA";


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
            bool bConnStat = false;
            if (m_DBCon == null || m_DBCon.State.ToString() == "Closed") return false;

            try
            {



                string sDate = "";
                m_DBCmd = m_DBCon.CreateCommand();
                m_DBCmd.CommandType = CommandType.Text;
                m_DBCmd.CommandText = "SELECT TO_CHAR(SYSDATE, 'YYYYMMDD') FROM DUAL";
                m_DataReader = m_DBCmd.ExecuteReader();
                while (m_DataReader.Read())
                {
                    sDate = m_DataReader[0].ToString();
                    bConnStat = true;
                    break;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                bConnStat = false;
            }
            finally
            {
                m_DBCmd.Dispose();
                if (m_DataReader != null)
                    m_DataReader.Dispose();
                if (!bConnStat)
                    DBDisConnect();
            }


            return bConnStat;
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
            try
            {
                m_DBCon = new SqlConnection(p_strConnectionString);
                m_DBCon.Open();

                //				m_DBCmd		= m_DBCon.CreateCommand();
                m_DBTrans = m_DBCon.BeginTransaction();
                //				m_DBCmd.Transaction = m_DBTrans;
            }
            catch (Exception)
            {
                throw;
            }

            return true;
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
                m_DBCon = new SqlConnection("Pooling=false;user id		= " + p_strUSER + ";" +
                    "data source	= " + p_strAlias + ";" +
                    "password		= " + p_strPW);
                m_DBCon.Open();

                //				m_DBCmd		= m_DBCon.CreateCommand();
                m_DBTrans = m_DBCon.BeginTransaction();
                //				m_DBCmd.Transaction = m_DBTrans;
            }
            catch
            {
                return false;
            }

            return true;
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
                m_DBCon = new SqlConnection("Pooling=false;user id		= " + p_strUSER + ";" +
                    "data source	= " + p_strAlias + ";" +
                    "password		= " + p_strPW);
                m_DBCon.Open();

                //				m_DBCmd		= m_DBCon.CreateCommand();
                m_DBTrans = m_DBCon.BeginTransaction();
                //				m_DBCmd.Transaction = m_DBTrans;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }

            return true;
        }


        /// <summary>
        /// DB 연결을 끊습니다.
        /// </summary>
        public void DBDisConnect()
        {
            try
            {
                m_DBTrans.Dispose();
                //				if ( m_DBCon.State == ConnectionState.Open )	
                m_DBCon.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                throw;

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
                m_DBTrans.Dispose();
                if (m_DBCon.State == ConnectionState.Open) m_DBCon.Close();
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }

            return true;
        }

        #endregion

        #region Commit / RollBack
        /// <summary>
        /// Commit
        /// </summary>
        public void Commit()
        {
            try
            {
                m_DBTrans.Commit();
                m_DBTrans = m_DBCon.BeginTransaction();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                throw;
            }
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
                m_DBTrans.Commit();
                m_DBTrans = m_DBCon.BeginTransaction();
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }

            return true;
        }


        /// <summary>
        /// RollBack
        /// </summary>
        public void RollBack()
        {
            try
            {
                m_DBTrans.Rollback();
                m_DBTrans = m_DBCon.BeginTransaction();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                throw;
            }
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
                m_DBTrans.Rollback();
                m_DBTrans = m_DBCon.BeginTransaction();
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }

            return true;
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
            if (e.Message.Substring(0, 3) == m_strOraErr)
            {
                p_strErrCode = "O" + e.Message.Substring(4, 5);
                p_strErrText = e.Message.Substring(10);
            }
            else
            {
                p_strErrCode = "AC7901";
                p_strErrText = e.Message;
            }
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
            int iReturn = 0;

            //OracleConnection con = null;
            //OracleCommand cmd = null;

            try
            {

                m_DBCmd = m_DBCon.CreateCommand();
                m_DBCmd.CommandText = commandText;
                m_DBCmd.CommandType = commandType;
                m_DBCmd.CommandTimeout = commandTimeout;
                m_DBCmd.Transaction = m_DBTrans;

                if (oraParameters != null)
                {
                    foreach (SqlParameter param in oraParameters)
                    {
                        AddParameter(m_DBCmd, param);
                    }
                }


                iReturn = m_DBCmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                if (m_DBCmd != null) m_DBCmd.Dispose();
            }

            return iReturn;
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
                m_DBCmd = m_DBCon.CreateCommand();
                m_DBCmd.CommandType = CommandType.Text;
                m_DBCmd.CommandText = p_strSQL;
                m_nRows = m_DBCmd.ExecuteNonQuery();

                if (m_nRows == 0)
                {
                    //p_strErrCode = OracleDBDef.ORAMID_NOFOUND;

                    return false;
                }
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }
            finally
            {
                m_DBCmd.Dispose();
            }

            return true;
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
        public SqlDataReader ExecuteReader(string p_strSQL, ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                m_DBCmd = m_DBCon.CreateCommand();
                m_DBCmd.CommandType = CommandType.Text;
                m_DBCmd.CommandText = p_strSQL;
                m_DataReader = m_DBCmd.ExecuteReader();

                return m_DataReader;
            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return null;
            }
            finally
            {
                m_DBCmd.Dispose();
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
        /// SQL문을 실행합니다.
        /// </summary>
        /// <param name="p_strSQL">SQL문</param>
        /// <param name="p_nValue">int Value(out)</param>
        /// <param name="p_strErrCode">Error Code(out)</param>
        /// <param name="p_strErrText">Error Text(out)</param>
        /// <returns></returns>
        public bool ExecuteScalar(string p_strSQL, ref int p_nValue, ref string p_strErrCode, ref string p_strErrText)
        {
            try
            {
                m_DBCmd = m_DBCon.CreateCommand();
                m_DBCmd.CommandType = CommandType.Text;
                m_DBCmd.CommandText = p_strSQL;

                //p_nValue = MES.FW.Common.Util.StringToInt(m_DBCmd.ExecuteScalar().ToString());
                string strValue = m_DBCmd.ExecuteScalar().ToString();

                if (strValue != null && strValue != "")
                {
                    try
                    {
                        p_nValue = int.Parse(strValue);
                    }
                    catch
                    {
                        p_nValue = 0;
                    }
                }

            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }
            finally
            {
                m_DBCmd.Dispose();
            }
            return true;
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
            int nVal = 0;
            if (!ExecuteScalar(p_strSQL, ref nVal, ref p_strErrCode, ref p_strErrText)) return false;
            p_lgValue = nVal;

            return true;
        }
        #endregion

        #region OraAgent : public bool ExecuteScalar(string p_strSQL, ref string p_strValue, ref string p_strErrCode, ref string p_strErrText)
        /// <summary>
        /// SQL문을 실행합니다.
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
                m_DBCmd = m_DBCon.CreateCommand();
                m_DBCmd.CommandType = CommandType.Text;
                m_DBCmd.CommandText = p_strSQL;

                if (m_DBCmd.ExecuteScalar() != null)
                {
                    p_strValue = m_DBCmd.ExecuteScalar().ToString();
                }
                else
                {
                    p_strValue = "";
                }

            }
            catch (Exception e)
            {
                GetErrorCode(e, ref p_strErrCode, ref p_strErrText);
                return false;
            }
            finally
            {
                m_DBCmd.Dispose();
            }

            return true;
        }
        #endregion


        #region ==== AddParameter ====
        private void AddParameter(SqlCommand cmd, SqlParameter param)
        {
            if ((param.Value == null) || ((param.Value.GetType().ToString().Equals("System.String")) && ((string)param.Value).Length == 0))
                param.Value = System.DBNull.Value;
            cmd.Parameters.Add(param);
        }
        #endregion
    }
    #endregion

    #region[MSSQLDBDef]
    public class MSSQLDBDef
    {
        /// <summary>
        /// OracleDBDef 생성자 입니다.
        /// </summary>
        public MSSQLDBDef() { }

        //------------------------------------------------------------------------------
        // MSSQL DB Error Code   -> 아직 정의되지 않음
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
