using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MiddleStack.Profiling.Events;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Provides async-safe profiling functionalities to calling code.
    /// </summary>
    public interface ILiveProfiler
    {
        /// <summary>
        ///     Start a new step. If there is currently an inflight step, the new step 
        ///     will be added as a child step to that step. If there is no inflight step
        ///     in the current context, nothing is done.
        /// </summary>
        /// <param name="category">
        ///     The category to which the step belongs. The step category is a string that  
        ///     can be used to categorize many steps of the same type. For example, "MSSQL" is
        ///     the category for all Microsoft SQL Server queries, "HTTP" is the category for
        ///     all HTTP calls.
        /// </param>
        /// <param name="name">
        ///     <para>An identifier for the step. It should not contain volatile data such as entity Ids,
        ///         which should be placed in <paramref name="parameters"/>. The following name for an HTTP 
        ///         call is an ideal example, in which volatile data is replaced with placeholders.</para>
        ///     <code>
        ///         GET http://acme.com/api/v1.0/users/{UserId}/settings/{SettingId}
        ///     </code>
        /// </param>
        /// <param name="displayName">
        ///     <para>Optional. An identifier for the step that, unlike <paramref name="name"/> may contain 
        ///         some or all of the volatile data from <paramref name="parameters"/>. For example:</para>
        ///     <code>
        ///         GET http://acme.com/api/v1.0/users/123/settings/456
        ///     </code>
        /// </param>
        /// <param name="parameters">
        ///     Optional. The parameters with which this step is initialized. This should be a simple object
        ///     that is JSON-serializable, and its state should not change after this call.
        /// </param>
        /// <returns>
        ///     An <see cref="ITiming"/> object which, when disposed, marks this step as finished.
        ///     It can also be used to access the current state of the transaction. If no step is created, 
        ///     <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para>The argument <paramref name="category"/> is <see langword="null"/> or empty.</para>
        ///     <para>-or-</para>
        ///     <para>The argument <paramref name="name"/> is <see langword="null"/> or empty.</para>
        /// </exception>
        ITiming Step(string category, string name, string displayName = null, object parameters = null);

        /// <summary>
        ///     Start a new transaction, if there is no inflight transaction.  If there is already an inflight exception
        ///     in the present context, an exception is thrown.
        /// </summary>
        /// <param name="category">
        ///     The category to which the transaction belongs. The transaction category is a string that  
        ///     can be used to categorize many transactions of the same type. For example, "MSSQL" is
        ///     the category for all Microsoft SQL Server queries, "HTTP" is the category for
        ///     all HTTP calls.
        /// </param>
        /// <param name="name">
        ///     <para>An identifier for the transaction. It should not contain volatile data such as entity Ids,
        ///         which should be placed in <paramref name="parameters"/>. The following name for an HTTP 
        ///         call is an ideal example, in which volatile data is replaced with placeholders.</para>
        ///     <code>
        ///         GET http://acme.com/api/v1.0/users/{UserId}/settings/{SettingId}
        ///     </code>
        /// </param>
        /// <param name="displayName">
        ///     <para>Optional. An identifier for the transaction that, unlike <paramref name="name"/> may contain 
        ///         some or all of the volatile data from <paramref name="parameters"/>. For example:</para>
        ///     <code>
        ///         GET http://acme.com/api/v1.0/users/123/settings/456
        ///     </code>
        /// </param>
        /// <param name="parameters">
        ///     Optional. The parameters with which this transaction is initialized. This should be a simple object
        ///     that is JSON-serializable, and its state should not change after this call.
        /// </param>
        /// <param name="correlationId">
        ///     An identifier that can associate multiple transactions. Optional.
        /// </param>
        /// <param name="mode">
        ///     Indicates how the transaction is to be created. The default is <see cref="TransactionMode.New"/>.
        /// </param>
        /// <returns>
        ///     An <see cref="ITiming"/> object which, when disposed, marks this transaction as finished.
        ///     It can also be used to access the current state of the transaction.
        ///     Never <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para>The argument <paramref name="category"/> is <see langword="null"/> or empty.</para>
        ///     <para>-or-</para>
        ///     <para>The argument <paramref name="name"/> is <see langword="null"/> or empty.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     There is already an inflight transaction in the current context, and <paramref name="mode"/>
        ///     is <see cref="TransactionMode.New"/>.
        /// </exception>
        ITiming Transaction(string category, string name, string displayName = null, object parameters = null, string correlationId = null, TransactionMode mode = TransactionMode.New);

        /// <summary>
        ///     Gets the snapshots of up to 100 transactions that have recently been started.
        /// </summary>
        /// <param name="inflightOnly">
        ///     Specify <see langword="true"/> to only return transactions that are still inflight.
        /// </param>
        /// <returns>
        ///     An array of <see cref="TransactionSnapshot"/> objects, each containing the state of a
        ///     transaction that has recently started. Empty if no transactions have ever 
        ///     been completed. Never <see langword="null"/>. 
        /// </returns>
        IList<TransactionSnapshot> GetRecentTransactions(bool inflightOnly = false);

        /// <summary>
        ///     Registers a <see cref="IProfilerEventSubscriber"/> to handle step events raised
        ///     by this <see cref="ILiveProfiler"/>.
        /// </summary>
        /// <param name="eventSubscriber"></param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="eventSubscriber"/> is <see langword="null"/>.
        /// </exception>
        void RegisterEventSubscriber(IProfilerEventSubscriber eventSubscriber);

        /// <summary>
        ///     Registers a <see cref="IProfilerEventSubscriberAsync"/> to handle step events raised
        ///     by this <see cref="ILiveProfiler"/>.
        /// </summary>
        /// <param name="eventSubscriber">
        ///     The handler to start receiving events.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="eventSubscriber"/> is <see langword="null"/>.
        /// </exception>
        void RegisterEventSubscriber(IProfilerEventSubscriberAsync eventSubscriber);

        /// <summary>
        ///     Unregister a previously registered <see cref="IProfilerEventSubscriber"/>, and cause it to stop receiving events.
        /// </summary>
        /// <param name="profilerEventSubscriber">
        ///     The handler to stop receiving events.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="profilerEventSubscriber"/> is <see langword="null"/>.
        /// </exception>
        void UnregisterEventSubscriber(IProfilerEventSubscriber profilerEventSubscriber);

        /// <summary>
        ///     Unregister a previously registered <see cref="IProfilerEventSubscriberAsync"/>, and cause it to stop receiving events.
        /// </summary>
        /// <param name="profilerEventSubscriber">
        ///     The handler to stop receiving events.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="profilerEventSubscriber"/> is <see langword="null"/>.
        /// </exception>
        void UnregisterEventSubscriber(IProfilerEventSubscriberAsync profilerEventSubscriber);
    }
}
