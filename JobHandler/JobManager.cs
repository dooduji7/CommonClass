using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace CommonClass.Worker
{
    public sealed class JobManager : IDisposable
    {
        public const int DefaultStopTimeoutMilliseconds = 5000;

        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, JobWorker> _workers =
            new Dictionary<string, JobWorker>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        public event EventHandler<JobStateChangedEventArgs> JobStateChanged;
        public event EventHandler<JobErrorEventArgs> JobError;

        public void Register(string name, Action action, int intervalMilliseconds)
        {
            Register(name, action, TimeSpan.FromMilliseconds(intervalMilliseconds));
        }

        public void Register(string name, Action action, TimeSpan interval)
        {
            ValidateRegistration(name, action, interval);

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                if (_workers.ContainsKey(name))
                    throw new InvalidOperationException("A job with the same name is already registered: " + name);

                JobWorker worker = new JobWorker(name, action, interval);
                worker.StateChanged += WorkerStateChanged;
                worker.Error += WorkerError;
                _workers.Add(name, worker);
            }
        }

        public bool Start(string name)
        {
            return GetWorker(name).Start();
        }

        public bool Stop(string name)
        {
            return Stop(name, DefaultStopTimeoutMilliseconds);
        }

        public bool Stop(string name, int timeoutMilliseconds)
        {
            return GetWorker(name).Stop(timeoutMilliseconds);
        }

        public bool Restart(string name)
        {
            return Restart(name, DefaultStopTimeoutMilliseconds);
        }

        public bool Restart(string name, int timeoutMilliseconds)
        {
            JobWorker worker = GetWorker(name);
            return worker.Stop(timeoutMilliseconds) && worker.Start();
        }

        public void StartAll()
        {
            foreach (JobWorker worker in GetWorkersSnapshot())
                worker.Start();
        }

        public bool StopAll()
        {
            return StopAll(DefaultStopTimeoutMilliseconds);
        }

        public bool StopAll(int timeoutMilliseconds)
        {
            if (timeoutMilliseconds < Timeout.Infinite)
                throw new ArgumentOutOfRangeException("timeoutMilliseconds");

            bool allStopped = true;
            foreach (JobWorker worker in GetWorkersSnapshot())
            {
                if (!worker.Stop(timeoutMilliseconds))
                    allStopped = false;
            }

            return allStopped;
        }

        public bool IsRunning(string name)
        {
            JobState state = GetState(name);
            return state == JobState.Starting || state == JobState.Running;
        }

        public JobState GetState(string name)
        {
            return GetWorker(name).GetStatus().State;
        }

        public JobStatus GetStatus(string name)
        {
            return GetWorker(name).GetStatus();
        }

        public IReadOnlyList<JobStatus> GetAllStatus()
        {
            List<JobStatus> statuses = GetWorkersSnapshot()
                .Select(worker => worker.GetStatus())
                .OrderBy(status => status.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ReadOnlyCollection<JobStatus>(statuses);
        }

        public bool Remove(string name)
        {
            return Remove(name, DefaultStopTimeoutMilliseconds);
        }

        public bool Remove(string name, int timeoutMilliseconds)
        {
            JobWorker worker = GetWorker(name);
            if (!worker.Stop(timeoutMilliseconds))
                return false;

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                JobWorker current;
                if (!_workers.TryGetValue(name, out current) || !ReferenceEquals(current, worker))
                    return false;

                _workers.Remove(name);
            }

            worker.StateChanged -= WorkerStateChanged;
            worker.Error -= WorkerError;
            worker.Dispose();
            return true;
        }

        public void Dispose()
        {
            JobWorker[] workers;

            lock (_syncRoot)
            {
                if (_disposed)
                    return;

                _disposed = true;
                workers = _workers.Values.ToArray();
                _workers.Clear();
            }

            foreach (JobWorker worker in workers)
            {
                worker.Stop(DefaultStopTimeoutMilliseconds);
                worker.StateChanged -= WorkerStateChanged;
                worker.Error -= WorkerError;
                worker.Dispose();
            }
        }

        private JobWorker GetWorker(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A job name is required.", "name");

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                JobWorker worker;
                if (!_workers.TryGetValue(name, out worker))
                    throw new KeyNotFoundException("The job is not registered: " + name);

                return worker;
            }
        }

        private JobWorker[] GetWorkersSnapshot()
        {
            lock (_syncRoot)
            {
                ThrowIfDisposed();
                return _workers.Values.ToArray();
            }
        }

        private static void ValidateRegistration(string name, Action action, TimeSpan interval)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A job name is required.", "name");
            if (action == null)
                throw new ArgumentNullException("action");
            if (interval.TotalMilliseconds < 1 || interval.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException("interval", "Interval must be between 1 ms and Int32.MaxValue ms.");
        }

        private void WorkerStateChanged(object sender, JobStateChangedEventArgs e)
        {
            RaiseEvent(JobStateChanged, e);
        }

        private void WorkerError(object sender, JobErrorEventArgs e)
        {
            RaiseEvent(JobError, e);
        }

        private void RaiseEvent<TEventArgs>(EventHandler<TEventArgs> handlers, TEventArgs eventArgs)
            where TEventArgs : EventArgs
        {
            if (handlers == null)
                return;

            foreach (EventHandler<TEventArgs> handler in handlers.GetInvocationList())
            {
                try { handler(this, eventArgs); }
                catch { }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
