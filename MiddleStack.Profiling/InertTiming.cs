using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    internal class InertTiming: ITiming
    {
        public void Dispose()
        {
        }

        public void Success(object result = null)
        {
        }

        public void Failure(object result = null)
        {
        }

        public TransactionSnapshot GetTransactionSnapshot()
        {
            throw new InvalidOperationException("There's no transaction in the current context.");
        }

        public TransactionState State { get; } = TransactionState.Success;
    }
}
