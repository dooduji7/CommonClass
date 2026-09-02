namespace ToolHandler.Atlas
{
    public class TorqueResultData
    {
        public string Mid { get; set; }
        public string PSet { get; set; }
        public string JobNo { get; set; }
        public string TotalResult { get; set; }

        public string Torque { get; set; }
        public string TorqueMin { get; set; }
        public string TorqueMax { get; set; }
        public string TorqueResult { get; set; }

        public string Angle { get; set; }
        public string AngleMin { get; set; }
        public string AngleMax { get; set; }
        public string AngleResult { get; set; }

        public string RundownAngle { get; set; }
        public string RundownAngleMin { get; set; }
        public string RundownAngleMax { get; set; }
        public string RundownAngleResult { get; set; }

        public string ResultType { get; set; }
        public string SourceData { get; set; }

        public TorqueResultData()
        {
            ResultType = string.Empty;
        }
    }
}
