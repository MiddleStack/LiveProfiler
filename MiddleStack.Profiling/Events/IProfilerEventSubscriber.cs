using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    /// <summary>
    ///     Implement this interface to handle step events raised by <see cref="ILiveProfiler"/>.
    ///     Use <see cref="IProfilerEventSubscriberAsync"/> if the handler logic contains asynchronous code.
    /// </summary>
    public interface IProfilerEventSubscriber
    {
        /// <summary>
        ///     Handles an event that just occurred on a profled transaction or step.
        /// </summary>
        /// <remarks>
        ///     The handler should do its own exception handling. Any unhandled exception is swallowed.
        /// </remarks>
        /// <param name="stepEvent">
        ///     The <see cref="IProfilerEvent"/> object encapsulating the event. Never <see langword="null"/>.
        ///     This value could be one of the following types:
        ///     <list type="bullet">
        ///         <item><see cref="ITransactionStartEvent"/></item>
        ///         <item><see cref="ITransactionFinishEvent"/></item>
        ///         <item><see cref="IStepStartEvent"/></item>
        ///         <item><see cref="IStepFinishEvent"/></item>
        ///     </list>
        /// </param>
        void HandleEvent(IProfilerEvent stepEvent);
    }
}
