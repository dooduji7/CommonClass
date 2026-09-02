using System;
using System.Diagnostics;
using System.Threading;

namespace CommonClass.Worker
{
    public sealed class JobWorker : IDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly Action _action;
        private readonly ManualResetEvent _stopSignal = new ManualResetEvent(false);
        private Thread _thread;
        private JobState _state = JobState.Stopped;
        private bool _isExecuting;
        private bool _disposeRequested;
        private bool _signalDisposed;
        private DateTime? _lastStartTime;
        private DateTime? _lastEndTime;
        private TimeSpan? _lastExecutionTime;
        private long _runCount;
        private long _errorCount;
        private Exception _lastException;

        internal JobWorker(string name, Action action, TimeSpan interval)
        {
            Name = name;
            _action = action;
            Interval = interval;
        }

        public event EventHandler<JobStateChangedEventArgs> StateChanged;
        public event EventHandler<JobErrorEventArgs> Error;

        public string Name { get; private set; }
        public TimeSpan Interval { get; private set; }

        public bool Start()
        {
            JobStateChangedEventArgs stateChange;

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                if (_state != JobState.Stopped)
                    return false;

                _stopSignal.Reset();
                stateChange = ChangeStateNoLock(JobState.Starting);
                _thread = new Thread(Run);
                _thread.IsBackground = true;
                _thread.Name = "JobWorker-" + Name;

                try
                {
                    _thread.Start();
                }
                catch
                {
                    _thread = null;
                    _state = JobState.Stopped;
                    throw;
                }
            }

            RaiseStateChanged(stateChange);
            return true;
        }

        public bool Stop(int timeoutMilliseconds)
        {
            if (timeoutMilliseconds < Timeout.Infinite)
                throw new ArgumentOutOfRangeException("timeoutMilliseconds");

            Thread thread;
            JobStateChangedEventArgs stateChange = null;

            lock (_syncRoot)
            {
                if (_state == JobState.Stopped)
                    return true;

                if (_state != JobState.Stopping)
                    stateChange = ChangeStateNoLock(JobState.Stopping);

                _stopSignal.Set();
                thread = _thread;
            }

            RaiseStateChanged(stateChange);

            if (thread == null || thread == Thread.CurrentThread)
                return thread == null;

            return thread.Join(timeoutMilliseconds);
        }

        public JobStatus GetStatus()
        {
            lock (_syncRoot)
            {
                return new JobStatus(
                    Name,
                    _state,
                    _isExecuting,
                    Interval,
                    _lastStartTime,
                    _lastEndTime,
                    _lastExecutionTime,
                    _runCount,
                    _errorCount,
                    _lastException);
            }
        }

        public void Dispose()
        {
            bool disposeSignal = false;

            lock (_syncRoot)
            {
                if (_disposeRequested)
                    return;

                _disposeRequested = true;
                _stopSignal.Set();
                disposeSignal = _state == JobState.Stopped;
            }

            if (disposeSignal)
                DisposeSignal();
        }

        private void Run()
        {
            JobStateChangedEventArgs stateChange = null;

            lock (_syncRoot)
            {
                if (_state == JobState.Starting)
                    stateChange = ChangeStateNoLock(JobState.Running);
            }

            RaiseStateChanged(stateChange);

            try
            {
                while (!_stopSignal.WaitOne(0))
                {
                    ExecuteOnce();

                    // The interval starts after one complete invocation. This also
                    // makes overlap impossible because a worker owns one thread.
                    if (_stopSignal.WaitOne(Interval))
                        break;
                }
            }
            finally
            {
                lock (_syncRoot)
                {
                    _isExecuting = false;
                    _thread = null;
                    stateChange = ChangeStateNoLock(JobState.Stopped);
                }

                RaiseStateChanged(stateChange);

                if (_disposeRequested)
                    DisposeSignal();
            }
        }

        private void ExecuteOnce()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            lock (_syncRoot)
            {
                _isExecuting = true;
                _lastStartTime = DateTime.Now;
            }

            try
            {
                _action();
            }
            catch (Exception exception)
            {
                lock (_syncRoot)
                {
                    _errorCount++;
                    _lastException = exception;
                }

                RaiseError(new JobErrorEventArgs(Name, exception, DateTime.Now));
            }
            finally
            {
                stopwatch.Stop();

                lock (_syncRoot)
                {
                    _runCount++;
                    _lastEndTime = DateTime.Now;
                    _lastExecutionTime = stopwatch.Elapsed;
                    _isExecuting = false;
                }
            }
        }

        private JobStateChangedEventArgs ChangeStateNoLock(JobState newState)
        {
            if (_state == newState)
                return null;

            JobState oldState = _state;
            _state = newState;
            return new JobStateChangedEventArgs(Name, oldState, newState);
        }

        private void RaiseStateChanged(JobStateChangedEventArgs eventArgs)
        {
            if (eventArgs == null)
                return;

            EventHandler<JobStateChangedEventArgs> handlers = StateChanged;
            if (handlers == null)
                return;

            foreach (EventHandler<JobStateChangedEventArgs> handler in handlers.GetInvocationList())
            {
                try { handler(this, eventArgs); }
                catch { }
            }
        }

        private void RaiseError(JobErrorEventArgs eventArgs)
        {
            EventHandler<JobErrorEventArgs> handlers = Error;
            if (handlers == null)
                return;

            foreach (EventHandler<JobErrorEventArgs> handler in handlers.GetInvocationList())
            {
                try { handler(this, eventArgs); }
                catch { }
            }
        }

        private void DisposeSignal()
        {
            lock (_syncRoot)
            {
                if (_signalDisposed)
                    return;

                _signalDisposed = true;
                _stopSignal.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposeRequested)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
