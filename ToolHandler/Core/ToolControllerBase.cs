using System;

namespace ToolHandler.Core
{
    public abstract class ToolControllerBase : IDisposable
    {
        private ToolConnectionState _connectionState;


        public ToolOptions Options { get; private set; }

        public abstract ToolType ToolType { get; }


        public ToolConnectionState ConnectionState
        {
            get
            {
                return _connectionState;
            }
        }


        public bool IsRunning { get; protected set; }


        public bool IsConnected
        {
            get
            {
                return _connectionState ==
                           ToolConnectionState.Connected ||
                       _connectionState ==
                           ToolConnectionState.Communicating;
            }
        }


        public event EventHandler<ToolConnectionEventArgs>
            StateChanged;

        public event EventHandler<ToolMessageEventArgs>
            Message;

        public event EventHandler<ToolErrorEventArgs>
            Error;


        protected ToolControllerBase(
            ToolOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Options = options;

            _connectionState =
                ToolConnectionState.Stopped;
        }


        public abstract bool Start();

        public abstract bool Stop();


        protected void SetConnectionState(
            ToolConnectionState state)
        {
            if (_connectionState == state)
                return;

            _connectionState = state;

            OnStateChanged(state);
        }


        protected virtual void OnStateChanged(
            ToolConnectionState state)
        {
            EventHandler<ToolConnectionEventArgs>
                handler = StateChanged;

            if (handler != null)
            {
                handler(
                    this,
                    new ToolConnectionEventArgs(state));
            }
        }


        protected virtual void OnMessage(
            string message,
            bool isReceived)
        {
            EventHandler<ToolMessageEventArgs>
                handler = Message;

            if (handler != null)
            {
                handler(
                    this,
                    new ToolMessageEventArgs(
                        message,
                        isReceived));
            }
        }


        protected virtual void OnError(
            string message,
            Exception exception = null)
        {
            EventHandler<ToolErrorEventArgs>
                handler = Error;

            if (handler != null)
            {
                handler(
                    this,
                    new ToolErrorEventArgs(
                        message,
                        exception));
            }
        }


        public virtual void Dispose()
        {
            Stop();
        }
    }
}