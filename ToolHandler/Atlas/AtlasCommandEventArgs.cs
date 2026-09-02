using System;

namespace ToolHandler.Atlas
{
    public enum AtlasCommandType
    {
        Job = 0,
        PSet,
        Vin
    }

    public class AtlasCommandEventArgs : EventArgs
    {
        public AtlasCommandType CommandType { get; private set; }
        public string Value { get; private set; }
        public bool Success { get; private set; }
        public string Message { get; private set; }
        public DateTime CompletedTime { get; private set; }

        public AtlasCommandEventArgs(
            AtlasCommandType commandType,
            string value,
            bool success,
            string message)
        {
            CommandType = commandType;
            Value = value ?? string.Empty;
            Success = success;
            Message = message ?? string.Empty;
            CompletedTime = DateTime.Now;
        }
    }
}
