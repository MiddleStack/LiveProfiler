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
            Category = transaction.Category;
            Template = transaction.Template;
            Name = transaction.Name;
            Start = transaction.Start;
            CorrelationId = transaction.CorrelationId;
        }

        public string Category { get; }
        public string Template { get; }
        public string Name { get; }
        public DateTimeOffset Start { get; }
        public string CorrelationId { get; }
    }
}
