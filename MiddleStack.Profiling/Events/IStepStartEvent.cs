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
        ///     Gets the name of the parent step. This could be the Id of the transaction.
        /// </summary>
        /// <value>
        ///     A <see cref="Guid"/> that is the unique identifier for the parent step.
        /// </value>
        Guid ParentId { get; }

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
