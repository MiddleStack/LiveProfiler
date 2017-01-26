using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    /// <summary>
    ///     An event that is fired when a step starts.
    /// </summary>
    public interface IStepStartEvent: IProfilerEvent
    {
        /// <summary>
        ///     Gets the name of the parent step. This colud be the Id of the transaction.
        /// </summary>
        /// <value>
        ///     A <see cref="Guid"/> that is the unique identifier for the parent step.
        ///     <see langword="null"/> if this is a step.
        /// </value>
        Guid? ParentId { get; }

        /// <summary>
        ///     Gets the category of the step. 
        /// </summary>
        /// <remarks>
        ///     <para>The step category is a string that  
        ///     can be used to categorize many steps of the same type. For example, "MSSQL" is
        ///     the category for all Microsoft SQL Server queries, "HTTP" is the category for
        ///     all HTTP calls.</para>
        /// </remarks>
        /// <value>
        ///     A <see cref="string"/> providing the category of the step.
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
        ///     if the template is not applicable to this particular type of step/step, or 
        ///     if it's not simply not recorded.
        /// </value>
        string Template { get; }
        /// <summary>
        ///     Gets the name of this step.
        /// </summary>
        /// <remarks>
        ///     <para>The name contains information that is unique to this particular step.
        ///     For example, if the step is an HTTP query, the name could contain specific host and entity Ids in the URL:</para>
        ///     <code>
        ///         GET http://acme.com/api/v1.0/users/123/settings/456
        ///     </code>
        /// </remarks>
        /// <value>
        ///     A <see cref="string"/> providing the name of this step.
        /// </value>
        string Name { get; }
        /// <summary>
        ///     Gets the absolute date/time at which this step was started.
        /// </summary>
        /// <value>
        ///     A <see cref="DateTimeOffset"/> value indicating the absolute date/time at which this 
        ///     step was started.
        /// </value>
        DateTimeOffset Start { get; }
        /// <summary>
        ///     Gets the time at which this step was started, relative to the 
        ///     starting time of the entire transaction. 
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value indicating the relative time at which this 
        ///     step was started. 
        /// </value>
        TimeSpan RelativeStart { get; }
    }
}
