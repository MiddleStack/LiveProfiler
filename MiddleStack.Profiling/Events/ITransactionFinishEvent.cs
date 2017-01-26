using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    /// <summary>
    ///     An event that is raised when a transaction finishes.
    /// </summary>
    public interface ITransactionFinishEvent: IProfilerEvent
    {
        /// <summary>
        ///     Gets the duration of this transaction, if it's already finished.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value providing the duration of this transaction
        ///     has finished. <see langword="null"/> if it's not finished.
        /// </value>
        TimeSpan? Duration { get; }
    }
}
