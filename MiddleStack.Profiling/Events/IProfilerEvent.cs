using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    /// <summary>
    ///     Encapsulates an event that has occurred on a transaction or step.
    /// </summary>
    public interface IProfilerEvent
    {
        /// <summary>
        ///     Gets the unique identifier of a transaction or step.
        /// </summary>
        /// <value>
        ///     A <see cref="Guid"/> that is the unique identifier of this transaction or step.
        /// </value>
        Guid Id { get; }
        /// <summary>
        ///     Gets a snapshot of the entire transaction, at the time of this event.
        /// </summary>
        /// <returns>
        ///     A snapshot of the transaction at the time of this event. Never <see langword="null"/>.
        /// </returns>
        TransactionSnapshot GetTransactionSnapshot();
    }
}
