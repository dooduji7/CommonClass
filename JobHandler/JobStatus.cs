using System;

namespace CommonClass.Worker
{
    public sealed class JobStatus
    {
        internal JobStatus(
            string name,
            JobState state,
            bool isExecuting,
            TimeSpan interval,
            DateTime? lastStartTime,
            DateTime? lastEndTime,
            TimeSpan? lastExecutionTime,
            long runCount,
            long errorCount,
            Exception lastException)
        {
            Name = name;
            State = state;
            IsExecuting = isExecuting;
            Interval = interval;
            LastStartTime = lastStartTime;
            LastEndTime = lastEndTime;
            LastExecutionTime = lastExecutionTime;
            RunCount = runCount;
            ErrorCount = errorCount;
            LastException = lastException;
        }

        public string Name { get; private set; }
        public JobState State { get; private set; }
        public bool IsExecuting { get; private set; }
        public TimeSpan Interval { get; private set; }
        public DateTime? LastStartTime { get; private set; }
        public DateTime? LastEndTime { get; private set; }
        public TimeSpan? LastExecutionTime { get; private set; }
        public long RunCount { get; private set; }
        public long ErrorCount { get; private set; }
        public Exception LastException { get; private set; }
    }
}
