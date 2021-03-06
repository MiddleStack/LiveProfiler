﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiddleStack.Profiling.Events;

namespace MiddleStack.Profiling
{
    internal class Step: Timing, IStepInfo
    {
        public Step(LiveProfiler profiler, string category, string name, string displayName, object parameters, Timing parent) 
            : base(profiler, category, name, displayName, parameters, parent)
        {
            var stepParent = parent as Step;
            RelativeStart = (stepParent?.RelativeStart ?? TimeSpan.Zero) + (parent?.Elapsed ?? TimeSpan.Zero);
        }

        public override TimingType Type => TimingType.Step;
        public override object SyncRoot => Transaction.SyncRoot;
        public override int Version => Transaction.Version;
        public override int IncrementVersion()
        {
            return Transaction.IncrementVersion();
        }

        public Transaction Transaction => Parent as Transaction ?? (Parent as Step)?.Transaction;

        public TimeSpan RelativeStart { get; }
        public override TransactionState TransactionState => Transaction.State;
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

        public override TransactionSnapshot GetTransactionSnapshot()
        {
            return Transaction.GetTransactionSnapshot();
        }
    }
}
