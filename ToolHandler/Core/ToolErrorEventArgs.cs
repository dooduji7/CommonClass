using System;

namespace ToolHandler.Core
{
    public class ToolErrorEventArgs : EventArgs
    {
        public string Message { get; }

        public Exception Exception { get; }

        public DateTime OccurredTime { get; }

        public ToolErrorEventArgs(
            string message,
            Exception exception = null)
        {
            Message = message ?? string.Empty;
            Exception = exception;
            OccurredTime = DateTime.Now;
        }
    }
}