using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using PTC.MES.DBHANDLER;
//using PTC.MES.LOG;
using System.Data.OracleClient;
using System.Data;
using System.Data.SqlClient;


/*
 * NAME     : DictionaryHelper
 * PURPOSE  : Dictionary Helper Class
 * REVISIONS:
 *   Ver        Date        Author           Description
 *  ---------  ----------  ---------------  ------------------------------------
 *    1.0.0.3  2010-01-13   JKCHOI          기존 PTA
 *    1.0.1    2011-04-25   JKCHOI          ConnectionString관련 메소드 수정
 * 
 * 
 * 
 * 
 * INFO     : 사용자 연결제어의 경우 동시에 다른 디비에 연결 할 수 없다(다중스레딩지원 NO).
 *            이 경우 직접 스레드 별로 멤버생성 후 연결할 것.
 * */


namespace KFA.DBHANDLER
{
    public class CTMDBhandler
    {
        #region[member]
        private static CTMDBhandler _instance = null;
        //private clsDBHandler cls_dbHandler;
        private string m_strPrgName = string.Empty;

        //2009-12-01 jkchoi 
        private OracleDbAgent m_dbAgent = null;
        #endregion

        #region[Instance]
        public static CTMDBhandler Instance()
        {
            return _instance;
        }
        #endregion

        #region[Init]      
        public static CTMDBhandler Init(string PrgName, string SID, string Name, string PWD)
        {
            if (_instance != null)
            {
                _instance = null;
            }
            _instance = new CTMDBhandler(PrgName, SID, Name, PWD);
            
            return _instance;
        }

        public static CTMDBhandler Init(string PrgName, string p_strConnectionString)
        {
            if (_instance != null)
            {
                _instance = null;
            }
            _instance = new CTMDBhandler(PrgName, p_strConnectionString);

            return _instance;
        }
        #endregion

        #region[CTMDBhandler]
        public CTMDBhandler(string p_strPrgName, string p_strSID, string p_strName, string p_strPWD)
        {
            m_strPrgName = p_strPrgName;
            OracleDbAccess.ConnectionString(p_strSID, p_strName, p_strPWD);
            m_dbAgent = new OracleDbAgent();
        }

        public CTMDBhandler(string p_strPrgName, string p_strConnectionString)
        {
            m_strPrgName = p_strPrgName;
            OracleDbAccess.strConnectionString = p_strConnectionString;
            m_dbAgent = new OracleDbAgent();
        }
        #endregion

        #region[Execute]
        public int Execute(string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return this.Execute(commandText, oraParameters, commandType, false);
        }

        public int Execute(string commandText, OracleParameter[] oraParameters, CommandType commandType, bool Log)
        {
            return this.Execute(OracleDbAccess.strConnectionString, commandText, oraParameters, commandType, Log);
        }

        public int Execute(string connectionString, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return this.Execute(connectionString, commandText, oraParameters, commandType, false);
        }

        public int Execute(string connectionString, string commandText, OracleParameter[] oraParameters, CommandType commandType, bool Log)
        {
            int nRet = -1;
            if (Log)
                Send_LogWrite(commandText, oraParameters, Log);
            try
            {
                nRet = OracleDbAccess.Execute(connectionString, commandText, oraParameters, commandType);
            }
            catch (Exception ex)
            {
                DBException_LogWrite(commandText, ex.Message);
                throw ex;
            }
            if (Log)
                Recive_LogWrite(commandText, oraParameters, Log);

            return nRet;

        }

        #endregion

        #region[GetDataSet]
        public DataSet GetDataSet(string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return this.GetDataSet(commandText, oraParameters, commandType, false);
        }

        public DataSet GetDataSet(string commandText, OracleParameter[] oraParameters, CommandType commandType, bool Log)
        {
            return this.GetDataSet(OracleDbAccess.strConnectionString, commandText, oraParameters, commandType, Log);
        }

        public DataSet GetDataSet(string p_strConnectionString, string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return this.GetDataSet(p_strConnectionString, commandText, oraParameters, commandType, false);
        }

        public DataSet GetDataSet(string p_strConnectionString, string commandText, OracleParameter[] oraParameters, CommandType commandType, bool Log)
        {
            DataSet ds = null;
            if (Log)
                Send_LogWrite(commandText, oraParameters, Log);
            try
            {
                ds = OracleDbAccess.GetDataSet(p_strConnectionString, commandText, oraParameters, commandType);
            }
            catch (Exception ex)
            {
                DBException_LogWrite(commandText, ex.Message);
                throw ex;
            }
            if (Log)
                Recive_LogWrite(commandText, oraParameters, Log);
            return ds;

        }
        #endregion

        #region[Send_LogWrite]
        public void Send_LogWrite(string commandText, OracleParameter[] oraParameters, bool flag)
        {

            OracleParameter[] Params = new OracleParameter[3];
            string sp = string.Empty;
            string Param = string.Empty;
            try
            {
                if (oraParameters != null)
                {
                    for (int i = 0; i < oraParameters.Length; i++)
                    {
                        if (oraParameters[i].Direction == ParameterDirection.Input || oraParameters[i].Direction == ParameterDirection.InputOutput)
                        {
                            Param += oraParameters[i].ParameterName + ": " + oraParameters[i].Value.ToString() + "; ";
                        }
                    }
                }

                
                LogHandler.Instance().EnqueueLog(m_strPrgName, new string [] {commandText, Param});

                if (flag)
                {
                    sp = commandText;
                    if (sp.Length > 100)
                    {
                        Param = sp.Substring(100) + Param;
                        sp = sp.Substring(0, 100);
                    }
                    if (Param.Length > 3000)
                        Param = Param.Substring(0, 3000);

                    Params[0] = new OracleParameter("P_SP_NM", sp);
                    Params[1] = new OracleParameter("P_MSG", Param);
                    Params[2] = new OracleParameter("P_PROGRAM_NM", m_strPrgName);

                    Params[0].Direction = ParameterDirection.Input;
                    Params[1].Direction = ParameterDirection.Input;
                    Params[2].Direction = ParameterDirection.Input;

                    OracleDbAccess.Execute("SP_PROGRAM_LOG", Params, CommandType.StoredProcedure);
                }


            }
            catch (Exception ex)
            {
                LogHandler.Instance().EnqueueLog(m_strPrgName, new string [] {ex.Message});
                return;
            }

        }
        #endregion

        #region[Recive_LogWrite]
        public void Recive_LogWrite(string commandText, OracleParameter[] oraParameters, bool flag)
        {
            OracleParameter[] Params = new OracleParameter[3];
            string Param = string.Empty;
            string sp = string.Empty;
            try
            {
                if (oraParameters != null)
                {
                    for (int i = 0; i < oraParameters.Length; i++)
                    {
                        if (oraParameters[i].Direction == ParameterDirection.Output || oraParameters[i].Direction == ParameterDirection.InputOutput)
                        {
                            Param += oraParameters[i].ParameterName + ": " + oraParameters[i].Value.ToString() + "; ";
                        }
                    }
                }

                LogHandler.Instance().EnqueueLog(m_strPrgName, new string[] {commandText, Param});

                if (flag)
                {
                    sp = commandText;
                    if (sp.Length > 100)
                    {
                        Param = sp.Substring(100) + Param;
                        sp = sp.Substring(0, 100);
                    }

                    if (Param.Length > 3000)
                        Param = Param.Substring(0, 3000);

                    Params[0] = new OracleParameter("P_SP_NM", sp);
                    Params[1] = new OracleParameter("P_MSG", Param);
                    Params[2] = new OracleParameter("P_PROGRAM_NM", "POP");

                    Params[0].Direction = ParameterDirection.Input;
                    Params[1].Direction = ParameterDirection.Input;
                    Params[2].Direction = ParameterDirection.Input;

                    OracleDbAccess.Execute("SP_PROGRAM_LOG", Params, CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                LogHandler.Instance().EnqueueLog(m_strPrgName, new string[] {ex.Message});
                return;
            }

        }
        #endregion

        #region[DBException_LogWrite]
        public void DBException_LogWrite(string commandText, string p_ex)
        {
            OracleParameter[] Params = new OracleParameter[3];
            string Param = string.Empty;
            string sp = string.Empty;
            try
            {
                Param = p_ex;

                sp = commandText;
                if (sp.Length > 100)
                {
                    Param = sp.Substring(100) + Param;
                    sp = sp.Substring(0, 100);
                }
                if (Param.Length > 3000)
                    Param = Param.Substring(0, 3000);

                Params[0] = new OracleParameter("P_SP_NM", sp);
                Params[1] = new OracleParameter("P_MSG", Param);
                Params[2] = new OracleParameter("P_PROGRAM_NM", "POP");

                Params[0].Direction = ParameterDirection.Input;
                Params[1].Direction = ParameterDirection.Input;
                Params[2].Direction = ParameterDirection.Input;

                OracleDbAccess.Execute("SP_PROGRAM_LOG", Params, CommandType.StoredProcedure);

            }
            catch (Exception ex)
            {
                LogHandler.Instance().EnqueueLog(m_strPrgName, new string[] {ex.Message});
                return;
            }

        }
        #endregion


        #region[Connect 연결 사용자 제어]

        #region[Connect_Status]

        public bool Connect_State()
        {
            bool bRet = false;
            try
            {
                bRet = m_dbAgent.DBConnectState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("Connect", ex.Message);
                bRet = false;
            }

            return bRet;
        }
        #endregion

        #region[Connect]

        public bool Connect()
        {
            bool bRet = false;
            try
            {
                bRet = m_dbAgent.DBConnect(OracleDbAccess.strConnectionString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("Connect", ex.Message);
                bRet = false;
            }

            return bRet;
        }

        public bool Connect(string p_strConnectionString)
        {
            bool bRet = false;
            try
            {
                bRet = m_dbAgent.DBConnect(p_strConnectionString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("Connect", ex.Message);
                bRet = false;
            }

            return bRet;
        }

        #endregion

        #region[DisConnect]
        public void DisConnect()
        {
            
            try
            {
                m_dbAgent.DBDisConnect();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("DBDisConnect", ex.Message);
                
            }

            
        }
        #endregion

        #region[Commit]
        public void Commit()
        {
            try
            {
                m_dbAgent.Commit();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("Commit", ex.Message);
                throw ex;
            }
        }
        #endregion

        #region[RollBack]
        public void RollBack()
        {
            try
            {
                m_dbAgent.RollBack();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("RollBack", ex.Message);
                
            }
        }
        #endregion

        #region[ExecuteNonQuery]

        public int ExecuteNonQuery(string commandText, OracleParameter[] oraParameters, CommandType commandType)
        {
            return ExecuteNonQuery(commandText, oraParameters, commandType, false);
        }
        public int ExecuteNonQuery(string commandText, OracleParameter[] oraParameters, CommandType commandType,bool Log)
        {
            int nRet = -1;
            if (Log)
                Send_LogWrite(commandText, oraParameters, Log);
            try
            {
                nRet = m_dbAgent.ExecuteNonQuery(commandText, oraParameters, commandType);
            }
            catch (Exception ex)
            {
                DBException_LogWrite(commandText, ex.Message);
                throw ex;
            }
            if (Log)
                Recive_LogWrite(commandText, oraParameters, Log);

            return nRet;
        }   
        #endregion
        #endregion

    }

    public class CTMDBhandler_MS
    {
        #region[member]
        private static CTMDBhandler_MS _instance = null;
        //private clsDBHandler cls_dbHandler;
        private string m_strPrgName = string.Empty;

        //2009-12-01 jkchoi 
        private MSSQLDbAgent m_dbAgent = null;
        #endregion

        #region[Instance]
        public static CTMDBhandler_MS Instance()
        {
            return _instance;
        }
        #endregion

        #region[Init]
        public static CTMDBhandler_MS Init(string DBtype, string IP, string ID, string PWD, string DBname)
        {
            if (_instance != null)
            {
                _instance = null;
            }
            _instance = new CTMDBhandler_MS(DBtype, IP, ID, PWD, DBname);

            return _instance;
        }       
        #endregion

        #region[CTMDBhandler]
      

        public CTMDBhandler_MS(string DBtype, string IP, string ID, string PWD, string DBname)
        {
            m_strPrgName = DBtype;
            MSSQLDbAccess.ConnectionString(IP, ID, PWD, DBname);
            m_dbAgent = new MSSQLDbAgent();
        }
       
        #endregion

        #region[Execute]
        public int Execute(string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return this.Execute(commandText, oraParameters, commandType, false);
        }

        public int Execute(string commandText, SqlParameter[] oraParameters, CommandType commandType, bool Log)
        {
            return this.Execute(MSSQLDbAccess.strConnectionString, commandText, oraParameters, commandType, Log);
        }

        public int Execute(string connectionString, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return this.Execute(connectionString, commandText, oraParameters, commandType, false);
        }

        public int Execute(string connectionString, string commandText, SqlParameter[] oraParameters, CommandType commandType, bool Log)
        {
            int nRet = -1;
            if (Log)
                Send_LogWrite(commandText, oraParameters, Log);
            try
            {
                nRet = MSSQLDbAccess.Execute(connectionString, commandText, oraParameters, commandType);
            }
            catch (Exception ex)
            {
                DBException_LogWrite(commandText, ex.Message);
                throw ex;
            }
            if (Log)
                Recive_LogWrite(commandText, oraParameters, Log);

            return nRet;

        }

        #endregion

        #region[GetDataSet]
        public DataSet GetDataSet(string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return this.GetDataSet(commandText, oraParameters, commandType, false);
        }

        public DataSet GetDataSet(string commandText, SqlParameter[] oraParameters, CommandType commandType, bool Log)
        {
            return this.GetDataSet( MSSQLDbAccess.strConnectionString, commandText, oraParameters, commandType, Log);
        }

        public DataSet GetDataSet(string p_strConnectionString, string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return this.GetDataSet(p_strConnectionString, commandText, oraParameters, commandType, false);
        }

        public DataSet GetDataSet(string p_strConnectionString, string commandText, SqlParameter[] oraParameters, CommandType commandType, bool Log)
        {
            DataSet ds = null;
            if (Log)
                Send_LogWrite(commandText, oraParameters, Log);
            try
            {
                ds = MSSQLDbAccess.GetDataSet(p_strConnectionString, commandText, oraParameters, commandType);
            }
            catch (Exception ex)
            {
                DBException_LogWrite(commandText, ex.Message);
                throw ex;
            }
            if (Log)
                Recive_LogWrite(commandText, oraParameters, Log);
            return ds;

        }
        #endregion

        #region[Send_LogWrite]
        public void Send_LogWrite(string commandText, SqlParameter[] oraParameters, bool flag)
        {

            SqlParameter[] Params = new SqlParameter[3];
            string sp = string.Empty;
            string Param = string.Empty;
            try
            {
                if (oraParameters != null)
                {
                    for (int i = 0; i < oraParameters.Length; i++)
                    {
                        if (oraParameters[i].Direction == ParameterDirection.Input || oraParameters[i].Direction == ParameterDirection.InputOutput)
                        {
                            Param += oraParameters[i].ParameterName + ": " + oraParameters[i].Value.ToString() + "; ";
                        }
                    }
                }

                
                LogHandler.Instance().EnqueueLog(m_strPrgName, new string [] {commandText, Param});

                if (flag)
                {
                    sp = commandText;
                    if (sp.Length > 100)
                    {
                        Param = sp.Substring(100) + Param;
                        sp = sp.Substring(0, 100);
                    }
                    if (Param.Length > 3000)
                        Param = Param.Substring(0, 3000);

                    Params[0] = new SqlParameter("P_SP_NM", sp);
                    Params[1] = new SqlParameter("P_MSG", Param);
                    Params[2] = new SqlParameter("P_PROGRAM_NM", m_strPrgName);

                    Params[0].Direction = ParameterDirection.Input;
                    Params[1].Direction = ParameterDirection.Input;
                    Params[2].Direction = ParameterDirection.Input;

                    MSSQLDbAccess.Execute("SP_PROGRAM_LOG", Params, CommandType.StoredProcedure);
                }


            }
            catch (Exception ex)
            {
                LogHandler.Instance().EnqueueLog(m_strPrgName, new string [] {ex.Message});
                return;
            }

        }
        #endregion

        #region[Recive_LogWrite]
        public void Recive_LogWrite(string commandText, SqlParameter[] oraParameters, bool flag)
        {
            SqlParameter[] Params = new SqlParameter[3];
            string Param = string.Empty;
            string sp = string.Empty;
            try
            {
                if (oraParameters != null)
                {
                    for (int i = 0; i < oraParameters.Length; i++)
                    {
                        if (oraParameters[i].Direction == ParameterDirection.Output || oraParameters[i].Direction == ParameterDirection.InputOutput)
                        {
                            Param += oraParameters[i].ParameterName + ": " + oraParameters[i].Value.ToString() + "; ";
                        }
                    }
                }

                LogHandler.Instance().EnqueueLog(m_strPrgName, new string[] {commandText, Param});

                if (flag)
                {
                    sp = commandText;
                    if (sp.Length > 100)
                    {
                        Param = sp.Substring(100) + Param;
                        sp = sp.Substring(0, 100);
                    }

                    if (Param.Length > 3000)
                        Param = Param.Substring(0, 3000);

                    Params[0] = new SqlParameter("P_SP_NM", sp);
                    Params[1] = new SqlParameter("P_MSG", Param);
                    Params[2] = new SqlParameter("P_PROGRAM_NM", "POP");

                    Params[0].Direction = ParameterDirection.Input;
                    Params[1].Direction = ParameterDirection.Input;
                    Params[2].Direction = ParameterDirection.Input;

                    MSSQLDbAccess.Execute("SP_PROGRAM_LOG", Params, CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                LogHandler.Instance().EnqueueLog(m_strPrgName, new string[] {ex.Message});
                return;
            }

        }
        #endregion

        #region[DBException_LogWrite]
        public void DBException_LogWrite(string commandText, string p_ex)
        {
            SqlParameter[] Params = new SqlParameter[3];
            string Param = string.Empty;
            string sp = string.Empty;
            try
            {
                Param = p_ex;

                sp = commandText;
                if (sp.Length > 100)
                {
                    Param = sp.Substring(100) + Param;
                    sp = sp.Substring(0, 100);
                }
                if (Param.Length > 3000)
                    Param = Param.Substring(0, 3000);

                Params[0] = new SqlParameter("P_SP_NM", sp);
                Params[1] = new SqlParameter("P_MSG", Param);
                Params[2] = new SqlParameter("P_PROGRAM_NM", "POP");

                Params[0].Direction = ParameterDirection.Input;
                Params[1].Direction = ParameterDirection.Input;
                Params[2].Direction = ParameterDirection.Input;

                MSSQLDbAccess.Execute("SP_PROGRAM_LOG", Params, CommandType.StoredProcedure);

            }
            catch (Exception ex)
            {
                LogHandler.Instance().EnqueueLog(m_strPrgName, new string[] {ex.Message});
                return;
            }

        }
        #endregion


        #region[Connect 연결 사용자 제어]

        #region[Connect_Status]

        public bool Connect_State()
        {
            bool bRet = false;
            try
            {
                bRet = m_dbAgent.DBConnectState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("Connect", ex.Message);
                bRet = false;
            }

            return bRet;
        }
        #endregion

        #region[Connect]

        public bool Connect()
        {
            bool bRet = false;
            try
            {
                bRet = m_dbAgent.DBConnect(MSSQLDbAccess.strConnectionString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("Connect", ex.Message);
                bRet = false;
            }

            return bRet;
        }

        public bool Connect(string p_strConnectionString)
        {
            bool bRet = false;
            try
            {
                bRet = m_dbAgent.DBConnect(p_strConnectionString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("Connect", ex.Message);
                bRet = false;
            }

            return bRet;
        }

        #endregion

        #region[DisConnect]
        public void DisConnect()
        {
            
            try
            {
                m_dbAgent.DBDisConnect();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("DBDisConnect", ex.Message);
                
            }

            
        }
        #endregion

        #region[Commit]
        public void Commit()
        {
            try
            {
                m_dbAgent.Commit();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("Commit", ex.Message);
                throw ex;
            }
        }
        #endregion

        #region[RollBack]
        public void RollBack()
        {
            try
            {
                m_dbAgent.RollBack();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                DBException_LogWrite("RollBack", ex.Message);
                
            }
        }
        #endregion

        #region[ExecuteNonQuery]

        public int ExecuteNonQuery(string commandText, SqlParameter[] oraParameters, CommandType commandType)
        {
            return ExecuteNonQuery(commandText, oraParameters, commandType, false);
        }
        public int ExecuteNonQuery(string commandText, SqlParameter[] oraParameters, CommandType commandType, bool Log)
        {
            int nRet = -1;
            if (Log)
                Send_LogWrite(commandText, oraParameters, Log);
            try
            {
                nRet = m_dbAgent.ExecuteNonQuery(commandText, oraParameters, commandType);
            }
            catch (Exception ex)
            {
                DBException_LogWrite(commandText, ex.Message);
                throw ex;
            }
            if (Log)
                Recive_LogWrite(commandText, oraParameters, Log);

            return nRet;
        }   
        #endregion
        #endregion
    }



}
