using System;

namespace ToolHandler.Core
{
    public class ToolConnectionEventArgs : EventArgs
    {
        public ToolConnectionState State { get; }

        public DateTime ChangedTime { get; }

        public ToolConnectionEventArgs(
            ToolConnectionState state)
        {
            State = state;
            ChangedTime = DateTime.Now;
        }
    }
}