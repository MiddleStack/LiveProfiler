using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Represents a profiled transaction. When this object is disposed, 
    ///     the transaction, along with all of its outstanding steps, is marked as complete.
    /// </summary>
    public interface ITransaction: IStep
    {
        /// <summary>
        ///     Gets the snap shot of the transaction state.
        /// </summary>
        /// <returns>
        ///     A <see cref="TransactionSnapshot"/> object of the transaction state.
        ///     Never <see langword="null"/>.
        /// </returns>
        TransactionSnapshot GetTransactionSnapshot();
    }
}
