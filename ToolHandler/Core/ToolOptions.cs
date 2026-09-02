namespace ToolHandler.Core
{
    public class ToolOptions
    {
        public string Name { get; set; }

        public string IpAddress { get; set; }

        public int Port { get; set; }

        public int ReceiveTimeout { get; set; }

        public int ReconnectInterval { get; set; }

        public int KeepAliveInterval { get; set; }

        public bool AutoReconnect { get; set; }


        public ToolOptions()
        {
            Name = string.Empty;
            IpAddress = "127.0.0.1";
            Port = 0;

            ReceiveTimeout = 3000;
            ReconnectInterval = 3000;
            KeepAliveInterval = 5000;

            AutoReconnect = true;
        }
    }
}