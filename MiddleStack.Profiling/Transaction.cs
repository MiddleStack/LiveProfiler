using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiddleStack.Profiling.Events;

namespace MiddleStack.Profiling
{
    internal class Transaction: Timing, ITransactionInfo
    {
        private int _version;
        private static int _sequenceSeed;

        public Transaction(LiveProfiler profiler, string category, string name, string displayName, object parameters, string correlationId) 
            : base(profiler, category, name, displayName, parameters, null)
        {
            CorrelationId = correlationId;
            Sequence = Interlocked.Increment(ref _sequenceSeed);
        }

        public override TimingType Type => TimingType.Transaction;
        public int Sequence { get; }
        public string CorrelationId { get; }
        public override object SyncRoot { get; } = new object();

        public override int Version => _version;

        public override int IncrementVersion()
        {
            return Interlocked.Increment(ref _version);
        }

        public override TransactionState TransactionState => State;

        public override TransactionSnapshot GetTransactionSnapshot()
        {
            return GetTransactionSnapshot(null);
        }

        internal TransactionSnapshot GetTransactionSnapshot(int? version)
        {
            return (TransactionSnapshot)GetSnapshot(version);
        }

        protected override SnapshotBase NewSnapshot()
        {
            return new TransactionSnapshot();
        }

        protected override SnapshotBase GetSnapshot(int? version)
        {
            lock (SyncRoot)
            {
                var snapshot = base.GetSnapshot(version) as TransactionSnapshot;

                if (snapshot != null)
                {
                    snapshot.CorrelationId = CorrelationId;
                }

                return snapshot;
            }
        }

        protected override IProfilerEvent GetFinishEvent(int version)
        {
            return new TransactionFinishEvent(this, Version);
        }
    }
}
