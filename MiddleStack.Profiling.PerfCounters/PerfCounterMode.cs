using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.PerfCounters
{
    /// <summary>
    ///     Provides enumerated values indicating which data is recorded
    ///     in performance counters.
    /// </summary>
    public enum PerfCounterMode
    {
        /// <summary>
        ///     Do not update performance counters.
        /// </summary>
        None,
        /// <summary>
        ///     Record only transaction/step categories as performance counter instances,
        ///     one category per instance.
        /// </summary>
        CategoriesOnly,
        /// <summary>
        ///     Record only transaction/step names (in the form of [Category]-[Name])
        ///     as performance counter instances, one category/name combination per instance.
        /// </summary>
        CategoriesAndNames
    }
}
