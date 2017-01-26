using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Represents snapshot of the state of a profiled transaction.
    /// </summary>
    public class TransactionSnapshot: SnapshotBase
    {
        /// <summary>
        ///     Gets or sets the identifier that can associate this transaction with other transactions.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> that provides the correlation Id, <see langword="null"/> if unspecified.
        /// </value>
        public string CorrelationId { get; set; }
    }
}
