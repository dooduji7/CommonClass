using System;

namespace MessageSocket
{
    public class MessageReceivedEventArgs : EventArgs
    {
        private readonly byte[] data;
        private readonly string text;

        public MessageReceivedEventArgs(byte[] data, string text)
        {
            this.data = data ?? new byte[0];
            this.text = text ?? string.Empty;
        }

        public byte[] Data
        {
            get { return data; }
        }

        public string Text
        {
            get { return text; }
        }
    }

    public class MessageSocketErrorEventArgs : EventArgs
    {
        public MessageSocketErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }

    public class MessageSocketSendEventArgs : EventArgs
    {
        public MessageSocketSendEventArgs(int bytes)
        {
            Bytes = bytes;
        }

        public int Bytes { get; private set; }
    }
}
