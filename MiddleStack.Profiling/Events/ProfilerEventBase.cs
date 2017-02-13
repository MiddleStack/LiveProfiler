using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    internal abstract class ProfilerEventBase: IProfilerEvent
    {
        private readonly Timing _step;
        private readonly int _version;

        protected ProfilerEventBase(Timing step, int version)
        {
            _step = step;
            _version = version;
            Id = step.Id;
            Category = step.Category;
            Parameters = step.Parameters;
            Name = step.Name;
            DisplayName = step.DisplayName;
            Start = step.Start;
        }
        public Guid Id { get; }
        public string Category { get; }
        public object Parameters { get; }
        public string Name { get; }
        public DateTimeOffset Start { get; }
        public TransactionSnapshot GetTransactionSnapshot()
        {
            var transaction = _step as Transaction;
            var step = _step as Step;

            return (transaction ?? step.Transaction).GetTransactionSnapshot(_version);
        }

        public string DisplayName { get; }
        public abstract ProfilerEventType Type { get; }
    }
}
