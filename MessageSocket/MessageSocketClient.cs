using System;
using System.Net.Sockets;
using AsyncSocket;

namespace MessageSocket
{
    /// <summary>
    /// AsyncSocket 위에서 TCP stream을 완성된 전문 단위로 조립하여 전달하는 Client.
    /// 송신 데이터의 Header/Length/Delimiter 구성은 프로젝트별 전문 규격에 맡기며,
    /// 이 클래스는 수신 전문 경계 처리와 공통 Socket 생명주기를 담당한다.
    /// </summary>
    public sealed class MessageSocketClient : IDisposable
    {
        private readonly object syncRoot = new object();
        private readonly MessageSocketOptions options;
        private readonly IMessageFrameParser frameParser;
        private readonly ReceiveBuffer receiveBuffer = new ReceiveBuffer();
        private readonly AsyncSocketClient socketClient;
        private readonly bool stripDefaultDelimiter;

        private bool connected;
        private bool disposed;

        public event EventHandler Connected;
        public event EventHandler Closed;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<MessageSocketSendEventArgs> Sent;
        public event EventHandler<MessageSocketErrorEventArgs> Error;

        public MessageSocketClient(MessageSocketOptions options)
            : this(options, null)
        {
        }

        public MessageSocketClient(MessageSocketOptions options, IMessageFrameParser customFrameParser)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            options.Validate();
            this.options = options;
            frameParser = customFrameParser ?? CreateFrameParser(options);
            stripDefaultDelimiter = customFrameParser == null &&
                                    options.FrameMode == MessageFrameMode.Delimiter &&
                                    !options.IncludeDelimiter;

            socketClient = new AsyncSocketClient(0);
            socketClient.OnConnet += SocketClient_OnConnet;
            socketClient.OnClose += SocketClient_OnClose;
            socketClient.OnReceive += SocketClient_OnReceive;
            socketClient.OnSend += SocketClient_OnSend;
            socketClient.OnError += SocketClient_OnError;
        }

        public bool IsConnected
        {
            get
            {
                lock (syncRoot)
                {
                    return connected;
                }
            }
        }

        public MessageSocketOptions Options
        {
            get { return options; }
        }

        /// <summary>
        /// Options의 Host/Port로 비동기 연결을 시작한다.
        /// 실제 연결 완료는 Connected 이벤트에서 확인한다.
        /// </summary>
        public bool Connect()
        {
            ThrowIfDisposed();

            lock (syncRoot)
            {
                receiveBuffer.Clear();
            }

            return socketClient.Connect(options.Host, options.Port);
        }

        /// <summary>
        /// 전문 규격에 맞게 구성된 원본 byte[]를 그대로 송신한다.
        /// </summary>
        public bool Send(byte[] data)
        {
            ThrowIfDisposed();

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            byte[] copy = new byte[data.Length];
            Buffer.BlockCopy(data, 0, copy, 0, data.Length);
            return socketClient.Send(copy);
        }

        /// <summary>
        /// 문자열을 Options.Encoding으로 변환하여 그대로 송신한다.
        /// Delimiter/Header 등 프로토콜 문자는 호출부에서 문자열에 포함해야 한다.
        /// </summary>
        public bool Send(string text)
        {
            ThrowIfDisposed();

            if (text == null)
                throw new ArgumentNullException(nameof(text));

            return Send(options.Encoding.GetBytes(text));
        }

        public void Close()
        {
            if (disposed)
                return;

            socketClient.Close();
        }

        private static IMessageFrameParser CreateFrameParser(MessageSocketOptions options)
        {
            switch (options.FrameMode)
            {
                case MessageFrameMode.Delimiter:
                    return new DelimiterFrameParser(options.Delimiter);

                case MessageFrameMode.FixedLength:
                    return new FixedLengthFrameParser(options.FixedFrameLength);

                case MessageFrameMode.LengthField:
                    return new LengthFieldFrameParser(
                        options.LengthFieldOffset,
                        options.LengthFieldSize,
                        options.LengthFieldBigEndian,
                        options.LengthFieldAdjustment,
                        options.MaxFrameLength);

                default:
                    throw new ArgumentOutOfRangeException(nameof(options.FrameMode));
            }
        }

        private void SocketClient_OnConnet(object sender, AsyncSocketConnectionEventArgs e)
        {
            lock (syncRoot)
            {
                connected = true;
                receiveBuffer.Clear();
            }

            RaiseEvent(Connected, EventArgs.Empty);
        }

        private void SocketClient_OnClose(object sender, AsyncSocketConnectionEventArgs e)
        {
            lock (syncRoot)
            {
                connected = false;
                receiveBuffer.Clear();
            }

            RaiseEvent(Closed, EventArgs.Empty);
        }

        private void SocketClient_OnSend(object sender, AsyncSocketSendEventArgs e)
        {
            EventHandler<MessageSocketSendEventArgs> handler = Sent;
            if (handler == null)
                return;

            try
            {
                handler(this, new MessageSocketSendEventArgs(e.SendBytes));
            }
            catch
            {
                // 사용자 Event Handler 예외가 Socket 통신 흐름을 중단시키지 않도록 격리한다.
            }
        }

        private void SocketClient_OnReceive(object sender, AsyncSocketReceiveEventArgs e)
        {
            try
            {
                ProcessReceivedData(e.ReceiveData, e.ReceiveBytes);
            }
            catch (Exception ex)
            {
                lock (syncRoot)
                {
                    receiveBuffer.Clear();
                }

                RaiseError(ex);
            }
        }

        private void ProcessReceivedData(byte[] data, int count)
        {
            lock (syncRoot)
            {
                receiveBuffer.Append(data, count);

                if (receiveBuffer.Count > options.MaxBufferLength)
                    throw new InvalidOperationException("수신 Buffer가 MaxBufferLength를 초과했습니다.");

                while (receiveBuffer.Count > 0)
                {
                    byte[] snapshot = receiveBuffer.ToArray();
                    int frameLength;

                    if (!frameParser.TryGetFrameLength(snapshot, snapshot.Length, out frameLength))
                        break;

                    if (frameLength <= 0 || frameLength > options.MaxFrameLength)
                        throw new InvalidOperationException("FrameParser가 유효하지 않은 전문 길이를 반환했습니다. Length=" + frameLength);

                    if (frameLength > snapshot.Length)
                        break;

                    byte[] frame = receiveBuffer.Take(frameLength);
                    byte[] eventData = frame;

                    if (stripDefaultDelimiter)
                    {
                        int payloadLength = frame.Length - options.Delimiter.Length;
                        if (payloadLength < 0)
                            throw new InvalidOperationException("Delimiter보다 짧은 Frame이 생성되었습니다.");

                        eventData = new byte[payloadLength];
                        if (payloadLength > 0)
                            Buffer.BlockCopy(frame, 0, eventData, 0, payloadLength);
                    }

                    string text = options.Encoding.GetString(eventData);
                    RaiseMessageReceived(eventData, text);
                }
            }
        }

        private void SocketClient_OnError(object sender, AsyncSocketErrorEventArgs e)
        {
            RaiseError(e.AsyncSocketException);
        }

        private void RaiseMessageReceived(byte[] data, string text)
        {
            EventHandler<MessageReceivedEventArgs> handler = MessageReceived;
            if (handler == null)
                return;

            try
            {
                handler(this, new MessageReceivedEventArgs(data, text));
            }
            catch
            {
                // 사용자 Event Handler 예외 격리
            }
        }

        private void RaiseError(Exception exception)
        {
            EventHandler<MessageSocketErrorEventArgs> handler = Error;
            if (handler == null)
                return;

            try
            {
                handler(this, new MessageSocketErrorEventArgs(exception));
            }
            catch
            {
                // Error Handler 내부 예외가 다시 통신부로 전파되지 않도록 격리한다.
            }
        }

        private void RaiseEvent(EventHandler handler, EventArgs args)
        {
            if (handler == null)
                return;

            try
            {
                handler(this, args);
            }
            catch
            {
                // 사용자 Event Handler 예외 격리
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(MessageSocketClient));
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            socketClient.OnConnet -= SocketClient_OnConnet;
            socketClient.OnClose -= SocketClient_OnClose;
            socketClient.OnReceive -= SocketClient_OnReceive;
            socketClient.OnSend -= SocketClient_OnSend;
            socketClient.OnError -= SocketClient_OnError;

            socketClient.Dispose();

            lock (syncRoot)
            {
                connected = false;
                receiveBuffer.Clear();
            }
        }
    }
}
