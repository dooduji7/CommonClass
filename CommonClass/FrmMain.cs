using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBHandler;


namespace CommonClass
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            string m_strIP = string.Empty;
            string m_strID = string.Empty;
            string m_strPW = string.Empty;
            string m_strDB = string.Empty;

            MSSQLDbAccess.ConnectionString(m_strIP, m_strID, m_strPW,m_strDB);

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


    }
}
