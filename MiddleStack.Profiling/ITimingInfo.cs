using System;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Provides real-time information about a transaction or step.
    ///     Derivatives are <see cref="ITransactionInfo"/> and <see cref="IStepInfo"/>.
    /// </summary>
    public interface ITimingInfo
    {
        /// <summary>
        ///     Gets whether this timing is a transaction or a step.
        /// </summary>
        /// <value>
        ///     A <see cref="TimingType"/> value.
        /// </value>
        TimingType Type { get; }
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
        ///     Gets the name of this transaction. The name
        ///     does not contain any instance-specific values, such as entity ids. Such values
        ///     are found in the <see cref="Parameters"/> object.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> providing the name of this transaction or step.
        /// </value>
        string Name { get; }
        /// <summary>
        ///     Gets the display name of this transaction. This name is distinguished
        ///     from <see cref="Name"/> in that it may contain some instance specific values 
        ///     from <see cref="Parameters"/>.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> providing the name of this transaction or step.
        /// </value>
        string DisplayName { get; }
        /// <summary>
        ///     Gets the parameters with which this transaction or step was 
        ///     initialized.
        /// </summary>
        /// <value>
        ///     A simple object encapsulating the parameters of this transaction or step.
        /// </value>
        object Parameters { get; }
        /// <summary>
        ///     Gets the result with which this transaction or step finished.
        /// </summary>
        /// <value>
        ///     A simple object encapsulating the result of this transaction or step.
        /// </value>
        object Result { get; }
        /// <summary>
        ///     Gets the absolute date/time at which this step was started.
        /// </summary>
        /// <value>
        ///     A <see cref="DateTimeOffset"/> value indicating the absolute date/time at which this 
        ///     step was started.
        /// </value>
        DateTimeOffset Start { get; }
        /// <summary>
        ///     Gets the duration of this transaction or step, whether or not it's finished executing,
        ///     inclusive of the duration of child steps. For the time consumed by this step's own code,
        ///     use <see cref="OwnDuration"/>.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value providing the duration of this transaction/step.
        /// </value>
        TimeSpan Duration { get; }
        /// <summary>
        ///     Gets the duration of this transaction or step, whether or not it's finished executing,
        ///     exclusive of the duration of the child steps.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value providing the time consumed by own code of this transaction/step
        ///     exclusive of child stepss.
        /// </value>
        TimeSpan OwnDuration { get; }
        /// <summary>
        ///     Gets the state of this transaction or state.
        /// </summary>
        /// <value>
        ///     A <see cref="TransactionState"/> value.
        /// </value>
        TransactionState State { get; }
    }
}