using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    /// <summary>
    ///     Provides enumerated values indicating the type of profiler event 
    ///     being raised.
    /// </summary>
    public enum ProfilerEventType
    {
        /// <summary>
        ///     A transaction has started. The event's type is <see cref="ITransactionStartEvent"/>.
        /// </summary>
        TransactionStart = 1,
        /// <summary>
        ///     A transaction has finished. The event's type is <see cref="ITransactionFinishEvent"/>.
        /// </summary>
        TransactionFinish = 2,
        /// <summary>
        ///     A step has started. The event's type is <see cref="IStepStartEvent"/>.
        /// </summary>
        StepStart = 3,
        /// <summary>
        ///     A step has finished. The event's type is <see cref="IStepFinishEvent"/>.
        /// </summary>
        StepFinish = 4
    }
}
