﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    /// <summary>
    ///     An event that is raised when a transaction finishes.
    /// </summary>
    public interface IStepFinishEvent: IProfilerEvent
    {
        /// <summary>
        ///     Gets the duration of this transaction, if it's already finished.
        /// </summary>
        /// <value>
        ///     A <see cref="TimeSpan"/> value providing the duration of this step.
        /// </value>
        TimeSpan Duration { get; }
        /// <summary>
        ///     Gets whether this step was successful.
        /// </summary>
        /// <value>
        ///     <see langword="true"/> if this step was successful. 
        ///     <see langword="false"/> if this step failed.
        /// </value>
        bool IsSuccess { get; }
        /// <summary>
        ///     Gets the result with which this step finished.
        /// </summary>
        /// <value>
        ///     A simple object encapsulating the result of this step.
        /// </value>
        object Result { get; set; }
    }
}
