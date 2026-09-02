namespace ToolHandler.Estic
{
    public class EsticResultData
    {
        public string Value { get; set; }
        public string Result { get; set; }
        public string PSet { get; set; }

        public EsticResultData()
        {
            Value = string.Empty;
            Result = string.Empty;
            PSet = string.Empty;
        }
    }
}
