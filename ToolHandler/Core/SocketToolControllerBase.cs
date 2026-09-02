using System;
using System.Text;
using System.Threading;
using SocketClient;

namespace ToolHandler.Core
{
    public abstract class SocketToolControllerBase
        : ToolControllerBase
    {
        private Thread _workThread;
        private readonly ManualResetEvent _stopEvent;
        private clsSocketClient _socketClient;
        private readonly StringBuilder _receiveBuffer;
        private DateTime _lastKeepAliveTime;
        private readonly object _sendLock = new object();
        private bool _disposed;

        protected SocketToolControllerBase(
            ToolOptions options)
            : base(options)
        {
            _stopEvent =
                new ManualResetEvent(false);

            _receiveBuffer =
                new StringBuilder();

            _lastKeepAliveTime =
                DateTime.MinValue;
        }

        public override bool Start()
        {
            if (_disposed)
                return false;

            if (IsRunning)
                return true;

            try
            {
                _stopEvent.Reset();
                _receiveBuffer.Clear();

                IsRunning = true;

                _workThread =
                    new Thread(WorkProc);

                _workThread.IsBackground = true;
                _workThread.Start();

                return true;
            }
            catch (Exception ex)
            {
                IsRunning = false;

                SetConnectionState(
                    ToolConnectionState.Error);

                OnError(
                    "Tool communication start failed.",
                    ex);

                return false;
            }
        }

        public override bool Stop()
        {
            if (_disposed)
                return true;

            if (!IsRunning)
            {
                SetConnectionState(
                    ToolConnectionState.Stopped);

                return true;
            }

            try
            {
                _stopEvent.Set();

                // NetworkStream.Read가 대기 중이어도 Socket을 닫아
                // Receive Thread가 빠져나올 수 있도록 한다.
                DisconnectSocket();

                Thread thread = _workThread;

                if (thread != null &&
                    thread.IsAlive &&
                    Thread.CurrentThread != thread)
                {
                    if (!thread.Join(3000))
                    {
                        OnError(
                            "Tool communication thread did not stop normally.");

                        return false;
                    }
                }

                // 외부 Thread에서 Stop한 경우에는 Join 이후이므로
                // WorkProc이 실제 종료된 상태다.
                if (Thread.CurrentThread != thread)
                {
                    IsRunning = false;

                    SetConnectionState(
                        ToolConnectionState.Stopped);
                }

                return true;
            }
            catch (Exception ex)
            {
                OnError(
                    "Tool communication stop failed.",
                    ex);

                return false;
            }
        }

        private void WorkProc()
        {
            try
            {
                while (!_stopEvent.WaitOne(0))
                {
                    if (!ConnectSocket())
                    {
                        if (!Options.AutoReconnect)
                            break;

                        WaitReconnect();
                        continue;
                    }

                    try
                    {
                        RunCommunication();
                    }
                    finally
                    {
                        bool isStopping =
                            _stopEvent.WaitOne(0);

                        DisconnectSocket();

                        try
                        {
                            OnSocketDisconnected(isStopping);
                        }
                        catch (Exception ex)
                        {
                            if (!isStopping)
                            {
                                OnError(
                                    "Tool disconnect processing failed.",
                                    ex);
                            }
                        }
                    }

                    if (_stopEvent.WaitOne(0))
                        break;

                    if (!Options.AutoReconnect)
                        break;

                    WaitReconnect();
                }
            }
            catch (Exception ex)
            {
                if (!_stopEvent.WaitOne(0))
                {
                    SetConnectionState(
                        ToolConnectionState.Error);

                    OnError(
                        "Tool communication error.",
                        ex);
                }
            }
            finally
            {
                DisconnectSocket();

                IsRunning = false;
                _workThread = null;

                if (_stopEvent.WaitOne(0))
                {
                    SetConnectionState(
                        ToolConnectionState.Stopped);
                }
                else
                {
                    SetConnectionState(
                        ToolConnectionState.Disconnected);
                }
            }
        }

        private bool ConnectSocket()
        {
            if (_stopEvent.WaitOne(0))
                return false;

            try
            {
                SetConnectionState(
                    ToolConnectionState.Connecting);

                DisconnectSocket();

                _socketClient =
                    new clsSocketClient(
                        Options.IpAddress,
                        Options.Port);

                _socketClient.ReceiveTimeout =
                    Options.ReceiveTimeout;

                if (!_socketClient.SocketConnect())
                {
                    string errorMessage =
                        _socketClient.ERROR_MESSAGE;

                    DisconnectSocket();

                    OnError(
                        "Tool socket connect failed. " +
                        errorMessage);

                    return false;
                }

                _receiveBuffer.Clear();

                _lastKeepAliveTime =
                    DateTime.Now;

                SetConnectionState(
                    ToolConnectionState.Connected);

                if (!OnSocketConnected())
                {
                    DisconnectSocket();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                DisconnectSocket();

                if (!_stopEvent.WaitOne(0))
                {
                    OnError(
                        "Tool socket connect failed.",
                        ex);
                }

                return false;
            }
        }

        private void DisconnectSocket()
        {
            clsSocketClient client =
                _socketClient;

            _socketClient = null;

            if (client == null)
                return;

            try
            {
                client.SocketDisconnect();
            }
            catch
            {
                // Disconnect 과정의 오류는 무시한다.
            }

            try
            {
                client.Dispose();
            }
            catch
            {
                // Dispose 과정의 오류는 무시한다.
            }
        }

        private void RunCommunication()
        {
            while (!_stopEvent.WaitOne(0))
            {
                clsSocketClient client =
                    _socketClient;

                if (client == null)
                    break;

                byte[] data;

                bool received =
                    client.ReadData(out data);

                if (_stopEvent.WaitOne(0))
                    break;

                if (received)
                {
                    if (data != null &&
                        data.Length > 0)
                    {
                        ProcessReceivedData(data);
                    }
                }
                else
                {
                    switch (client.LastReceiveState)
                    {
                        case clsSocketClient.ReceiveState.Timeout:
                            // 정상적인 무수신 상태.
                            // 장비별 명령 응답 Timeout 등을 확인할 수 있다.
                            OnReceiveTimeout();
                            break;

                        case clsSocketClient.ReceiveState.ConnectionClosed:
                            SetConnectionState(
                                ToolConnectionState.Disconnected);
                            return;

                        case clsSocketClient.ReceiveState.Error:
                            OnError(
                                "Tool receive failed. " +
                                client.ERROR_MESSAGE);
                            return;
                    }
                }

                ProcessKeepAlive();
            }
        }

        private void ProcessReceivedData(
            byte[] data)
        {
            string received =
                Encoding.ASCII.GetString(data);

            _receiveBuffer.Append(received);

            while (!_stopEvent.WaitOne(0))
            {
                string frame;

                if (!TryExtractFrame(
                    _receiveBuffer,
                    out frame))
                {
                    break;
                }

                if (string.IsNullOrEmpty(frame))
                    continue;

                OnMessage(
                    frame,
                    true);

                ProcessFrame(frame);
            }
        }

        private void ProcessKeepAlive()
        {
            if (Options.KeepAliveInterval <= 0)
                return;

            double elapsed =
                (DateTime.Now - _lastKeepAliveTime)
                .TotalMilliseconds;

            if (elapsed <
                Options.KeepAliveInterval)
            {
                return;
            }

            _lastKeepAliveTime =
                DateTime.Now;

            OnKeepAlive();
        }

        private void WaitReconnect()
        {
            if (_stopEvent.WaitOne(0))
                return;

            SetConnectionState(
                ToolConnectionState.Reconnecting);

            int interval =
                Options.ReconnectInterval;

            if (interval <= 0)
                interval = 1000;

            _stopEvent.WaitOne(interval);
        }

        protected bool Send(
            string message)
        {
            bool result;
            string errorMessage;

            if (string.IsNullOrEmpty(message))
                return false;

            lock (_sendLock)
            {
                clsSocketClient client =
                    _socketClient;

                if (client == null)
                    return false;

                result =
                    client.SendData(message);

                errorMessage =
                    client.ERROR_MESSAGE;
            }

            if (result)
            {
                OnMessage(
                    message,
                    false);

                return true;
            }

            if (!_stopEvent.WaitOne(0))
            {
                OnError(
                    "Tool message send failed. " +
                    errorMessage);
            }

            return false;
        }

        protected void RequestReconnect()
        {
            if (_stopEvent.WaitOne(0))
                return;

            DisconnectSocket();
        }

        protected void SetCommunicating()
        {
            SetConnectionState(
                ToolConnectionState.Communicating);
        }

        protected virtual bool OnSocketConnected()
        {
            return true;
        }

        protected virtual void OnKeepAlive()
        {
        }

        protected virtual void OnReceiveTimeout()
        {
        }

        protected virtual void OnSocketDisconnected(
            bool isStopping)
        {
        }

        protected abstract bool TryExtractFrame(
            StringBuilder buffer,
            out string frame);

        protected abstract void ProcessFrame(
            string frame);

        public override void Dispose()
        {
            if (_disposed)
                return;

            Thread thread = _workThread;
            bool calledFromWorker =
                thread != null &&
                Thread.CurrentThread == thread;

            if (!Stop())
                return;

            _disposed = true;

            // ResultReceived 등의 이벤트가 Work Thread에서 실행되므로
            // 해당 이벤트 안에서 Dispose가 호출될 가능성까지 방어한다.
            // Work Thread 자신이 WaitHandle을 Dispose하면 이후 finally에서
            // WaitOne 호출 시 ObjectDisposedException이 발생할 수 있다.
            if (!calledFromWorker)
                _stopEvent.Dispose();
        }
    }
}
