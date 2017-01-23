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
    public interface IStepEvent
    {
        /// <summary>
        ///     Gets the type of event that this instance represents.
        /// </summary>
        /// <value>
        ///     A <see cref="StepEventType"/> value providing the type of event that occurred.
        /// </value>
        StepEventType EventType { get; }

        /// <summary>
        ///     Gets whether this event relates to a transaction, rather than a child step.
        /// </summary>
        /// <value>
        ///     <see langword="true"/> if this event relates to a transaction.
        ///     <see langword="false"/> if this event relates to a child step of a transaction or step.
        /// </value>
        bool IsTransaction { get; }

        /// <summary>
        ///     Gets the name of the parent step. This colud be the Id of the transaction.
        /// </summary>
        /// <value>
        ///     A <see cref="Guid"/> that is the unique identifier for the parent transaction or step.
        ///     <see langword="null"/> if this is a transaction.
        /// </value>
        Guid? ParentId { get; }

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
        ///     <para>The step category is a string that  
        ///     can be used to categorize many steps of the same type. For example, "MSSQL" is
        ///     the category for all Microsoft SQL Server queries, "HTTP" is the category for
        ///     all HTTP calls.</para>
        /// </remarks>
        /// <value>
        ///     A <see cref="string"/> providing the category of the transaction or step.
        /// </value>
        string Category { get; }
        /// <summary>
        ///     Gets the optional template from which <see cref="Name"/> was created.
        /// </summary>
        /// <remarks>
        ///     <para>Optional. A template string from which <see cref="Name"/> was built, that 
        ///     doesn't have any of the step-specific information, such as entity Ids.
        ///     For example, if the step is an HTTP query, the name would be the Method + URL, but not the host name and entity Ids:</para>
        ///     <code>
        ///         GET /api/v1.0/users/[userid]/settings/[settingid]
        ///     </code>
        /// </remarks>
        /// <value>
        ///     A <see cref="string"/> providing the template of this snapshot. Could be <see langword="null"/>
        ///     if the template is not applicable to this particular type of transaction/step, or 
        ///     if it's not simply not recorded.
        /// </value>
        string Template { get; }
        /// <summary>
        ///     Gets the name of this transaction.
        /// </summary>
        /// <remarks>
        ///     <para>The name contains information that is unique to this particular step.
        ///     For example, if the step is an HTTP query, the name could contain specific host and entity Ids in the URL:</para>
        ///     <code>
        ///         GET http://acme.com/api/v1.0/users/123/settings/456
        ///     </code>
        /// </remarks>
        /// <value>
        ///     A <see cref="string"/> providing the name of this transaction.
        /// </value>
        string Name { get; }
        /// <summary>
        ///     Gets the time at which this step was started, relative to the 
        ///     starting time of the entire transaction. If this is a transaction rather than 
        ///     a step, this value is always <see cref="TimeSpan.Zero"/>.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value indicating the relative time at which this 
        ///     step was started. Always <see cref="TimeSpan.Zero"/> if this is a transaction.
        /// </value>
        TimeSpan Start { get; }
        /// <summary>
        ///     Gets the duration of this transaction or step, if it's already finished.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value providing the duration of this transaction/step
        ///     has finished. <see langword="null"/> if it's not finished.
        /// </value>
        TimeSpan? Duration { get; }
        /// <summary>
        ///     Gets whether this transaction or step has finished.
        /// </summary>
        /// <value>
        ///     <see langword="true"/> if this transaction/step has finished.
        ///     <see langword="false"/> if this transaction/step is still inflight.
        /// </value>
        bool IsFinished { get; }
        /// <summary>
        ///     Gets a snapshot of the entire transaction, at the time of this event.
        /// </summary>
        /// <returns>
        ///     A snapshot of the transaction at the time of this event. Never <see langword="null"/>.
        /// </returns>
        Snapshot GetTransactionSnapshot();
    }
}
