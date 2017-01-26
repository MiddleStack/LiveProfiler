using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Represents snapshot of the state of a profiled a step within a transaction.
    /// </summary>
    public class StepSnapshot: SnapshotBase
    {
        /// <summary>
        ///     Gets or sets the time at which this step was started, relative to the 
        ///     starting time of the entire transaction. If this is a transaction rather than 
        ///     a step, this value is always <see cref="TimeSpan.Zero"/>.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value indicating the relative time at which this 
        ///     step was started.
        /// </value>
        public TimeSpan RelativeStart { get; set; }
    }
}
