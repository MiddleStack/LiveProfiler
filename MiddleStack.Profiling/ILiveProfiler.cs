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
        ///     <para>An identifier for the step that contains information that is unique to this particular step.
        ///         For example, if the step is an HTTP query, the name could contain specific host and entity Ids in the URL:</para>
        ///     <code>
        ///         GET http://acme.com/api/v1.0/users/123/settings/456
        ///     </code>
        /// </param>
        /// <param name="template">
        ///     <para>Optional. A template string from which <paramref name="name"/> was built, that 
        ///         doesn't have any of the step-specific information, such as entity Ids.
        ///         For example, if the step is an HTTP query, the name would be the Method + URL, but not the host name and entity Ids:</para>
        ///     <code>
        ///         GET /api/v1.0/users/[userid]/settings/[settingid]
        ///     </code>
        /// </param>
        /// <returns>
        ///     An <see cref="IStep"/> object which, when disposed, marks this step as finished.
        ///     It can also be used to access the current state of the transaction. If no step is created, 
        ///     <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para>The argument <paramref name="category"/> is <see langword="null"/> or empty.</para>
        ///     <para>-or-</para>
        ///     <para>The argument <paramref name="name"/> is <see langword="null"/> or empty.</para>
        ///     <para>-or-</para>
        ///     <para>The argument <paramref name="template"/> is specified but empty.</para>
        /// </exception>
        IStep Step(string category, string name, string template = null);

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
        ///     <para>An identifier for the transaction that contains information that is unique to this particular transaction.
        ///         For example, if the transaction is an HTTP query, the name could contain specific host and entity Ids in the URL:</para>
        ///     <code>
        ///         GET http://acme.com/api/v1.0/users/123/settings/456
        ///     </code>
        /// </param>
        /// <param name="template">
        ///     <para>Optional. A template string from which <paramref name="name"/> was built, that 
        ///         doesn't have any of the transaction-specific information, such as entity Ids.
        ///         For example, if the transaction is an HTTP query, the name would be the Method + URL, but not the host name and entity Ids:</para>
        ///     <code>
        ///         GET /api/v1.0/users/[userid]/settings/[settingid]
        ///     </code>
        /// </param>
        /// <param name="correlationId">
        ///     An identifier that can associate multiple transactions. Optional.
        /// </param>
        /// <param name="forceNew">
        ///     If <see langword="true"/>, always start a new transaction even if there is already a 
        ///     transaction in flight in the current context. Specify <see langword="true"/>
        ///     if the caller does not want a new thread to inherit the transaction started by a parent thread.
        ///     <see langword="false"/> by default.
        /// </param>
        /// <returns>
        ///     An <see cref="ITransaction"/> object which, when disposed, marks this transaction as finished.
        ///     It can also be used to access the current state of the transaction.
        ///     Never <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <para>The argument <paramref name="category"/> is <see langword="null"/> or empty.</para>
        ///     <para>-or-</para>
        ///     <para>The argument <paramref name="name"/> is <see langword="null"/> or empty.</para>
        ///     <para>-or-</para>
        ///     <para>The argument <paramref name="template"/> is specified but empty.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     There is already an inflight transaction in the current context, and <paramref name="forceNew"/>
        ///     is <see langword="false"/>.
        /// </exception>
        ITransaction NewTransaction(string category, string name, string template = null, string correlationId = null, bool forceNew = false);

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
        ///     Registers a <see cref="IProfilerEventHandler"/> to handle step events raised
        ///     by this <see cref="ILiveProfiler"/>.
        /// </summary>
        /// <param name="eventHandler"></param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="eventHandler"/> is <see langword="null"/>.
        /// </exception>
        void RegisterEventHandler(IProfilerEventHandler eventHandler);

        /// <summary>
        ///     Registers a <see cref="IProfilerEventHandlerAsync"/> to handle step events raised
        ///     by this <see cref="ILiveProfiler"/>.
        /// </summary>
        /// <param name="eventHandler">
        ///     The handler to start receiving events.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="eventHandler"/> is <see langword="null"/>.
        /// </exception>
        void RegisterEventHandler(IProfilerEventHandlerAsync eventHandler);

        /// <summary>
        ///     Unregister a previously registered <see cref="IProfilerEventHandler"/>, and cause it to stop receiving events.
        /// </summary>
        /// <param name="profilerEventHandler">
        ///     The handler to stop receiving events.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="profilerEventHandler"/> is <see langword="null"/>.
        /// </exception>
        void UnregisterEventHandler(IProfilerEventHandler profilerEventHandler);

        /// <summary>
        ///     Unregister a previously registered <see cref="IProfilerEventHandlerAsync"/>, and cause it to stop receiving events.
        /// </summary>
        /// <param name="profilerEventHandler">
        ///     The handler to stop receiving events.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="profilerEventHandler"/> is <see langword="null"/>.
        /// </exception>
        void UnregisterEventHandler(IProfilerEventHandlerAsync profilerEventHandler);
    }
}
