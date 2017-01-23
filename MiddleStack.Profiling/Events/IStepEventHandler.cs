using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    /// <summary>
    ///     Implement this interface to handle step events raised by <see cref="IAsyncProfiler"/>.
    ///     Use <see cref="IStepEventHandlerAsync"/> if the handler logic contains asynchronous code.
    /// </summary>
    public interface IStepEventHandler
    {
        /// <summary>
        ///     Handles an event that just occurred on a profled transaction or step.
        /// </summary>
        /// <remarks>
        ///     The handler should do its own exception handling. Any unhandled exception is swallowed.
        /// </remarks>
        /// <param name="stepEvent">
        ///     The <see cref="IStepEvent"/> object encapsulating the event. Never <see langword="null"/>.
        /// </param>
        void HandleEvent(IStepEvent stepEvent);
    }
}
