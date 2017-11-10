using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiddleStack.Profiling.Events;

namespace MiddleStack.Profiling
{
    internal abstract class Timing: ITiming, ITimingInfo
    {
        private readonly LiveProfiler _profiler;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Lazy<List<Timing>> _children = new Lazy<List<Timing>>(() => new List<Timing>());

        protected Timing(LiveProfiler profiler, string category, string name, string displayName, object parameters, Timing parent)
        {
            _profiler = profiler;

            Id = Guid.NewGuid();
            Category = category;
            Name = name;
            DisplayName = displayName;
            Parent = parent;
            Parameters = parameters;
            Start = DateTimeOffset.Now;

            if (parent != null)
            {
                if (parent.State != Profiling.TransactionState.Inflight)
                {
                    throw new InvalidOperationException("Unable to add a child step to this step, because the latter is already finished.");
                }

                VersionStarted = IncrementVersion();
                parent._children.Value.Add(this);
            }
        }

        public abstract Object SyncRoot { get; }
        public abstract int Version { get; }
        public abstract int IncrementVersion();
        public abstract TransactionState TransactionState { get; }
        public Guid Id { get; }
        public string Category { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public object Parameters { get; }
        public object Result { get; private set; }

        public DateTimeOffset Start { get; }

        public TimeSpan Duration => _stopwatch.Elapsed;

        public TimeSpan OwnDuration
        {
            get
            {
                var childrenDuration = TimeSpan.Zero;

                lock (SyncRoot)
                {
                    if (_children.IsValueCreated)
                    {
                        foreach (var child in _children.Value)
                        {
                            childrenDuration += child.Duration;
                        }
                    }

                    var result = Duration - childrenDuration;

                    if (result < TimeSpan.Zero) result = TimeSpan.Zero;

                    return result;
                }
            }
        }

        public TransactionState State { get; private set; } = TransactionState.Inflight;

        public Timing Parent { get; }
        public int VersionStarted { get; }
        public int? VersionFinished { get; private set; }

        public void Dispose()
        {
            Success();
        }

        protected abstract IProfilerEvent GetFinishEvent(int version);
        protected abstract SnapshotBase NewSnapshot();

        protected virtual SnapshotBase GetSnapshot(int? version)
        {
            lock (SyncRoot)
            {
                if (version != null && VersionStarted > version) return null;

                var snapshot = NewSnapshot();
                snapshot.Id = Id;
                snapshot.Category = Category;
                snapshot.Name = Name;
                snapshot.DisplayName = DisplayName;
                snapshot.Start = Start;
                snapshot.Parameters = Parameters;
                snapshot.Duration = Duration;
                snapshot.OwnDuration = OwnDuration;

                if (version == null || VersionFinished <= version)
                {
                    snapshot.State = State;
                    snapshot.Result = Result;
                }

                if (_children.IsValueCreated)
                {
                    snapshot.Steps = _children.Value
                        .Select(child => (StepSnapshot)child.GetSnapshot(version))
                        .Where(s => s != null).ToArray();
                }

                return snapshot;
            }
        }

        public TimeSpan Elapsed => _stopwatch.Elapsed;
        public void Success(object result = null)
        {
            lock (SyncRoot)
            {
                if (State == TransactionState.Inflight)
                {
                    if (_children.IsValueCreated && _children.Value.Any(c => c.State == TransactionState.Inflight))
                    {
                        throw new InvalidOperationException($"Unable to finish transaction or step '{Name}', category '{Category}', because some of its child steps haven't finished.");
                    }

                    Result = result;
                    State = TransactionState.Success;
                    VersionFinished = IncrementVersion();
                    _stopwatch.Stop();

                    TimingStore.SetCurrentTiming(Parent);

                    _profiler.RegisterEvent(GetFinishEvent(Version), this);
                }
            }
        }

        public void Failure(object result)
        {
            lock (SyncRoot)
            {
                if (State == TransactionState.Inflight)
                {
                    if (_children.IsValueCreated && _children.Value.Any(c => c.State == TransactionState.Inflight))
                    {
                        throw new InvalidOperationException($"Unable to finish transaction or step '{Name}', category '{Category}', because some of its child steps haven't finished.");
                    }

                    Result = result;
                    State = TransactionState.Failure;
                    VersionFinished = IncrementVersion();
                    _stopwatch.Stop();

                    TimingStore.SetCurrentTiming(Parent);

                    _profiler.RegisterEvent(GetFinishEvent(Version), this);
                }
            }
        }

        public abstract TransactionSnapshot GetTransactionSnapshot();
        public abstract TimingType Type { get; }
    }
}
