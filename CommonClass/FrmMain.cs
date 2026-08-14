using DBHandler;
using LogHandler;
using SocketClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommonClass
{
    public partial class FrmMain : Form
    {

        public FrmMain()
        {
            InitializeComponent();
            Log.Init(Application.StartupPath + "\\TestLog", "MAIN", true, 30);
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            string m_strIP = string.Empty;
            string m_strID = string.Empty;
            string m_strPW = string.Empty;
            string m_strDB = string.Empty;

            MSSQLDbAccess.ConnectionString(m_strIP, m_strID, m_strPW,m_strDB);

            bool bConnect = DBConnectState();


        }


        /// <summary>
        /// 커넥션 체크
        /// </summary>
        /// <returns></returns>
        private bool DBConnectState()
        {
            bool bFlag = false;
            DataTable dt = new DataTable();
            try
            {
                dt = MSSQLDbAccess.GetDataTable("SELECT GETDATE() ", null, CommandType.Text);
                if (dt.Rows.Count > 0)
                {
                    bFlag = true;
                }
            }
            catch (Exception exception)
            {
            }
            return bFlag;
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            Log.Instance().EnqueueLog(LogType.Error.ToString(), MethodBase.GetCurrentMethod().Name,
                           (new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(), new string[] {"TEST Message" });
        }

        private void btnSocketTest_Click(object sender, EventArgs e)
        {
            clsSocketClient socket = new clsSocketClient("127.0.0.1", 9100);

            bool result = socket.SocketConnect();

            Debug.WriteLine(result);
            Debug.WriteLine(socket.IsConnected);
            Debug.WriteLine(socket.ERROR_MESSAGE);
        }

        private void btnSocket2_Click(object sender, EventArgs e)
        {
            clsSocketClient socket = new clsSocketClient("127.0.0.1", 9100);

            socket.Dispose();

            bool result = socket.SocketConnect();

            Debug.WriteLine(result);
            Debug.WriteLine(socket.IsConnected);
            Debug.WriteLine(socket.ERROR_MESSAGE);
        }

        private void btnSockset3_Click(object sender, EventArgs e)
        {
            clsSocketClient socket = new clsSocketClient("127.0.0.1", 9100);

            byte[] data = socket.ReceiveData(100);

            Console.WriteLine(data == null);
            Console.WriteLine(socket.LastReceiveState);
            Console.WriteLine(socket.ERROR_MESSAGE);
        }

        private void btnSockset4_Click(object sender, EventArgs e)
        {
            clsSocketClient socket = new clsSocketClient("127.0.0.1", 9100);

            byte[] data = socket.ReceiveData(0);

            Console.WriteLine(data == null);
            Console.WriteLine(socket.LastReceiveState);
            Console.WriteLine(socket.ERROR_MESSAGE);
        }
    }
}
