using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Provides read-only real-time information about a step.
    /// </summary>
    public interface IStepInfo: ITimingInfo
    {
        /// <summary>
        ///     Gets the time at which this step was started, relative to the 
        ///     starting time of the entire transaction. If this is a transaction rather than 
        ///     a step, this value is always <see cref="TimeSpan.Zero"/>.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value indicating the relative time at which this 
        ///     step was started.
        /// </value>
        TimeSpan RelativeStart { get; }
    }
}
