using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Represents snapshot of a profiled transaction or a step within a transaction,
    ///     at a particular moment in time.
    /// </summary>
    public abstract class SnapshotBase
    {
        /// <summary>
        ///     Gets or sets the unique identifier of a transaction or step.
        /// </summary>
        /// <value>
        ///     A <see cref="Guid"/> that is the unique identifier of this transaction or step.
        /// </value>
        public Guid Id { get; set; }
        /// <summary>
        ///     Gets or sets the category of the transaction or step. 
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
        public string Category { get; set; }
        /// <summary>
        ///     Gets or sets the optional template from which <see cref="Name"/> was created.
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
        public string Template { get; set; }
        /// <summary>
        ///     Gets or sets the name of this transaction.
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
        public string Name { get; set; }
        /// <summary>
        ///     Gets or sets the absolute date/time at which this step was started.
        /// </summary>
        /// <value>
        ///     A <see cref="DateTimeOffset"/> value indicating the absolute date/time at which this 
        ///     step was started.
        /// </value>
        public DateTimeOffset Start { get; set; }
        /// <summary>
        ///     Gets or sets the duration of this transaction or step, whether or not it's finished executing.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value providing the duration of this transaction/step.
        /// </value>
        public TimeSpan Duration { get; set; }
        /// <summary>
        ///     Gets or sets whether this transaction or step has finished.
        /// </summary>
        /// <value>
        ///     <see langword="true"/> if this transaction/step has finished.
        ///     <see langword="false"/> if this transaction/step is still inflight.
        /// </value>
        public bool IsFinished { get; set; }
        /// <summary>
        ///     Gets or sets the child steps of this transaction or step.
        /// </summary>
        /// <value>
        ///     An array of non-empty child steps if this transaction/step has children.
        ///     Otherwise, <see langword="null"/>.
        /// </value>
        public StepSnapshot[] Steps { get; set; }
    }
}
