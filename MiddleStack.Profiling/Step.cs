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
    internal class Step: StepBase
    {
        public Step(LiveProfiler profiler, string category, string name, string template, StepBase parent) : base(profiler, category, name, template, parent)
        {
            var stepParent = parent as Step;
            RelativeStart = (stepParent?.RelativeStart ?? TimeSpan.Zero) + (parent?.Elapsed ?? TimeSpan.Zero);
        }

        public override object SyncRoot => Transaction.SyncRoot;
        public override int Version => Transaction.Version;
        public override int IncrementVersion()
        {
            return Transaction.IncrementVersion();
        }

        public Transaction Transaction => Parent as Transaction ?? (Parent as Step)?.Transaction;

        public TimeSpan RelativeStart { get; }
        public override bool IsTransactionFinished => Transaction.IsFinished;
        protected override SnapshotBase NewSnapshot()
        {
            return new StepSnapshot();
        }

        protected override SnapshotBase GetSnapshot(int? version)
        {
            var snapshot = (StepSnapshot)base.GetSnapshot(version);

            if (snapshot != null)
            {
                snapshot.RelativeStart = RelativeStart;
            }

            return snapshot;
        }

        protected override IProfilerEvent GetFinishEvent(int version)
        {
            return new StepFinishEvent(this, version);
        }

    }
}
