using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Provides enumerated values indicating how to create a new transaction.
    /// </summary>
    public enum TransactionMode
    {
        /// <summary>
        ///     Create a new transaction if there's no inflight transaction.
        ///     If there is an inflight transaction, throw an <see cref="InvalidOperationException"/>.
        /// </summary>
        New = 0,
        /// <summary>
        ///     Create a new transaction if there is no inflight transaction.
        ///     If there is an inflight transaction, replace it with a new transaction.
        /// </summary>
        Replace = 1,
        /// <summary>
        ///     Create a new transaction if there is no inflight transaction.
        ///     If there is a transaction, create a step under it instead.
        /// </summary>
        StepOrTransaction = 2
    }
}
