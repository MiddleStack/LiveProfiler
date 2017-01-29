using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Represents a transaction or step in transaction. When this object is disposed, or when
    ///     either <see cref="Success"/> or <see cref="Failure"/> is invoked, 
    ///     the step is marked as complete, along with all of its outstanding child steps.
    /// </summary>
    public interface ITiming: IDisposable
    {
        /// <summary>
        ///     Marks this transaction/step as successfully completed, with the specified result object.
        /// </summary>
        /// <remarks>
        ///     <para>Disposing the object also markes the transaction/step as successfully completed,
        ///     only without a result object.</para>
        ///     <para>If this instance is already complete, no action is taken.</para>
        /// </remarks>
        /// <param name="result">
        ///     Optional. A simple serializable object that encapsulates the result of this transaction/step.
        ///     This object must be effectively immutable--its state must not change after this method is
        ///     called.
        /// </param>
        void Success(object result = null);

        /// <summary>
        ///     Marks this transaction/step as unsuccessfully completed, with the specified result object.
        /// </summary>
        /// <remarks>
        ///     If this instance is already complete, no action is taken.
        /// </remarks>
        /// <param name="result">
        ///     Required. A simple serializable object, or an exception object, that encapsulates the reason 
        ///     that this transaction/step failed. 
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="result"/> is <see langword="null"/>.
        /// </exception>
        void Failure(object result);

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
