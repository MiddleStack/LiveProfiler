using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    internal class TransactionStartEvent: ProfilerEventBase, ITransactionStartEvent
    {
        public TransactionStartEvent(Transaction transaction, int version): base(transaction, version)
        {
            CorrelationId = transaction.CorrelationId;
        }

        public string CorrelationId { get; }
    }
}
