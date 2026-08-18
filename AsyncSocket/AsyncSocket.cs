using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AsyncSocket
{
    public class StateObject
    {
        // 기존 크기를 유지하여 기존 수신 동작과의 호환성을 보존한다.
        private const int BUFFER_SIZE = 327680;

        private Socket worker;
        private byte[] buffer;

        public StateObject(Socket worker)
        {
            this.worker = worker;
            this.buffer = new byte[BUFFER_SIZE];
        }

        public Socket Worker
        {
            get { return this.worker; }
            set { this.worker = value; }
        }

        public byte[] Buffer
        {
            get { return this.buffer; }
            set { this.buffer = value; }
        }

        public int BufferSize
        {
            get { return BUFFER_SIZE; }
        }
    }

    /// <summary>
    /// 비동기 소켓에서 발생한 에러 처리를 위한 이벤트 Argument Class
    /// </summary>
    public class AsyncSocketErrorEventArgs : EventArgs
    {
        private readonly Exception exception;
        private readonly int id = 0;

        public AsyncSocketErrorEventArgs(int id, Exception exception)
        {
            this.id = id;
            this.exception = exception;
        }

        public Exception AsyncSocketException
        {
            get { return this.exception; }
        }

        public int ID
        {
            get { return this.id; }
        }
    }

    /// <summary>
    /// 비동기 소켓의 연결 및 연결해제 이벤트 처리를 위한 Argument Class
    /// </summary>
    public class AsyncSocketConnectionEventArgs : EventArgs
    {
        private readonly int id = 0;

        public AsyncSocketConnectionEventArgs(int id)
        {
            this.id = id;
        }

        public int ID
        {
            get { return this.id; }
        }
    }

    /// <summary>
    /// 비동기 소켓의 데이터 전송 이벤트 처리를 위한 Argument Class
    /// </summary>
    public class AsyncSocketSendEventArgs : EventArgs
    {
        private readonly int id = 0;
        private readonly int sendBytes;

        public AsyncSocketSendEventArgs(int id, int sendBytes)
        {
            this.id = id;
            this.sendBytes = sendBytes;
        }

        public int SendBytes
        {
            get { return this.sendBytes; }
        }

        public int ID
        {
            get { return this.id; }
        }
    }

    /// <summary>
    /// 비동기 소켓의 데이터 수신 이벤트 처리를 위한 Argument Class
    /// </summary>
    public class AsyncSocketReceiveEventArgs : EventArgs
    {
        private readonly int id = 0;
        private readonly int receiveBytes;
        private readonly byte[] receiveData;

        public AsyncSocketReceiveEventArgs(int id, int receiveBytes, byte[] receiveData)
        {
            this.id = id;
            this.receiveBytes = receiveBytes;
            this.receiveData = receiveData;
        }

        public int ReceiveBytes
        {
            get { return this.receiveBytes; }
        }

        public byte[] ReceiveData
        {
            get { return this.receiveData; }
        }

        public int ID
        {
            get { return this.id; }
        }
    }

    /// <summary>
    /// 비동기 서버의 Accept 이벤트를 위한 Argument Class
    /// </summary>
    public class AsyncSocketAcceptEventArgs : EventArgs
    {
        private readonly Socket conn;

        public AsyncSocketAcceptEventArgs(Socket conn)
        {
            this.conn = conn;
        }

        public Socket Worker
        {
            get { return this.conn; }
        }
    }

    public delegate void AsyncSocketErrorEventHandler(object sender, AsyncSocketErrorEventArgs e);
    public delegate void AsyncSocketConnectEventHandler(object sender, AsyncSocketConnectionEventArgs e);
    public delegate void AsyncSocketCloseEventHandler(object sender, AsyncSocketConnectionEventArgs e);
    public delegate void AsyncSocketSendEventHandler(object sender, AsyncSocketSendEventArgs e);
    public delegate void AsyncSocketReceiveEventHandler(object sender, AsyncSocketReceiveEventArgs e);
    public delegate void AsyncSocketAcceptEventHandler(object sender, AsyncSocketAcceptEventArgs e);

    public class AsyncSocketClass
    {
        protected int id;

        public event AsyncSocketErrorEventHandler OnError;
        public event AsyncSocketConnectEventHandler OnConnet;
        public event AsyncSocketCloseEventHandler OnClose;
        public event AsyncSocketSendEventHandler OnSend;
        public event AsyncSocketReceiveEventHandler OnReceive;
        public event AsyncSocketAcceptEventHandler OnAccept;

        public AsyncSocketClass()
        {
            this.id = -1;
        }

        public AsyncSocketClass(int id)
        {
            this.id = id;
        }

        public int ID
        {
            get { return this.id; }
        }

        protected virtual void ErrorOccured(AsyncSocketErrorEventArgs e)
        {
            AsyncSocketErrorEventHandler handler = OnError;

            if (handler != null)
                handler(this, e);
        }

        protected virtual void Connected(AsyncSocketConnectionEventArgs e)
        {
            AsyncSocketConnectEventHandler handler = OnConnet;

            if (handler != null)
                handler(this, e);
        }

        protected virtual void Closed(AsyncSocketConnectionEventArgs e)
        {
            AsyncSocketCloseEventHandler handler = OnClose;

            if (handler != null)
                handler(this, e);
        }

        protected virtual void Sent(AsyncSocketSendEventArgs e)
        {
            AsyncSocketSendEventHandler handler = OnSend;

            if (handler != null)
                handler(this, e);
        }

        protected virtual void Received(AsyncSocketReceiveEventArgs e)
        {
            AsyncSocketReceiveEventHandler handler = OnReceive;

            if (handler != null)
                handler(this, e);
        }

        protected virtual void Accepted(AsyncSocketAcceptEventArgs e)
        {
            AsyncSocketAcceptEventHandler handler = OnAccept;

            if (handler != null)
                handler(this, e);
        }
    }

    /// <summary>
    /// 비동기 소켓 Client
    /// </summary>
    public class AsyncSocketClient : AsyncSocketClass, IDisposable
    {
        private sealed class SendState
        {
            public Socket Socket;
            public byte[] Buffer;
            public int Offset;
            public int TotalBytes;
        }

        private readonly object syncRoot = new object();

        // connection socket
        private Socket conn = null;
        private Socket pendingConnect = null;

        // Receive() 중복 호출에 의한 중복 BeginReceive 방지
        private int receiveStarted = 0;

        // Close / Remote Close가 겹쳐도 OnClose는 1회만 발생
        private int closeNotified = 0;
        private int closing = 0;
        private int disposed = 0;

        public AsyncSocketClient(int id)
        {
            this.id = id;
        }

        public AsyncSocketClient(int id, Socket conn)
        {
            this.id = id;
            this.conn = conn;
        }

        public Socket Connection
        {
            get { return this.conn; }
            set
            {
                lock (syncRoot)
                {
                    this.conn = value;
                    Interlocked.Exchange(ref receiveStarted, 0);
                    Interlocked.Exchange(ref closeNotified, 0);
                    Interlocked.Exchange(ref closing, 0);
                }
            }
        }

        /// <summary>
        /// 연결을 시도한다.
        /// BeginConnect 요청 성공 여부를 반환하며 실제 연결 성공은 OnConnet 이벤트로 통지한다.
        /// </summary>
        public bool Connect(string hostAddress, int port)
        {
            Socket client = null;

            try
            {
                ThrowIfDisposed();

                if (string.IsNullOrWhiteSpace(hostAddress))
                    throw new ArgumentException("hostAddress가 비어 있습니다.", nameof(hostAddress));

                if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                    throw new ArgumentOutOfRangeException(nameof(port));

                IPAddress[] ips = Dns.GetHostAddresses(hostAddress);

                if (ips == null || ips.Length == 0)
                    throw new SocketException((int)SocketError.HostNotFound);

                // 기존 구현이 IPv4 Socket을 생성했던 동작을 우선 보존한다.
                IPAddress selectedAddress = null;

                for (int i = 0; i < ips.Length; i++)
                {
                    if (ips[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        selectedAddress = ips[i];
                        break;
                    }
                }

                if (selectedAddress == null)
                    selectedAddress = ips[0];

                IPEndPoint remoteEP = new IPEndPoint(selectedAddress, port);
                client = new Socket(selectedAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                lock (syncRoot)
                {
                    if (pendingConnect != null)
                        throw new InvalidOperationException("이미 연결을 시도 중입니다.");

                    pendingConnect = client;
                    Interlocked.Exchange(ref closeNotified, 0);
                    Interlocked.Exchange(ref closing, 0);
                }

                client.BeginConnect(remoteEP, OnConnectCallback, client);
                return true;
            }
            catch (Exception e)
            {
                lock (syncRoot)
                {
                    if (ReferenceEquals(pendingConnect, client))
                        pendingConnect = null;
                }

                SafeClose(client);
                RaiseError(e);
                return false;
            }
        }

        /// <summary>
        /// 연결 요청 처리 콜백 함수
        /// </summary>
        private void OnConnectCallback(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;

            try
            {
                client.EndConnect(ar);

                if (Volatile.Read(ref disposed) != 0 || Volatile.Read(ref closing) != 0)
                {
                    SafeClose(client);
                    return;
                }

                Socket oldConnection = null;

                lock (syncRoot)
                {
                    if (ReferenceEquals(pendingConnect, client))
                        pendingConnect = null;

                    oldConnection = conn;
                    conn = client;
                }

                if (oldConnection != null && !ReferenceEquals(oldConnection, client))
                    SafeClose(oldConnection);

                Interlocked.Exchange(ref receiveStarted, 0);

                // 먼저 수신 대기를 설정한 후 연결 완료를 알린다.
                Receive();

                AsyncSocketConnectionEventArgs cev =
                    new AsyncSocketConnectionEventArgs(this.id);

                Connected(cev);
            }
            catch (Exception e)
            {
                lock (syncRoot)
                {
                    if (ReferenceEquals(pendingConnect, client))
                        pendingConnect = null;
                }

                SafeClose(client);

                if (Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    RaiseError(e);
                }
            }
        }

        /// <summary>
        /// 데이터 수신을 비동기적으로 처리한다.
        /// 외부에서 중복 호출해도 BeginReceive는 하나만 유지한다.
        /// </summary>
        public void Receive()
        {
            try
            {
                ThrowIfDisposed();

                Socket client = conn;

                if (client == null)
                    throw new InvalidOperationException("Socket이 연결되어 있지 않습니다.");

                if (Interlocked.CompareExchange(ref receiveStarted, 1, 0) != 0)
                    return;

                StateObject state = new StateObject(client);
                BeginReceive(state);
            }
            catch (Exception e)
            {
                Interlocked.Exchange(ref receiveStarted, 0);

                if (Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    RaiseError(e);
                }
            }
        }

        private void BeginReceive(StateObject state)
        {
            state.Worker.BeginReceive(
                state.Buffer,
                0,
                state.BufferSize,
                SocketFlags.None,
                OnReceiveCallBack,
                state);
        }

        /// <summary>
        /// 데이터 수신 처리 콜백 함수
        /// </summary>
        private void OnReceiveCallBack(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;

            try
            {
                int bytesRead = state.Worker.EndReceive(ar);

                if (bytesRead <= 0)
                {
                    // TCP에서 EndReceive == 0은 상대방의 정상 종료를 의미한다.
                    Interlocked.Exchange(ref receiveStarted, 0);
                    CompleteClose(state.Worker, true);
                    return;
                }

                AsyncSocketReceiveEventArgs rev =
                    new AsyncSocketReceiveEventArgs(this.id, bytesRead, state.Buffer);

                Received(rev);

                if (Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    // 새 320KB StateObject를 매번 만들지 않고 동일 버퍼를 재사용한다.
                    BeginReceive(state);
                }
                else
                {
                    Interlocked.Exchange(ref receiveStarted, 0);
                }
            }
            catch (ObjectDisposedException)
            {
                Interlocked.Exchange(ref receiveStarted, 0);

                if (Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    CompleteClose(state.Worker, true);
                }
            }
            catch (Exception e)
            {
                Interlocked.Exchange(ref receiveStarted, 0);

                if (Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    RaiseError(e);
                    CompleteClose(state.Worker, true);
                }
            }
        }

        /// <summary>
        /// 데이터 송신을 비동기적으로 처리한다.
        /// EndSend가 일부 Byte만 전송하는 경우 남은 데이터까지 계속 전송한다.
        /// </summary>
        public bool Send(byte[] buffer)
        {
            try
            {
                ThrowIfDisposed();

                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));

                Socket client = conn;

                if (client == null)
                    throw new InvalidOperationException("Socket이 연결되어 있지 않습니다.");

                if (Volatile.Read(ref closing) != 0)
                    throw new InvalidOperationException("Socket이 종료 중입니다.");

                if (buffer.Length == 0)
                {
                    Sent(new AsyncSocketSendEventArgs(this.id, 0));
                    return true;
                }

                SendState state = new SendState
                {
                    Socket = client,
                    Buffer = buffer,
                    Offset = 0,
                    TotalBytes = buffer.Length
                };

                BeginSend(state);
                return true;
            }
            catch (Exception e)
            {
                RaiseError(e);
                return false;
            }
        }

        private void BeginSend(SendState state)
        {
            state.Socket.BeginSend(
                state.Buffer,
                state.Offset,
                state.TotalBytes - state.Offset,
                SocketFlags.None,
                OnSendCallBack,
                state);
        }

        /// <summary>
        /// 데이터 송신 처리 콜백 함수
        /// </summary>
        private void OnSendCallBack(IAsyncResult ar)
        {
            SendState state = (SendState)ar.AsyncState;

            try
            {
                int bytesWritten = state.Socket.EndSend(ar);

                if (bytesWritten <= 0)
                    throw new SocketException((int)SocketError.ConnectionReset);

                state.Offset += bytesWritten;

                if (state.Offset < state.TotalBytes)
                {
                    BeginSend(state);
                    return;
                }

                AsyncSocketSendEventArgs sev =
                    new AsyncSocketSendEventArgs(this.id, state.Offset);

                Sent(sev);
            }
            catch (ObjectDisposedException)
            {
                // Close 중 발생하는 종료 예외는 정상적인 수명주기일 수 있으므로 무시한다.
                if (Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    RaiseError(new ObjectDisposedException("Socket"));
                }
            }
            catch (Exception e)
            {
                if (Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    RaiseError(e);
                }
            }
        }

        /// <summary>
        /// 소켓 연결을 비동기적으로 종료한다.
        /// </summary>
        public void Close()
        {
            if (Interlocked.CompareExchange(ref closing, 1, 0) != 0)
                return;

            Socket client;
            Socket connecting;

            lock (syncRoot)
            {
                client = conn;
                connecting = pendingConnect;
                pendingConnect = null;
            }

            // 연결 진행 중인 Socket도 함께 정리한다.
            if (connecting != null && !ReferenceEquals(connecting, client))
                SafeClose(connecting);

            if (client == null)
            {
                NotifyClosedOnce();
                return;
            }

            try
            {
                try
                {
                    client.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                    // 이미 원격 종료된 Socket일 수 있다.
                }
                catch (ObjectDisposedException)
                {
                    // 이미 종료된 Socket일 수 있다.
                }

                client.BeginDisconnect(false, OnCloseCallBack, client);
            }
            catch (Exception e)
            {
                SafeClose(client);

                if (!(e is ObjectDisposedException))
                    RaiseError(e);

                NotifyClosedOnce();
            }
        }

        /// <summary>
        /// 소켓 연결 종료를 처리하는 콜백 함수
        /// </summary>
        private void OnCloseCallBack(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;

            try
            {
                client.EndDisconnect(ar);
            }
            catch (ObjectDisposedException)
            {
                // 이미 닫힌 경우 정상 종료로 간주
            }
            catch (SocketException e)
            {
                // 종료 중 이미 연결이 사라진 경우가 많으므로 로그/이벤트만 남긴다.
                if (Volatile.Read(ref disposed) == 0)
                    RaiseError(e);
            }
            catch (Exception e)
            {
                if (Volatile.Read(ref disposed) == 0)
                    RaiseError(e);
            }
            finally
            {
                SafeClose(client);
                Interlocked.Exchange(ref receiveStarted, 0);

                lock (syncRoot)
                {
                    if (ReferenceEquals(conn, client))
                        conn = null;
                }

                NotifyClosedOnce();
            }
        }

        private void CompleteClose(Socket client, bool notify)
        {
            if (client == null)
                return;

            Interlocked.Exchange(ref closing, 1);
            SafeClose(client);

            lock (syncRoot)
            {
                if (ReferenceEquals(conn, client))
                    conn = null;

                if (ReferenceEquals(pendingConnect, client))
                    pendingConnect = null;
            }

            if (notify)
                NotifyClosedOnce();
        }

        private void NotifyClosedOnce()
        {
            if (Interlocked.Exchange(ref closeNotified, 1) != 0)
                return;

            AsyncSocketConnectionEventArgs cev =
                new AsyncSocketConnectionEventArgs(this.id);

            Closed(cev);
        }

        private void RaiseError(Exception e)
        {
            AsyncSocketErrorEventArgs eev =
                new AsyncSocketErrorEventArgs(this.id, e);

            ErrorOccured(eev);
        }

        private static void SafeClose(Socket socket)
        {
            if (socket == null)
                return;

            try
            {
                socket.Close();
            }
            catch
            {
                // 자원 정리 단계에서는 Close 예외를 외부로 전파하지 않는다.
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref disposed) != 0)
                throw new ObjectDisposedException(nameof(AsyncSocketClient));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;

            Interlocked.Exchange(ref closing, 1);

            Socket client;
            Socket connecting;

            lock (syncRoot)
            {
                client = conn;
                connecting = pendingConnect;
                conn = null;
                pendingConnect = null;
            }

            if (connecting != null && !ReferenceEquals(connecting, client))
                SafeClose(connecting);

            SafeClose(client);
            Interlocked.Exchange(ref receiveStarted, 0);

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 비동기 방식의 서버
    /// </summary>
    public class AsyncSocketServer : AsyncSocketClass, IDisposable
    {
        private const int backLog = 100;

        private readonly object syncRoot = new object();
        private int port;
        private Socket listener;
        private int stopping = 0;
        private int disposed = 0;

        public AsyncSocketServer(int port)
        {
            this.port = port;
        }

        public int Port
        {
            get { return this.port; }
        }

        public void Listen()
        {
            try
            {
                ThrowIfDisposed();

                if (this.port < IPEndPoint.MinPort || this.port > IPEndPoint.MaxPort)
                    throw new ArgumentOutOfRangeException(nameof(port));

                lock (syncRoot)
                {
                    if (listener != null)
                        throw new InvalidOperationException("Server가 이미 Listen 중입니다.");

                    Interlocked.Exchange(ref stopping, 0);

                    listener = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);

                    listener.Bind(new IPEndPoint(IPAddress.Any, this.port));
                    listener.Listen(backLog);
                }

                StartAccept();
            }
            catch (Exception e)
            {
                Socket target = null;

                lock (syncRoot)
                {
                    target = listener;
                    listener = null;
                }

                SafeClose(target);
                RaiseError(e);
            }
        }

        /// <summary>
        /// Client의 접속을 비동기적으로 대기한다.
        /// </summary>
        private void StartAccept()
        {
            try
            {
                if (Volatile.Read(ref stopping) != 0 ||
                    Volatile.Read(ref disposed) != 0)
                    return;

                Socket target = listener;

                if (target == null)
                    return;

                target.BeginAccept(OnListenCallBack, target);
            }
            catch (ObjectDisposedException)
            {
                if (Volatile.Read(ref stopping) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    RaiseError(new ObjectDisposedException("listener"));
                }
            }
            catch (Exception e)
            {
                if (Volatile.Read(ref stopping) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    RaiseError(e);
                }
            }
        }

        /// <summary>
        /// Client의 비동기 접속을 처리한다.
        /// </summary>
        private void OnListenCallBack(IAsyncResult ar)
        {
            Socket acceptListener = (Socket)ar.AsyncState;
            Socket worker = null;

            try
            {
                worker = acceptListener.EndAccept(ar);

                if (Volatile.Read(ref stopping) != 0 ||
                    Volatile.Read(ref disposed) != 0)
                {
                    SafeClose(worker);
                    return;
                }

                AsyncSocketAcceptEventArgs aev =
                    new AsyncSocketAcceptEventArgs(worker);

                Accepted(aev);

                // 다시 새로운 클라이언트의 접속을 기다린다.
                StartAccept();
            }
            catch (ObjectDisposedException)
            {
                // Stop()에 의한 listener.Close() 후 callback 진입은 정상 종료 흐름이다.
                if (Volatile.Read(ref stopping) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    RaiseError(new ObjectDisposedException("listener"));
                }
            }
            catch (Exception e)
            {
                if (worker != null)
                    SafeClose(worker);

                if (Volatile.Read(ref stopping) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    RaiseError(e);

                    // 예상하지 못한 Accept 오류 후에도 Listener가 살아 있으면 다시 Accept를 시도한다.
                    StartAccept();
                }
            }
        }

        public void Stop()
        {
            if (Interlocked.Exchange(ref stopping, 1) != 0)
                return;

            Socket target;

            lock (syncRoot)
            {
                target = listener;
                listener = null;
            }

            SafeClose(target);
        }

        private void RaiseError(Exception e)
        {
            AsyncSocketErrorEventArgs eev =
                new AsyncSocketErrorEventArgs(this.id, e);

            ErrorOccured(eev);
        }

        private static void SafeClose(Socket socket)
        {
            if (socket == null)
                return;

            try
            {
                socket.Close();
            }
            catch
            {
                // 종료 단계의 Close 예외는 무시한다.
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref disposed) != 0)
                throw new ObjectDisposedException(nameof(AsyncSocketServer));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;

            Stop();
            GC.SuppressFinalize(this);
        }
    }
}