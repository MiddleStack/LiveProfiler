using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiddleStack.Profiling.Events;

namespace MiddleStack.Profiling
{
    internal class Step: IStep
    {
        private readonly AsyncProfiler _profiler;
        private int _version;
        private readonly object _syncRoot = new object();
        private Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Lazy<List<Step>> _children = new Lazy<List<Step>>(() => new List<Step>());

        public Step(AsyncProfiler profiler, string category, string name, string template, bool forceNewTransaction)
        {
            _profiler = profiler;
            var parent = forceNewTransaction ? null : CallContextHelper.GetCurrentStep();

            if (parent != null)
            {
                if (parent.Transaction.IsFinished)
                {
                    // Start a new root step.
                    parent = null;
                }
            }

            Id = Guid.NewGuid();
            Category = category;
            Name = name;
            Template = template;
            Parent = parent;
            Transaction = parent?.Transaction ?? this;
            Start = parent != null ? (parent.Start + parent._stopwatch.Elapsed) : TimeSpan.Zero;

            lock (Transaction._syncRoot)
            {
                if (parent != null)
                {
                    if (parent.IsFinished)
                    {
                        throw new InvalidOperationException("Unable to add a child step to this step, because the latter is already finished.");
                    }

                    VersionStarted = IncrementVersion();
                    parent._children.Value.Add(this);
                }

                CallContextHelper.SetCurrentStep(this);

                profiler.RegisterEvent(new StepEvent(this, Transaction._version, StepEventType.Started), this);
            }
        }

        public Guid Id { get; }
        public string Category { get; }
        public string Template { get; }
        public string Name { get; }
        public TimeSpan Start { get; }

        public TimeSpan? Duration { get; private set; }

        public bool IsFinished => VersionFinished != null;

        public Step Parent { get; }
        public Step Transaction { get; }
        public int VersionStarted { get; }
        public int? VersionFinished { get; private set; }

        public void Dispose()
        {
            lock (Transaction._syncRoot)
            {
                if (!IsFinished)
                {
                    if (_children.IsValueCreated && _children.Value.Any(c => !c.IsFinished))
                    {
                        throw new InvalidOperationException($"Unable to finish step {Name} because some of its child steps haven't finished.");
                    }

                    VersionFinished = IncrementVersion();
                    _stopwatch.Stop();
                    Duration = _stopwatch.Elapsed;
                    _stopwatch = null;

                    CallContextHelper.SetCurrentStep(Parent);

                    _profiler.RegisterEvent(new StepEvent(this, Transaction._version, StepEventType.Finished), this);
                }
            }
        }

        private int IncrementVersion()
        {
            return Interlocked.Increment(ref Transaction._version);
        }

        internal Snapshot GetSnapshot(int? version)
        {
            if (version != null && VersionStarted > version) return null;

            var snapshot = new Snapshot
            {
                Id = Id,
                Category = Category,
                Name = Name,
                Template = Template,
                Start = Start
            };

            if (version == null || VersionFinished <= version)
            {
                snapshot.Duration = Duration;
                snapshot.IsFinished = IsFinished;
            }

            if (_children.IsValueCreated)
            {
                snapshot.Steps = _children.Value
                    .Select(child => child.GetSnapshot(version))
                    .Where(s => s != null).ToArray();
            }

            return snapshot;
        }

        public Snapshot GetTransactionSnapshot()
        {
            return Transaction.GetSnapshot(null);
        }
    }
}
