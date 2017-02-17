using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Provides the present context of LiveProfiler on the current thread.
    /// </summary>
    public interface IProfilerContext
    {
        /// <summary>
        ///     Gets the inflight transaction or step in the current context.
        /// </summary>
        /// <value>
        ///     Either a <see cref="ITransactionInfo"/> or <see cref="IStepInfo"/> instance, if 
        ///     either a step or transaction is inflight in the current context. If there's
        ///     no transaction or step active, <see langword="null"/>.
        /// </value>
        ITimingInfo CurrentTiming { get; }
    }
}
