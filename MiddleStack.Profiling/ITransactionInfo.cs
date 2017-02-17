using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Provides read-only real-time information about a transaction.
    /// </summary>
    public interface ITransactionInfo: ITimingInfo
    {
        /// <summary>
        ///     Gets the identifier that can associate this transaction with other transactions.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> that provides the correlation Id, <see langword="null"/> if unspecified.
        /// </value>
        string CorrelationId { get; }
    }
}
