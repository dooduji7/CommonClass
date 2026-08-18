using AsyncSocket;
using DBHandler;
using LogHandler;
using SerialHandler;
using SocketClient;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CommonClass
{
    public partial class FrmMain : Form
    {
        #region Fields

        private TabControl tabMain;

        // Common
        private TextBox txtCommonLog;

        // AsyncSocket
        private NumericUpDown nudAsyncPort;
        private TextBox txtAsyncSend;
        private TextBox txtAsyncLog;
        private AsyncSocketServer asyncServer;
        private AsyncSocketClient asyncClient;
        private AsyncSocketClient asyncServerPeer;

        // DBHandler
        private ComboBox cboDbType;
        private TextBox txtDbServer;
        private TextBox txtDbUser;
        private TextBox txtDbPassword;
        private TextBox txtDbName;
        private TextBox txtDbLog;

        // LogHandler
        private TextBox txtLogMessage;
        private TextBox txtLogPath;
        private TextBox txtLogResult;

        // SerialHandler
        private ComboBox cboSerialPort;
        private ComboBox cboSerialBaud;
        private CheckBox chkSerialStxEtx;
        private TextBox txtSerialSend;
        private TextBox txtSerialLog;
        private SerialComPort serialPort;

        // SocketClient
        private NumericUpDown nudSocketClientPort;
        private TextBox txtSocketClientSend;
        private TextBox txtSocketClientLog;
        private clsSocketClient socketClient;
        private TcpListener socketClientEchoListener;
        private Thread socketClientEchoThread;
        private volatile bool socketClientEchoStop;

        #endregion

        public FrmMain()
        {
            InitializeComponent();

            try
            {
                Log.Init(
                    Path.Combine(Application.StartupPath, "TestLog"),
                    "MAIN",
                    true,
                    30);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Log.Init Error : " + ex);
            }

            BuildTestUi();

            FormClosing += FrmMain_FormClosing;
        }

        #region UI Build

        private void BuildTestUi()
        {
            SuspendLayout();

            Controls.Clear();

            Text = "CommonClass Library Test";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 700);
            Size = new Size(1180, 780);

            tabMain = new TabControl
            {
                Dock = DockStyle.Fill
            };

            tabMain.TabPages.Add(BuildAsyncSocketTab());
            tabMain.TabPages.Add(BuildDbHandlerTab());
            tabMain.TabPages.Add(BuildLogHandlerTab());
            tabMain.TabPages.Add(BuildSerialHandlerTab());
            tabMain.TabPages.Add(BuildSocketClientTab());

            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 125,
                Padding = new Padding(8)
            };

            Label lblCommon = new Label
            {
                Text = "Common Result",
                Dock = DockStyle.Top,
                Height = 20
            };

            txtCommonLog = CreateLogTextBox();
            txtCommonLog.Dock = DockStyle.Fill;

            bottomPanel.Controls.Add(txtCommonLog);
            bottomPanel.Controls.Add(lblCommon);

            Controls.Add(tabMain);
            Controls.Add(bottomPanel);

            ResumeLayout(true);

            AppendCommon("Test UI initialized.");
        }

        private TabPage BuildAsyncSocketTab()
        {
            TabPage page = new TabPage("AsyncSocket");

            FlowLayoutPanel top = CreateTopPanel();

            nudAsyncPort = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 65535,
                Value = 19001,
                Width = 80
            };

            txtAsyncSend = new TextBox
            {
                Text = "ASYNC SOCKET TEST",
                Width = 250
            };

            top.Controls.Add(CreateLabel("Port"));
            top.Controls.Add(nudAsyncPort);
            top.Controls.Add(CreateButton("Server Start", BtnAsyncServerStart_Click));
            top.Controls.Add(CreateButton("Server Stop", BtnAsyncServerStop_Click));
            top.Controls.Add(CreateButton("Client Connect", BtnAsyncConnect_Click));
            top.Controls.Add(CreateButton("Client Close", BtnAsyncClose_Click));
            top.Controls.Add(CreateLabel("Send"));
            top.Controls.Add(txtAsyncSend);
            top.Controls.Add(CreateButton("Send", BtnAsyncSend_Click));

            txtAsyncLog = CreateLogTextBox();

            page.Controls.Add(txtAsyncLog);
            page.Controls.Add(top);

            return page;
        }

        private TabPage BuildDbHandlerTab()
        {
            TabPage page = new TabPage("DBHandler");

            FlowLayoutPanel top = CreateTopPanel();
            top.Height = 105;
            top.WrapContents = true;

            cboDbType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100
            };
            cboDbType.Items.Add("MSSQL");
            cboDbType.Items.Add("ORACLE");
            cboDbType.SelectedIndex = 0;

            txtDbServer = new TextBox { Width = 180 };
            txtDbUser = new TextBox { Width = 110 };
            txtDbPassword = new TextBox
            {
                Width = 110,
                UseSystemPasswordChar = true
            };
            txtDbName = new TextBox { Width = 120 };

            top.Controls.Add(CreateLabel("Type"));
            top.Controls.Add(cboDbType);
            top.Controls.Add(CreateLabel("Server/Alias"));
            top.Controls.Add(txtDbServer);
            top.Controls.Add(CreateLabel("User"));
            top.Controls.Add(txtDbUser);
            top.Controls.Add(CreateLabel("Password"));
            top.Controls.Add(txtDbPassword);
            top.Controls.Add(CreateLabel("DB(MSSQL)"));
            top.Controls.Add(txtDbName);
            top.Controls.Add(CreateButton("Connection Test", BtnDbConnectTest_Click));

            txtDbLog = CreateLogTextBox();

            page.Controls.Add(txtDbLog);
            page.Controls.Add(top);

            return page;
        }

        private TabPage BuildLogHandlerTab()
        {
            TabPage page = new TabPage("LogHandler");

            FlowLayoutPanel top = CreateTopPanel();

            txtLogMessage = new TextBox
            {
                Text = "CommonClass LogHandler Test",
                Width = 320
            };

            txtLogPath = new TextBox
            {
                ReadOnly = true,
                Width = 320,
                Text = Path.Combine(Application.StartupPath, "TestLog")
            };

            top.Controls.Add(CreateLabel("Message"));
            top.Controls.Add(txtLogMessage);
            top.Controls.Add(CreateButton("Write 1", BtnLogWrite_Click));
            top.Controls.Add(CreateButton("Burst 100", BtnLogBurst_Click));
            top.Controls.Add(CreateButton("Open Folder", BtnLogFolder_Click));
            top.Controls.Add(CreateLabel("Path"));
            top.Controls.Add(txtLogPath);

            txtLogResult = CreateLogTextBox();

            page.Controls.Add(txtLogResult);
            page.Controls.Add(top);

            return page;
        }

        private TabPage BuildSerialHandlerTab()
        {
            TabPage page = new TabPage("SerialHandler");

            FlowLayoutPanel top = CreateTopPanel();
            top.Height = 105;
            top.WrapContents = true;

            cboSerialPort = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100
            };

            cboSerialBaud = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 90
            };

            cboSerialBaud.Items.AddRange(
                new object[] { "9600", "19200", "38400", "57600", "115200" });
            cboSerialBaud.SelectedItem = "9600";

            chkSerialStxEtx = new CheckBox
            {
                Text = "STX/ETX",
                AutoSize = true,
                Padding = new Padding(5, 7, 5, 0)
            };

            txtSerialSend = new TextBox
            {
                Text = "SERIAL TEST",
                Width = 220
            };

            top.Controls.Add(CreateButton("Refresh Ports", BtnSerialRefresh_Click));
            top.Controls.Add(CreateLabel("Port"));
            top.Controls.Add(cboSerialPort);
            top.Controls.Add(CreateLabel("Baud"));
            top.Controls.Add(cboSerialBaud);
            top.Controls.Add(chkSerialStxEtx);
            top.Controls.Add(CreateButton("Open", BtnSerialOpen_Click));
            top.Controls.Add(CreateButton("Close", BtnSerialClose_Click));
            top.Controls.Add(CreateLabel("Send"));
            top.Controls.Add(txtSerialSend);
            top.Controls.Add(CreateButton("Send", BtnSerialSend_Click));

            txtSerialLog = CreateLogTextBox();

            page.Controls.Add(txtSerialLog);
            page.Controls.Add(top);

            RefreshSerialPorts();

            return page;
        }

        private TabPage BuildSocketClientTab()
        {
            TabPage page = new TabPage("SocketClient");

            FlowLayoutPanel top = CreateTopPanel();

            nudSocketClientPort = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 65535,
                Value = 19002,
                Width = 80
            };

            txtSocketClientSend = new TextBox
            {
                Text = "SOCKET CLIENT TEST",
                Width = 250
            };

            top.Controls.Add(CreateLabel("Local Echo Port"));
            top.Controls.Add(nudSocketClientPort);
            top.Controls.Add(CreateButton("Echo Start", BtnSocketEchoStart_Click));
            top.Controls.Add(CreateButton("Echo Stop", BtnSocketEchoStop_Click));
            top.Controls.Add(CreateButton("Connect", BtnSocketClientConnect_Click));
            top.Controls.Add(CreateButton("Disconnect", BtnSocketClientDisconnect_Click));
            top.Controls.Add(CreateLabel("Send"));
            top.Controls.Add(txtSocketClientSend);
            top.Controls.Add(CreateButton("Send + Receive", BtnSocketClientSend_Click));

            txtSocketClientLog = CreateLogTextBox();

            page.Controls.Add(txtSocketClientLog);
            page.Controls.Add(top);

            return page;
        }

        private static FlowLayoutPanel CreateTopPanel()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 70,
                AutoScroll = true,
                Padding = new Padding(8),
                WrapContents = false
            };
        }

        private static TextBox CreateLogTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 9F),
                WordWrap = false
            };
        }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Padding = new Padding(3, 8, 3, 0)
            };
        }

        private static Button CreateButton(string text, EventHandler handler)
        {
            Button button = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 28,
                Margin = new Padding(3)
            };

            button.Click += handler;
            return button;
        }

        #endregion

        #region AsyncSocket Test

        private void BtnAsyncServerStart_Click(object sender, EventArgs e)
        {
            StopAsyncSocketTest();

            int port = (int)nudAsyncPort.Value;

            asyncServer = new AsyncSocketServer(port);
            asyncServer.OnError += Async_OnError;
            asyncServer.OnAccept += AsyncServer_OnAccept;

            asyncServer.Listen();

            AppendAsync("Server Listen requested. Port=" + port);
        }

        private void AsyncServer_OnAccept(object sender, AsyncSocketAcceptEventArgs e)
        {
            AppendAsync("Server accepted client.");

            try
            {
                if (asyncServerPeer != null)
                {
                    try { asyncServerPeer.Close(); }
                    catch { }
                }

                asyncServerPeer = new AsyncSocketClient(200, e.Worker);
                asyncServerPeer.OnError += Async_OnError;
                asyncServerPeer.OnReceive += AsyncServerPeer_OnReceive;
                asyncServerPeer.OnClose += Async_OnClose;

                asyncServerPeer.Receive();

                AppendAsync("Server peer receive started.");
            }
            catch (Exception ex)
            {
                AppendAsync("Server peer create error: " + ex.Message);
            }
        }

        private void AsyncServerPeer_OnReceive(
            object sender,
            AsyncSocketReceiveEventArgs e)
        {
            byte[] data = CopyReceiveData(e);

            AppendAsync(
                "SERVER RX [" + e.ReceiveBytes + "] " +
                Encoding.UTF8.GetString(data));

            AsyncSocketClient peer = asyncServerPeer;

            if (peer != null)
            {
                bool result = peer.Send(data);
                AppendAsync("SERVER Echo Send requested = " + result);
            }
        }

        private void BtnAsyncServerStop_Click(object sender, EventArgs e)
        {
            StopAsyncSocketTest();
            AppendAsync("AsyncSocket test stopped.");
        }

        private void BtnAsyncConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (asyncClient != null)
                {
                    try { asyncClient.Close(); }
                    catch { }
                }

                asyncClient = new AsyncSocketClient(100);
                asyncClient.OnError += Async_OnError;
                asyncClient.OnConnet += AsyncClient_OnConnect;
                asyncClient.OnReceive += AsyncClient_OnReceive;
                asyncClient.OnSend += AsyncClient_OnSend;
                asyncClient.OnClose += Async_OnClose;

                bool result = asyncClient.Connect(
                    "127.0.0.1",
                    (int)nudAsyncPort.Value);

                AppendAsync("Client Connect requested = " + result);
            }
            catch (Exception ex)
            {
                AppendAsync("Client Connect exception: " + ex);
            }
        }

        private void AsyncClient_OnConnect(
            object sender,
            AsyncSocketConnectionEventArgs e)
        {
            AppendAsync("CLIENT Connected. ID=" + e.ID);
        }

        private void AsyncClient_OnReceive(
            object sender,
            AsyncSocketReceiveEventArgs e)
        {
            byte[] data = CopyReceiveData(e);

            AppendAsync(
                "CLIENT RX [" + e.ReceiveBytes + "] " +
                Encoding.UTF8.GetString(data));
        }

        private void AsyncClient_OnSend(
            object sender,
            AsyncSocketSendEventArgs e)
        {
            AppendAsync("CLIENT Send completed. Bytes=" + e.SendBytes);
        }

        private void Async_OnClose(
            object sender,
            AsyncSocketConnectionEventArgs e)
        {
            AppendAsync("Socket Closed. ID=" + e.ID);
        }

        private void Async_OnError(
            object sender,
            AsyncSocketErrorEventArgs e)
        {
            string message =
                e.AsyncSocketException == null
                    ? "(null exception)"
                    : e.AsyncSocketException.ToString();

            AppendAsync("ERROR ID=" + e.ID + " / " + message);
        }

        private void BtnAsyncSend_Click(object sender, EventArgs e)
        {
            if (asyncClient == null)
            {
                AppendAsync("Client is null.");
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(txtAsyncSend.Text);
            bool result = asyncClient.Send(data);

            AppendAsync(
                "CLIENT Send requested = " + result +
                " / Bytes=" + data.Length);
        }

        private void BtnAsyncClose_Click(object sender, EventArgs e)
        {
            if (asyncClient != null)
            {
                try
                {
                    asyncClient.Close();
                    AppendAsync("Client Close requested.");
                }
                catch (Exception ex)
                {
                    AppendAsync("Client Close exception: " + ex.Message);
                }
            }
        }

        private void StopAsyncSocketTest()
        {
            AsyncSocketClient client = asyncClient;
            AsyncSocketClient peer = asyncServerPeer;
            AsyncSocketServer server = asyncServer;

            asyncClient = null;
            asyncServerPeer = null;
            asyncServer = null;

            if (client != null)
            {
                try { client.Close(); }
                catch { }
            }

            if (peer != null)
            {
                try { peer.Close(); }
                catch { }
            }

            if (server != null)
            {
                try { server.Stop(); }
                catch { }
            }
        }

        private static byte[] CopyReceiveData(AsyncSocketReceiveEventArgs e)
        {
            if (e == null ||
                e.ReceiveData == null ||
                e.ReceiveBytes <= 0)
            {
                return new byte[0];
            }

            int length = Math.Min(e.ReceiveBytes, e.ReceiveData.Length);

            byte[] data = new byte[length];
            Buffer.BlockCopy(e.ReceiveData, 0, data, 0, length);

            return data;
        }

        #endregion

        #region DBHandler Test

        private void BtnDbConnectTest_Click(object sender, EventArgs e)
        {
            string type = Convert.ToString(cboDbType.SelectedItem);

            try
            {
                if (string.Equals(type, "ORACLE", StringComparison.OrdinalIgnoreCase))
                {
                    TestOracleConnection();
                }
                else
                {
                    TestMssqlConnection();
                }
            }
            catch (Exception ex)
            {
                AppendDb("DB TEST ERROR: " + ex);
            }
        }

        private void TestMssqlConnection()
        {
            string server = txtDbServer.Text.Trim();
            string user = txtDbUser.Text.Trim();
            string password = txtDbPassword.Text;
            string database = txtDbName.Text.Trim();

            MSSQLDbAccess.ConnectionString(
                server,
                user,
                password,
                database);

            DateTime start = DateTime.Now;

            DataTable dt =
                MSSQLDbAccess.GetDataTable(
                    "SELECT GETDATE() AS SERVER_TIME",
                    null,
                    CommandType.Text);

            double elapsed =
                (DateTime.Now - start).TotalMilliseconds;

            if (dt != null && dt.Rows.Count > 0)
            {
                AppendDb(
                    "MSSQL OK / SERVER_TIME=" +
                    Convert.ToString(dt.Rows[0][0]) +
                    " / " + elapsed.ToString("0") + " ms");
            }
            else
            {
                AppendDb("MSSQL Query completed but no rows.");
            }
        }

        private void TestOracleConnection()
        {
            string alias = txtDbServer.Text.Trim();
            string user = txtDbUser.Text.Trim();
            string password = txtDbPassword.Text;

            OracleDbAgent oracle = new OracleDbAgent();

            DateTime start = DateTime.Now;

            try
            {
                bool result =
                    oracle.DBConnect(
                        user,
                        password,
                        alias);

                double elapsed =
                    (DateTime.Now - start).TotalMilliseconds;

                AppendDb(
                    "ORACLE Connect=" + result +
                    " / State=" + oracle.DBConnectState() +
                    " / " + elapsed.ToString("0") + " ms");
            }
            catch (Exception ex)
            {
                double elapsed =
                    (DateTime.Now - start).TotalMilliseconds;

                AppendDb(
                    "ORACLE Connect ERROR" +
                    " / " + elapsed.ToString("0") + " ms" +
                    " / " + ex.Message);
            }
            finally
            {
                try
                {
                    oracle.DBDisConnect();
                }
                catch (Exception ex)
                {
                    AppendDb(
                        "ORACLE Disconnect ERROR: " +
                        ex.Message);
                }

                oracle.Dispose();
            }
        }

        #endregion

        #region LogHandler Test

        private void BtnLogWrite_Click(object sender, EventArgs e)
        {
            Log instance = Log.Instance();

            if (instance == null)
            {
                AppendLogResult("Log.Instance() == null");
                return;
            }

            string message = txtLogMessage.Text;

            instance.EnqueueLog(
                LogType.Normal.ToString(),
                MethodBase.GetCurrentMethod().Name,
                new StackFrame(0, true).GetFileLineNumber(),
                new string[] { message });

            AppendLogResult("Enqueue 1 completed: " + message);
        }

        private void BtnLogBurst_Click(object sender, EventArgs e)
        {
            Log instance = Log.Instance();

            if (instance == null)
            {
                AppendLogResult("Log.Instance() == null");
                return;
            }

            DateTime start = DateTime.Now;

            for (int i = 1; i <= 100; i++)
            {
                instance.EnqueueLog(
                    "BURST",
                    new string[]
                    {
                        "SEQ=" + i.ToString("000"),
                        txtLogMessage.Text
                    });
            }

            double elapsed =
                (DateTime.Now - start).TotalMilliseconds;

            AppendLogResult(
                "Burst 100 enqueue completed / " +
                elapsed.ToString("0.0") + " ms");
        }

        private void BtnLogFolder_Click(object sender, EventArgs e)
        {
            try
            {
                string path = txtLogPath.Text;

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                AppendLogResult("Open folder error: " + ex.Message);
            }
        }

        #endregion

        #region SerialHandler Test

        private void BtnSerialRefresh_Click(object sender, EventArgs e)
        {
            RefreshSerialPorts();
        }

        private void RefreshSerialPorts()
        {
            if (cboSerialPort == null)
                return;

            string selected = Convert.ToString(cboSerialPort.SelectedItem);

            cboSerialPort.Items.Clear();

            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports, StringComparer.OrdinalIgnoreCase);

            cboSerialPort.Items.AddRange(ports);

            if (!string.IsNullOrEmpty(selected) &&
                cboSerialPort.Items.Contains(selected))
            {
                cboSerialPort.SelectedItem = selected;
            }
            else if (cboSerialPort.Items.Count > 0)
            {
                cboSerialPort.SelectedIndex = 0;
            }

            AppendSerial(
                "Port refresh: " +
                (ports.Length == 0
                    ? "(none)"
                    : string.Join(", ", ports)));
        }

        private void BtnSerialOpen_Click(object sender, EventArgs e)
        {
            CloseSerial();

            string portName = Convert.ToString(cboSerialPort.SelectedItem);

            if (string.IsNullOrWhiteSpace(portName))
            {
                AppendSerial("COM Port를 선택하세요.");
                return;
            }

            int baud;

            if (!int.TryParse(
                Convert.ToString(cboSerialBaud.SelectedItem),
                out baud))
            {
                baud = 9600;
            }

            try
            {
                serialPort = new SerialComPort(true);
                serialPort.Name = portName;
                serialPort.BaudRate = baud;
                serialPort.DataBit = 8;
                serialPort.StopBit = 1;
                serialPort.Paritys = 0;
                serialPort.Flow = 0;
                serialPort.STXETX = chkSerialStxEtx.Checked;
                serialPort.AddEvent(Serial_DataRecv);

                bool result = serialPort.PortOpen();

                AppendSerial(
                    "Open=" + result +
                    " / Port=" + portName +
                    " / Baud=" + baud +
                    " / STXETX=" + chkSerialStxEtx.Checked +
                    " / Err=" + serialPort.ErrMsg);
            }
            catch (Exception ex)
            {
                AppendSerial("Serial Open exception: " + ex);
                CloseSerial();
            }
        }

        private void Serial_DataRecv(object sender, PortEventArgs args)
        {
            AppendSerial("RX: " + args.GetRecvData());
        }

        private void BtnSerialClose_Click(object sender, EventArgs e)
        {
            CloseSerial();
            AppendSerial("Serial closed.");
        }

        private void BtnSerialSend_Click(object sender, EventArgs e)
        {
            if (serialPort == null)
            {
                AppendSerial("Serial is not created.");
                return;
            }

            string data = txtSerialSend.Text;

            if (chkSerialStxEtx.Checked)
                data = ((char)0x02) + data + ((char)0x03);

            bool result = serialPort.DataSend(data);

            AppendSerial(
                "TX result=" + result +
                " / Data=" + EscapeControlCharacters(data) +
                " / Err=" + serialPort.ErrMsg);
        }

        private void CloseSerial()
        {
            SerialComPort port = serialPort;
            serialPort = null;

            if (port == null)
                return;

            try { port.ClearEvent(); }
            catch { }

            try { port.PortClose(); }
            catch { }
        }

        #endregion

        #region SocketClient Test

        private void BtnSocketEchoStart_Click(object sender, EventArgs e)
        {
            StopSocketClientEchoServer();

            int port = (int)nudSocketClientPort.Value;

            try
            {
                socketClientEchoStop = false;
                socketClientEchoListener =
                    new TcpListener(IPAddress.Loopback, port);

                socketClientEchoListener.Start();

                socketClientEchoThread =
                    new Thread(SocketClientEchoWorker);

                socketClientEchoThread.IsBackground = true;
                socketClientEchoThread.Name = "CommonClass.SocketClientEcho";
                socketClientEchoThread.Start();

                AppendSocketClient(
                    "Local Echo Server started. 127.0.0.1:" + port);
            }
            catch (Exception ex)
            {
                AppendSocketClient("Echo Start error: " + ex);
                StopSocketClientEchoServer();
            }
        }

        private void SocketClientEchoWorker()
        {
            while (!socketClientEchoStop)
            {
                TcpClient client = null;

                try
                {
                    TcpListener listener = socketClientEchoListener;

                    if (listener == null)
                        break;

                    client = listener.AcceptTcpClient();

                    using (client)
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] buffer = new byte[8192];

                        while (!socketClientEchoStop)
                        {
                            int read = stream.Read(
                                buffer,
                                0,
                                buffer.Length);

                            if (read <= 0)
                                break;

                            stream.Write(buffer, 0, read);
                            stream.Flush();
                        }
                    }
                }
                catch (SocketException)
                {
                    if (!socketClientEchoStop)
                        AppendSocketClient("Echo SocketException.");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!socketClientEchoStop)
                        AppendSocketClient("Echo error: " + ex.Message);
                }
            }

            AppendSocketClient("Local Echo Worker exited.");
        }

        private void BtnSocketEchoStop_Click(object sender, EventArgs e)
        {
            StopSocketClientEchoServer();
            AppendSocketClient("Local Echo Server stopped.");
        }

        private void BtnSocketClientConnect_Click(object sender, EventArgs e)
        {
            CloseSocketClient();

            socketClient =
                new clsSocketClient(
                    "127.0.0.1",
                    (int)nudSocketClientPort.Value);

            bool result = socketClient.SocketConnect();

            AppendSocketClient(
                "Connect=" + result +
                " / IsConnected=" + socketClient.IsConnected +
                " / Error=" + socketClient.ERROR_MESSAGE);
        }

        private void BtnSocketClientDisconnect_Click(object sender, EventArgs e)
        {
            if (socketClient != null)
            {
                try
                {
                    socketClient.SocketDisconnect();
                    AppendSocketClient("SocketDisconnect called.");
                }
                catch (Exception ex)
                {
                    AppendSocketClient(
                        "SocketDisconnect exception: " + ex.Message);
                }
            }
        }

        private void BtnSocketClientSend_Click(object sender, EventArgs e)
        {
            if (socketClient == null)
            {
                AppendSocketClient("SocketClient is null.");
                return;
            }

            string message = txtSocketClientSend.Text;
            byte[] expected = Encoding.ASCII.GetBytes(message);

            bool sendResult = socketClient.SendData(message);

            AppendSocketClient(
                "Send=" + sendResult +
                " / Error=" + socketClient.ERROR_MESSAGE);

            if (!sendResult)
                return;

            byte[] data = socketClient.ReceiveData(expected.Length);

            AppendSocketClient(
                "Receive State=" + socketClient.LastReceiveState +
                " / Length=" +
                (data == null ? "null" : data.Length.ToString()) +
                " / Error=" + socketClient.ERROR_MESSAGE +
                " / Data=" +
                (data == null
                    ? "(null)"
                    : Encoding.ASCII.GetString(data)));
        }

        private void CloseSocketClient()
        {
            clsSocketClient client = socketClient;
            socketClient = null;

            if (client == null)
                return;

            try { client.Dispose(); }
            catch { }
        }

        private void StopSocketClientEchoServer()
        {
            socketClientEchoStop = true;

            TcpListener listener = socketClientEchoListener;
            socketClientEchoListener = null;

            if (listener != null)
            {
                try { listener.Stop(); }
                catch { }
            }

            Thread thread = socketClientEchoThread;
            socketClientEchoThread = null;

            if (thread != null &&
                thread != Thread.CurrentThread &&
                thread.IsAlive)
            {
                try { thread.Join(1000); }
                catch { }
            }
        }

        #endregion

        #region Logging Helpers

        private void AppendAsync(string text)
        {
            AppendTextSafe(txtAsyncLog, "AsyncSocket", text);
        }

        private void AppendDb(string text)
        {
            AppendTextSafe(txtDbLog, "DBHandler", text);
        }

        private void AppendLogResult(string text)
        {
            AppendTextSafe(txtLogResult, "LogHandler", text);
        }

        private void AppendSerial(string text)
        {
            AppendTextSafe(txtSerialLog, "SerialHandler", text);
        }

        private void AppendSocketClient(string text)
        {
            AppendTextSafe(txtSocketClientLog, "SocketClient", text);
        }

        private void AppendCommon(string text)
        {
            AppendTextSafe(txtCommonLog, "COMMON", text);
        }

        private void AppendTextSafe(
            TextBox target,
            string category,
            string text)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(
                        new Action<TextBox, string, string>(
                            AppendTextSafe),
                        target,
                        category,
                        text);
                }
                catch
                {
                }

                return;
            }

            string line =
                DateTime.Now.ToString("HH:mm:ss.fff") +
                " [" + category + "] " +
                text +
                Environment.NewLine;

            if (target != null && !target.IsDisposed)
                target.AppendText(line);

            if (txtCommonLog != null &&
                !txtCommonLog.IsDisposed &&
                !ReferenceEquals(target, txtCommonLog))
            {
                txtCommonLog.AppendText(line);
            }
        }

        private static string EscapeControlCharacters(string value)
        {
            if (value == null)
                return string.Empty;

            return value
                .Replace(((char)0x02).ToString(), "<STX>")
                .Replace(((char)0x03).ToString(), "<ETX>")
                .Replace("\r", "<CR>")
                .Replace("\n", "<LF>");
        }

        #endregion

        #region Cleanup

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopAsyncSocketTest();
            CloseSerial();
            CloseSocketClient();
            StopSocketClientEchoServer();

            try
            {
                Log instance = Log.Instance();

                if (instance != null)
                    instance.Dispose();
            }
            catch
            {
            }
        }

        #endregion

        #region Existing Designer Handler Compatibility

        // 기존 FrmMain.Designer.cs가 이 이벤트 핸들러를 참조하고 있으므로
        // Designer 파일을 변경하지 않아도 컴파일되도록 유지한다.

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (tabMain != null)
                tabMain.SelectedIndex = 1;
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            if (tabMain != null)
                tabMain.SelectedIndex = 2;
        }

        private void btnSocketTest_Click(object sender, EventArgs e)
        {
            if (tabMain != null)
                tabMain.SelectedIndex = 4;
        }

        private void btnSocket2_Click(object sender, EventArgs e)
        {
            if (tabMain != null)
                tabMain.SelectedIndex = 4;
        }

        private void btnSockset3_Click(object sender, EventArgs e)
        {
            if (tabMain != null)
                tabMain.SelectedIndex = 4;
        }

        private void btnSockset4_Click(object sender, EventArgs e)
        {
            if (tabMain != null)
                tabMain.SelectedIndex = 4;
        }

        #endregion
    }
}