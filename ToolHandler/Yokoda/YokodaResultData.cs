namespace ToolHandler.Yokoda
{
    public class YokodaResultData
    {
        public int PSet { get; set; }
        public string TotalResult { get; set; }
        public string Torque { get; set; }
        public string TorqueResult { get; set; }
        public string Angle { get; set; }
        public string AngleResult { get; set; }
        public string WorkName { get; set; }

        public YokodaResultData()
        {
            PSet = 0;
            TotalResult = string.Empty;
            Torque = string.Empty;
            TorqueResult = string.Empty;
            Angle = string.Empty;
            AngleResult = string.Empty;
            WorkName = string.Empty;
        }
    }
}
