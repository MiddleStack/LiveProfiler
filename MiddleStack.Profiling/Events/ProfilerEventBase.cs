using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    internal class ProfilerEventBase: IProfilerEvent
    {
        private readonly StepBase _step;
        private readonly int _version;

        protected ProfilerEventBase(StepBase step, int version)
        {
            _step = step;
            _version = version;
            Id = step.Id;
        }

        public Guid Id { get; }
        public TransactionSnapshot GetTransactionSnapshot()
        {
            var transaction = _step as Transaction;
            var step = _step as Step;

            return (transaction ?? step.Transaction).GetTransactionSnapshot(_version);
        }
    }
}
