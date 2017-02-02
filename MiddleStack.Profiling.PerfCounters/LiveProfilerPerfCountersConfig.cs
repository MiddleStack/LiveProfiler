using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.PerfCounters
{
    /// <summary>
    ///     Configuration parameters for <see cref="LiveProfilerPerfCounters"/>.
    /// </summary>
    public class LiveProfilerPerfCountersConfig
    {
        /// <summary>
        ///     Gets or sets the mode in which transaction metrics are turned into performance counters.
        /// </summary>
        /// <value>
        ///     A <see cref="PerfCounterMode"/> value. <see cref="PerfCounterMode.CategoriesAndNames"/>
        ///     by default.
        /// </value>
        public PerfCounterMode? TransactionsMode { get; set; } = PerfCounterMode.CategoriesAndNames;
        /// <summary>
        ///     Gets or sets the mode in which step metrics are turned into performance counters.
        /// </summary>
        /// <value>
        ///     A <see cref="PerfCounterMode"/> value. <see cref="PerfCounterMode.CategoriesAndNames"/>
        ///     by default.
        /// </value>
        public PerfCounterMode? StepsMode { get; set; } = PerfCounterMode.CategoriesAndNames;
    }
}
