using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Indicates the outcome of a transaction or step.
    /// </summary>
    public enum TransactionState
    {
        /// <summary>
        ///     The transaction or step is still inflight.
        /// </summary>
        Inflight,
        /// <summary>
        ///     The transaction or step completed successfully.
        /// </summary>
        Success,
        /// <summary>
        ///     The transaction or step completed with failure.
        /// </summary>
        Failure
    }
}
