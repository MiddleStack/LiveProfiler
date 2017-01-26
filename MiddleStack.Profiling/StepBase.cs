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
    internal abstract class StepBase: IStep
    {
        private readonly LiveProfiler _profiler;
        private Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Lazy<List<StepBase>> _children = new Lazy<List<StepBase>>(() => new List<StepBase>());

        protected StepBase(LiveProfiler profiler, string category, string name, string template, StepBase parent)
        {
            _profiler = profiler;

            Id = Guid.NewGuid();
            Category = category;
            Name = name;
            Template = template;
            Parent = parent;
            Start = DateTimeOffset.Now;

            if (parent != null)
            {
                if (parent.IsFinished)
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
        public abstract bool IsTransactionFinished { get; }
        public Guid Id { get; }
        public string Category { get; }
        public string Template { get; }
        public string Name { get; }
        public DateTimeOffset Start { get; }

        public TimeSpan Duration => _stopwatch.Elapsed;

        public bool IsFinished => VersionFinished != null;

        public StepBase Parent { get; }
        public int VersionStarted { get; }
        public int? VersionFinished { get; private set; }

        public void Dispose()
        {
            lock (SyncRoot)
            {
                if (!IsFinished)
                {
                    if (_children.IsValueCreated && _children.Value.Any(c => !c.IsFinished))
                    {
                        throw new InvalidOperationException($"Unable to finish transaction or step '{Name}', category '{Category}', template '{Template}', because some of its child steps haven't finished.");
                    }

                    VersionFinished = IncrementVersion();
                    _stopwatch.Stop();

                    CallContextHelper.SetCurrentStep(Parent);

                    _profiler.RegisterEvent(GetFinishEvent(Version), this);
                }
            }
        }

        protected abstract IProfilerEvent GetFinishEvent(int version);
        protected abstract SnapshotBase NewSnapshot();

        protected virtual SnapshotBase GetSnapshot(int? version)
        {
            if (version != null && VersionStarted > version) return null;

            var snapshot = NewSnapshot();
            snapshot.Id = Id;
            snapshot.Category = Category;
            snapshot.Name = Name;
            snapshot.Template = Template;
            snapshot.Start = Start;

            if (version == null || VersionFinished <= version)
            {
                snapshot.Duration = Duration;
                snapshot.IsFinished = IsFinished;
            }

            if (_children.IsValueCreated)
            {
                snapshot.Steps = _children.Value
                    .Select(child => (StepSnapshot)child.GetSnapshot(version))
                    .Where(s => s != null).ToArray();
            }

            return snapshot;
        }

        public TimeSpan Elapsed => _stopwatch.Elapsed;
    }
}
