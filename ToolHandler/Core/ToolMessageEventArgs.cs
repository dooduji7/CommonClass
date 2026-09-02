using System;

namespace ToolHandler.Core
{
    public class ToolMessageEventArgs : EventArgs
    {
        public string Message { get; }

        public bool IsReceived { get; }

        public DateTime Timestamp { get; }

        public ToolMessageEventArgs(
            string message,
            bool isReceived)
        {
            Message = message ?? string.Empty;
            IsReceived = isReceived;
            Timestamp = DateTime.Now;
        }
    }
}