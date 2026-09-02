using System;

namespace CommonClass.Worker
{
    public sealed class JobStateChangedEventArgs : EventArgs
    {
        internal JobStateChangedEventArgs(string jobName, JobState previousState, JobState state)
        {
            JobName = jobName;
            PreviousState = previousState;
            State = state;
        }

        public string JobName { get; private set; }
        public JobState PreviousState { get; private set; }
        public JobState State { get; private set; }
    }

    public sealed class JobErrorEventArgs : EventArgs
    {
        internal JobErrorEventArgs(string jobName, Exception exception, DateTime occurredAt)
        {
            JobName = jobName;
            Exception = exception;
            OccurredAt = occurredAt;
        }

        public string JobName { get; private set; }
        public Exception Exception { get; private set; }
        public DateTime OccurredAt { get; private set; }
    }
}
