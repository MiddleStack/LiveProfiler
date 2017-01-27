using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    /// <summary>
    ///     An event that is fired when a transaction starts.
    /// </summary>
    public interface ITransactionStartEvent: IProfilerEvent
    {
        /// <summary>
        ///     Gets the identifier that can associate this transaction with other transactions.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> that provides the correlation Id, <see langword="null"/> if unspecified.
        /// </value>
        string CorrelationId { get;  }
    }
}
