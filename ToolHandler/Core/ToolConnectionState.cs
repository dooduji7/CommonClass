namespace ToolHandler.Core
{
    public enum ToolConnectionState
    {
        Stopped = 0,

        Connecting,
        Connected,
        Communicating,

        Reconnecting,
        Disconnected,

        Error
    }
}