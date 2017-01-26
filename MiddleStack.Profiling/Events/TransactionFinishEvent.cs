using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    internal class TransactionFinishEvent: ProfilerEventBase, ITransactionFinishEvent
    {
        public TransactionFinishEvent(Transaction transaction, int version) : base(transaction, version)
        {
            Duration = transaction.Duration;
        }

        public TimeSpan? Duration { get; }
    }
}
