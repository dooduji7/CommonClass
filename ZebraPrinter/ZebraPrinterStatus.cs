namespace ZebraPrinter
{
    /// <summary>
    /// Zebra Printer 상태 정보
    /// </summary>
    public class ZebraPrinterStatus
    {
        public bool HasResponse { get; internal set; }
        public bool IsReady { get; internal set; }

        public bool IsPaperOut { get; internal set; }
        public bool IsPaused { get; internal set; }
        public bool IsBufferFull { get; internal set; }

        public bool IsHeadOpen { get; internal set; }
        public bool IsRibbonOut { get; internal set; }

        public bool IsHeadUnderTemperature { get; internal set; }
        public bool IsHeadOverTemperature { get; internal set; }

        public bool IsLabelWaiting { get; internal set; }

        public bool IsThermalTransferMode { get; internal set; }

        public string PrintModeCode { get; internal set; }
        public string PrintModeText { get; internal set; }

        public string ErrorMessage { get; internal set; }
        public string RawResponse { get; internal set; }

        public ZebraPrinterStatus()
        {
            PrintModeCode = string.Empty;
            PrintModeText = string.Empty;
            ErrorMessage = string.Empty;
            RawResponse = string.Empty;
        }
    }
}