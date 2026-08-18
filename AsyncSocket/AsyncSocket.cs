using System;
using System.Collections.Generic;
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
        private int generation;

        public StateObject(Socket worker)
            : this(worker, 0)
        {
        }

        internal StateObject(Socket worker, int generation)
        {
            this.worker = worker;
            this.buffer = new byte[BUFFER_SIZE];
            this.generation = generation;
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

        internal int Generation
        {
            get { return this.generation; }
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
            this.receiveData = receiveData ?? new byte[0];
        }

        public int ReceiveBytes
        {
            get { return this.receiveBytes; }
        }

        public byte[] ReceiveData
        {
            get { return this.receiveData; }
        }

        /// <summary>
        /// 수신 데이터를 별도 배열로 복사해서 반환한다.
        /// ReceiveData 자체도 실제 수신 길이 배열이지만,
        /// 호출부에서 독립 복사본이 필요한 경우 사용할 수 있다.
        /// </summary>
        public byte[] GetDataCopy()
        {
            byte[] copy = new byte[this.receiveBytes];

            if (this.receiveBytes > 0)
                Buffer.BlockCopy(this.receiveData, 0, copy, 0, this.receiveBytes);

            return copy;
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

            if (handler == null)
                return;

            foreach (AsyncSocketErrorEventHandler subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, e);
                }
                catch (Exception ex)
                {
                    // Error Handler 자체 예외를 다시 OnError로 올리면 재귀 오류가 될 수 있으므로 Debug만 남긴다.
                    Debug.WriteLine("[AsyncSocket] OnError Handler Error : " + ex);
                }
            }
        }

        protected virtual void Connected(AsyncSocketConnectionEventArgs e)
        {
            AsyncSocketConnectEventHandler handler = OnConnet;

            if (handler == null)
                return;

            foreach (AsyncSocketConnectEventHandler subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, e);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[AsyncSocket] OnConnet Handler Error : " + ex);
                }
            }
        }

        protected virtual void Closed(AsyncSocketConnectionEventArgs e)
        {
            AsyncSocketCloseEventHandler handler = OnClose;

            if (handler == null)
                return;

            foreach (AsyncSocketCloseEventHandler subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, e);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[AsyncSocket] OnClose Handler Error : " + ex);
                }
            }
        }

        protected virtual void Sent(AsyncSocketSendEventArgs e)
        {
            AsyncSocketSendEventHandler handler = OnSend;

            if (handler == null)
                return;

            foreach (AsyncSocketSendEventHandler subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, e);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[AsyncSocket] OnSend Handler Error : " + ex);
                }
            }
        }

        protected virtual void Received(AsyncSocketReceiveEventArgs e)
        {
            AsyncSocketReceiveEventHandler handler = OnReceive;

            if (handler == null)
                return;

            foreach (AsyncSocketReceiveEventHandler subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, e);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[AsyncSocket] OnReceive Handler Error : " + ex);
                }
            }
        }

        protected virtual void Accepted(AsyncSocketAcceptEventArgs e)
        {
            AsyncSocketAcceptEventHandler handler = OnAccept;

            if (handler == null)
                return;

            foreach (AsyncSocketAcceptEventHandler subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, e);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[AsyncSocket] OnAccept Handler Error : " + ex);
                }
            }
        }
    }

    /// <summary>
    /// 비동기 소켓 Client
    /// </summary>
    public class AsyncSocketClient : AsyncSocketClass, IDisposable
    {
        private sealed class ConnectState
        {
            public Socket Socket;
            public int Generation;
        }

        private sealed class SendState
        {
            public Socket Socket;
            public byte[] Buffer;
            public int Offset;
            public int TotalBytes;
            public int Generation;
        }

        private readonly object syncRoot = new object();
        private readonly object sendLock = new object();
        private readonly Queue<SendState> sendQueue = new Queue<SendState>();

        private Socket conn = null;
        private Socket pendingConnect = null;

        private int connectionGeneration = 0;
        private int receiveStarted = 0;
        private bool sendInProgress = false;

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

            if (conn != null)
                this.connectionGeneration = 1;
        }

        public Socket Connection
        {
            get
            {
                lock (syncRoot)
                {
                    return this.conn;
                }
            }
            set
            {
                Socket oldSocket;

                lock (syncRoot)
                {
                    oldSocket = this.conn;
                    this.conn = value;
                    this.pendingConnect = null;
                    Interlocked.Increment(ref connectionGeneration);
                    Interlocked.Exchange(ref receiveStarted, 0);
                    Interlocked.Exchange(ref closeNotified, 0);
                    Interlocked.Exchange(ref closing, 0);
                }

                ClearSendQueue();

                if (oldSocket != null && !ReferenceEquals(oldSocket, value))
                    SafeClose(oldSocket);
            }
        }

        /// <summary>
        /// 연결을 시도한다.
        /// BeginConnect 요청 성공 여부를 반환하며 실제 연결 성공은 OnConnet 이벤트로 통지한다.
        /// </summary>
        public bool Connect(string hostAddress, int port)
        {
            Socket client = null;
            int generation = 0;

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

                    if (conn != null)
                        throw new InvalidOperationException("이미 연결된 Socket이 존재합니다.");

                    generation = Interlocked.Increment(ref connectionGeneration);
                    pendingConnect = client;

                    Interlocked.Exchange(ref closeNotified, 0);
                    Interlocked.Exchange(ref closing, 0);
                    Interlocked.Exchange(ref receiveStarted, 0);
                }

                ClearSendQueue();

                ConnectState state = new ConnectState
                {
                    Socket = client,
                    Generation = generation
                };

                client.BeginConnect(remoteEP, OnConnectCallback, state);
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

        private void OnConnectCallback(IAsyncResult ar)
        {
            ConnectState state = (ConnectState)ar.AsyncState;
            Socket client = state.Socket;

            try
            {
                client.EndConnect(ar);

                if (!IsCurrentGeneration(state.Generation) ||
                    Volatile.Read(ref disposed) != 0 ||
                    Volatile.Read(ref closing) != 0)
                {
                    SafeClose(client);
                    return;
                }

                lock (syncRoot)
                {
                    if (!ReferenceEquals(pendingConnect, client) ||
                        state.Generation != Volatile.Read(ref connectionGeneration))
                    {
                        SafeClose(client);
                        return;
                    }

                    pendingConnect = null;
                    conn = client;
                }

                Interlocked.Exchange(ref receiveStarted, 0);

                Receive();

                AsyncSocketConnectionEventArgs cev =
                    new AsyncSocketConnectionEventArgs(this.id);

                Connected(cev);
            }
            catch (Exception e)
            {
                bool isCurrent;

                lock (syncRoot)
                {
                    isCurrent =
                        ReferenceEquals(pendingConnect, client) &&
                        state.Generation == Volatile.Read(ref connectionGeneration);

                    if (isCurrent)
                        pendingConnect = null;
                }

                SafeClose(client);

                if (isCurrent &&
                    Volatile.Read(ref closing) == 0 &&
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

                Socket client;
                int generation;

                lock (syncRoot)
                {
                    client = conn;
                    generation = Volatile.Read(ref connectionGeneration);
                }

                if (client == null)
                    throw new InvalidOperationException("Socket이 연결되어 있지 않습니다.");

                if (Volatile.Read(ref closing) != 0)
                    return;

                if (Interlocked.CompareExchange(ref receiveStarted, 1, 0) != 0)
                    return;

                StateObject state = new StateObject(client, generation);
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
            if (!IsCurrentConnection(state.Worker, state.Generation))
            {
                Interlocked.Exchange(ref receiveStarted, 0);
                return;
            }

            state.Worker.BeginReceive(
                state.Buffer,
                0,
                state.BufferSize,
                SocketFlags.None,
                OnReceiveCallBack,
                state);
        }

        private void OnReceiveCallBack(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;

            try
            {
                int bytesRead = state.Worker.EndReceive(ar);

                if (!IsCurrentConnection(state.Worker, state.Generation))
                {
                    SafeClose(state.Worker);
                    return;
                }

                if (bytesRead <= 0)
                {
                    Interlocked.Exchange(ref receiveStarted, 0);
                    CompleteClose(state.Worker, state.Generation, true);
                    return;
                }

                // 내부 320KB Buffer는 재사용하되, 이벤트에는 실제 수신 길이만큼만 복사한다.
                // 이로써 다음 BeginReceive가 같은 Buffer를 덮어써도 EventArgs 데이터는 변하지 않는다.
                byte[] receivedData = new byte[bytesRead];
                Buffer.BlockCopy(state.Buffer, 0, receivedData, 0, bytesRead);

                AsyncSocketReceiveEventArgs rev =
                    new AsyncSocketReceiveEventArgs(this.id, bytesRead, receivedData);

                Received(rev);

                if (Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0 &&
                    IsCurrentConnection(state.Worker, state.Generation))
                {
                    BeginReceive(state);
                }
                else
                {
                    Interlocked.Exchange(ref receiveStarted, 0);
                }
            }
            catch (ObjectDisposedException)
            {
                if (IsCurrentConnection(state.Worker, state.Generation))
                    Interlocked.Exchange(ref receiveStarted, 0);
            }
            catch (Exception e)
            {
                if (IsCurrentConnection(state.Worker, state.Generation))
                {
                    Interlocked.Exchange(ref receiveStarted, 0);

                    if (Volatile.Read(ref closing) == 0 &&
                        Volatile.Read(ref disposed) == 0)
                    {
                        RaiseError(e);
                        CompleteClose(state.Worker, state.Generation, true);
                    }
                }
                else
                {
                    SafeClose(state.Worker);
                }
            }
        }

        /// <summary>
        /// 데이터 송신 요청을 Queue에 넣고 하나씩 직렬 처리한다.
        /// </summary>
        public bool Send(byte[] buffer)
        {
            try
            {
                ThrowIfDisposed();

                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));

                Socket client;
                int generation;

                lock (syncRoot)
                {
                    client = conn;
                    generation = Volatile.Read(ref connectionGeneration);
                }

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
                    TotalBytes = buffer.Length,
                    Generation = generation
                };

                bool startNow = false;

                lock (sendLock)
                {
                    sendQueue.Enqueue(state);

                    if (!sendInProgress)
                    {
                        sendInProgress = true;
                        startNow = true;
                    }
                }

                if (startNow)
                    StartNextSend();

                return true;
            }
            catch (Exception e)
            {
                RaiseError(e);
                return false;
            }
        }

        private void StartNextSend()
        {
            SendState state = null;

            lock (sendLock)
            {
                while (sendQueue.Count > 0)
                {
                    SendState candidate = sendQueue.Peek();

                    if (IsCurrentConnection(candidate.Socket, candidate.Generation) &&
                        Volatile.Read(ref closing) == 0 &&
                        Volatile.Read(ref disposed) == 0)
                    {
                        state = candidate;
                        break;
                    }

                    sendQueue.Dequeue();
                }

                if (state == null)
                {
                    sendInProgress = false;
                    return;
                }
            }

            try
            {
                BeginSend(state);
            }
            catch (Exception e)
            {
                bool report =
                    IsCurrentConnection(state.Socket, state.Generation) &&
                    Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0;

                RemoveCurrentSend(state);

                if (report)
                    RaiseError(e);

                StartNextSend();
            }
        }

        private void BeginSend(SendState state)
        {
            if (!IsCurrentConnection(state.Socket, state.Generation))
                throw new InvalidOperationException("현재 연결과 다른 Socket의 Send 요청입니다.");

            state.Socket.BeginSend(
                state.Buffer,
                state.Offset,
                state.TotalBytes - state.Offset,
                SocketFlags.None,
                OnSendCallBack,
                state);
        }

        private void OnSendCallBack(IAsyncResult ar)
        {
            SendState state = (SendState)ar.AsyncState;

            try
            {
                int bytesWritten = state.Socket.EndSend(ar);

                if (!IsCurrentConnection(state.Socket, state.Generation))
                {
                    RemoveCurrentSend(state);
                    StartNextSend();
                    return;
                }

                if (bytesWritten <= 0)
                    throw new SocketException((int)SocketError.ConnectionReset);

                state.Offset += bytesWritten;

                if (state.Offset < state.TotalBytes)
                {
                    BeginSend(state);
                    return;
                }

                RemoveCurrentSend(state);

                AsyncSocketSendEventArgs sev =
                    new AsyncSocketSendEventArgs(this.id, state.Offset);

                Sent(sev);
                StartNextSend();
            }
            catch (ObjectDisposedException)
            {
                bool report =
                    IsCurrentGeneration(state.Generation) &&
                    Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0;

                RemoveCurrentSend(state);

                if (report)
                    RaiseError(new ObjectDisposedException("Socket"));

                StartNextSend();
            }
            catch (Exception e)
            {
                bool report =
                    IsCurrentConnection(state.Socket, state.Generation) &&
                    Volatile.Read(ref closing) == 0 &&
                    Volatile.Read(ref disposed) == 0;

                RemoveCurrentSend(state);

                if (report)
                    RaiseError(e);

                StartNextSend();
            }
        }

        private void RemoveCurrentSend(SendState state)
        {
            lock (sendLock)
            {
                if (sendQueue.Count > 0 && ReferenceEquals(sendQueue.Peek(), state))
                {
                    sendQueue.Dequeue();
                    return;
                }

                // 정상적으로는 발생하지 않지만 Queue 정합성이 깨졌을 때 해당 State만 제거한다.
                if (sendQueue.Count > 0)
                {
                    Queue<SendState> temp = new Queue<SendState>();

                    while (sendQueue.Count > 0)
                    {
                        SendState current = sendQueue.Dequeue();

                        if (!ReferenceEquals(current, state))
                            temp.Enqueue(current);
                    }

                    while (temp.Count > 0)
                        sendQueue.Enqueue(temp.Dequeue());
                }
            }
        }

        private void ClearSendQueue()
        {
            lock (sendLock)
            {
                sendQueue.Clear();
                sendInProgress = false;
            }
        }

        /// <summary>
        /// 소켓 연결을 비동기적으로 종료한다.
        /// </summary>
        public void Close()
        {
            if (Volatile.Read(ref disposed) != 0)
                return;

            if (Interlocked.CompareExchange(ref closing, 1, 0) != 0)
                return;

            // 이전 Connect/Receive/Send Callback을 모두 stale 상태로 만든다.
            int closeGeneration = Interlocked.Increment(ref connectionGeneration);

            Socket client;
            Socket connecting;

            lock (syncRoot)
            {
                client = conn;
                connecting = pendingConnect;
                pendingConnect = null;
            }

            ClearSendQueue();
            Interlocked.Exchange(ref receiveStarted, 0);

            if (connecting != null && !ReferenceEquals(connecting, client))
                SafeClose(connecting);

            if (client == null)
            {
                lock (syncRoot)
                {
                    conn = null;
                }

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
                }
                catch (ObjectDisposedException)
                {
                }

                client.BeginDisconnect(
                    false,
                    OnCloseCallBack,
                    new ConnectState
                    {
                        Socket = client,
                        Generation = closeGeneration
                    });
            }
            catch (Exception e)
            {
                SafeClose(client);

                lock (syncRoot)
                {
                    if (ReferenceEquals(conn, client))
                        conn = null;
                }

                if (!(e is ObjectDisposedException))
                    RaiseError(e);

                NotifyClosedOnce();
            }
        }

        private void OnCloseCallBack(IAsyncResult ar)
        {
            ConnectState state = (ConnectState)ar.AsyncState;
            Socket client = state.Socket;

            try
            {
                client.EndDisconnect(ar);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException e)
            {
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

                lock (syncRoot)
                {
                    if (ReferenceEquals(conn, client))
                        conn = null;
                }

                ClearSendQueue();
                Interlocked.Exchange(ref receiveStarted, 0);
                NotifyClosedOnce();
            }
        }

        private void CompleteClose(Socket client, int generation, bool notify)
        {
            if (client == null)
                return;

            // 오래된 연결의 callback이 새 연결을 닫지 못하도록 현재 Socket인지 먼저 검사한다.
            if (!IsCurrentConnection(client, generation))
            {
                SafeClose(client);
                return;
            }

            Interlocked.Exchange(ref closing, 1);
            Interlocked.Increment(ref connectionGeneration);

            SafeClose(client);

            lock (syncRoot)
            {
                if (ReferenceEquals(conn, client))
                    conn = null;

                if (ReferenceEquals(pendingConnect, client))
                    pendingConnect = null;
            }

            ClearSendQueue();
            Interlocked.Exchange(ref receiveStarted, 0);

            if (notify)
                NotifyClosedOnce();
        }

        private bool IsCurrentConnection(Socket socket, int generation)
        {
            if (socket == null)
                return false;

            lock (syncRoot)
            {
                return ReferenceEquals(conn, socket) &&
                       generation == Volatile.Read(ref connectionGeneration);
            }
        }

        private bool IsCurrentGeneration(int generation)
        {
            return generation == Volatile.Read(ref connectionGeneration);
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
            Interlocked.Increment(ref connectionGeneration);

            Socket client;
            Socket connecting;

            lock (syncRoot)
            {
                client = conn;
                connecting = pendingConnect;
                conn = null;
                pendingConnect = null;
            }

            ClearSendQueue();
            Interlocked.Exchange(ref receiveStarted, 0);

            if (connecting != null && !ReferenceEquals(connecting, client))
                SafeClose(connecting);

            SafeClose(client);

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

        private void StartAccept()
        {
            try
            {
                if (Volatile.Read(ref stopping) != 0 ||
                    Volatile.Read(ref disposed) != 0)
                    return;

                Socket target;

                lock (syncRoot)
                {
                    target = listener;
                }

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

        private void OnListenCallBack(IAsyncResult ar)
        {
            Socket acceptListener = (Socket)ar.AsyncState;
            Socket worker = null;
            bool continueAccept = false;

            try
            {
                worker = acceptListener.EndAccept(ar);

                if (Volatile.Read(ref stopping) != 0 ||
                    Volatile.Read(ref disposed) != 0)
                {
                    SafeClose(worker);
                    return;
                }

                continueAccept = true;

                AsyncSocketAcceptEventArgs aev =
                    new AsyncSocketAcceptEventArgs(worker);

                // Accepted 내부는 구독자별 예외 격리되어 있으므로,
                // 특정 OnAccept Handler 오류로 Accept Loop가 중단되지 않는다.
                Accepted(aev);
            }
            catch (ObjectDisposedException)
            {
                if (Volatile.Read(ref stopping) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
                    RaiseError(new ObjectDisposedException("listener"));
                    continueAccept = true;
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
                    continueAccept = true;
                }
            }
            finally
            {
                if (continueAccept &&
                    Volatile.Read(ref stopping) == 0 &&
                    Volatile.Read(ref disposed) == 0)
                {
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