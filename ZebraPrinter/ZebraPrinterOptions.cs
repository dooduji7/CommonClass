using System.IO.Ports;

namespace ZebraPrinter
{
    /// <summary>
    /// Zebra Printer 통신 설정
    /// </summary>
    public class ZebraPrinterOptions
    {
        public ZebraConnectionType ConnectionType { get; set; }

        // Ethernet
        public string IpAddress { get; set; }
        public int Port { get; set; }

        // Serial
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }

        // Status
        public int ReceiveTimeout { get; set; }
        public int StatusTimeout { get; set; }

        public ZebraPrinterOptions()
        {
            ConnectionType = ZebraConnectionType.Ethernet;

            IpAddress = string.Empty;
            Port = 9100;

            PortName = string.Empty;
            BaudRate = 9600;
            DataBits = 8;
            Parity = Parity.None;
            StopBits = StopBits.One;

            ReceiveTimeout = 3000;
            StatusTimeout = 1500;
        }
    }
}