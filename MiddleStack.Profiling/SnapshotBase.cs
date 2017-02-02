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
        ///     Gets or sets the name of this transaction. The name
        ///     does not contain any instance-specific values, such as entity ids. Such values
        ///     are found in the <see cref="Parameters"/> object.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> providing the name of this transaction or step.
        /// </value>
        public string Name { get; set; }
        /// <summary>
        ///     Gets or sets the display name of this transaction. This name is distinguished
        ///     from <see cref="Name"/> in that it may contain some instance specific values 
        ///     from <see cref="Parameters"/>.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> providing the name of this transaction or step.
        /// </value>
        public string DisplayName { get; set; }
        /// <summary>
        ///     Gets or sets the parameters with which this transaction or step was 
        ///     initialized.
        /// </summary>
        /// <value>
        ///     A simple object encapsulating the parameters of this transaction or step.
        /// </value>
        public object Parameters { get; set; }
        /// <summary>
        ///     Gets or sets the result with which this transaction or step finished.
        /// </summary>
        /// <value>
        ///     A simple object encapsulating the result of this transaction or step.
        /// </value>
        public object Result { get; set; }
        /// <summary>
        ///     Gets or sets the absolute date/time at which this step was started.
        /// </summary>
        /// <value>
        ///     A <see cref="DateTimeOffset"/> value indicating the absolute date/time at which this 
        ///     step was started.
        /// </value>
        public DateTimeOffset Start { get; set; }
        /// <summary>
        ///     Gets or sets the duration of this transaction or step, whether or not it's finished executing,
        ///     inclusive of the duration of child steps. For the time consumed by this step's own code,
        ///     use <see cref="OwnDuration"/>.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value providing the duration of this transaction/step.
        /// </value>
        public TimeSpan Duration { get; set; }
        /// <summary>
        ///     Gets or sets the duration of this transaction or step, whether or not it's finished executing,
        ///     exclusive of the duration of the child steps.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value providing the time consumed by own code of this transaction/step
        ///     exclusive of child stepss.
        /// </value>
        public TimeSpan OwnDuration { get; set; }
        /// <summary>
        ///     Gets or sets the state of this transaction or state.
        /// </summary>
        /// <value>
        ///     A <see cref="TransactionState"/> value.
        /// </value>
        public TransactionState State { get; set; }

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
