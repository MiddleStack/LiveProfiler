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
        ///     Gets the type of event this is. Each event type has a unique interface type
        ///     that derives from <see cref="IProfilerEvent"/>.
        /// </summary>
        /// <value>
        ///     A <see cref="ProfilerEventType"/> value, corresponding to the specific type of
        ///     interface this instance implements.
        /// </value>
        ProfilerEventType Type { get; }

        /// <summary>
        ///     Gets the unique identifier of a transaction or step.
        /// </summary>
        /// <value>
        ///     A <see cref="Guid"/> that is the unique identifier of this transaction or step.
        /// </value>
        Guid Id { get; }
        /// <summary>
        ///     Gets the category of the transaction or step. 
        /// </summary>
        /// <remarks>
        ///     <para>The category is a string that  
        ///     can be used to categorize many transactions/steps of the same type. For example, "MSSQL" is
        ///     the category for all Microsoft SQL Server queries, "HTTP" is the category for
        ///     all HTTP calls.</para>
        /// </remarks>
        /// <value>
        ///     A <see cref="string"/> providing the category of the step.
        /// </value>
        string Category { get; }
        /// <summary>
        ///     Gets the parameters with which this transaction or step was 
        ///     initialized.
        /// </summary>
        /// <value>
        ///     A simple object encapsulating the parameters of this transaction or step.
        /// </value>
        object Parameters { get; }
        /// <summary>
        ///     Gets the name of this transaction or step. The name
        ///     does not contain any instance-specific values, such as entity ids. Such values
        ///     are found in the <see cref="Parameters"/> object.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> providing the name of this transaction or step.
        /// </value>
        string Name { get; }
        /// <summary>
        ///     Gets the optional display name of this transaction or step. This name is distinguished
        ///     from <see cref="Name"/> in that it may contain some instance specific values 
        ///     from <see cref="Parameters"/>.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> providing the display name of this transaction or step.
        /// </value>
        string DisplayName { get; }
        /// <summary>
        ///     Gets the absolute date/time at which this transaction or step was started.
        /// </summary>
        /// <value>
        ///     A <see cref="DateTimeOffset"/> value indicating the absolute date/time at which this 
        ///     transaction or step was started.
        /// </value>
        DateTimeOffset Start { get; }
        /// <summary>
        ///     Gets a snapshot of the entire transaction, at the time of this event.
        /// </summary>
        /// <returns>
        ///     A snapshot of the transaction at the time of this event. Never <see langword="null"/>.
        /// </returns>
        TransactionSnapshot GetTransactionSnapshot();
    }
}
