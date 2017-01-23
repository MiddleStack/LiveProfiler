using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Represents a step in a profiled transaction. When this object is disposed, 
    ///     the step is marked as complete.
    /// </summary>
    public interface IStep: IDisposable
    {
        /// <summary>
        ///     Gets the snap shot of the transaction to which this step belongs.
        /// </summary>
        /// <returns>
        ///     A <see cref="Snapshot"/> object of the transaction to which this step belongs.
        ///     Never <see langword="null"/>.
        /// </returns>
        Snapshot GetTransactionSnapshot();
    }
}
