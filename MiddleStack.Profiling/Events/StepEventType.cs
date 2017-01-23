using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    /// <summary>
    ///     Provides enumerated value indicating the type of event that has occurred to a transaction or step.
    /// </summary>
    public enum StepEventType
    {
        /// <summary>
        ///     The transaction or step has started.
        /// </summary>
        Started,
        /// <summary>
        ///     The transaction or step has finished.
        /// </summary>
        Finished
    }
}
