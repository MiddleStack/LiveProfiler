using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    /// <summary>
    ///     Implement this interface to handle step events raised by <see cref="ILiveProfiler"/>.
    ///     Use <see cref="IProfilerEventSubscriber"/> if the handler logic contains synchronous code.
    /// </summary>
    public interface IProfilerEventSubscriberAsync
    {
        /// <summary>
        ///     Handles an event that just occurred on a profled transaction or step.
        ///     The events are guaranteed to come in the correct order, and this method will only be
        ///     called serially.
        /// </summary>
        /// <remarks>
        ///     The handler should do its own exception handling. Any unhandled exception is swallowed.
        /// </remarks>
        /// <param name="stepEvent">
        ///     The <see cref="IProfilerEvent"/> object encapsulating the event. Never <see langword="null"/>.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> that completes when this method completes.
        ///     Never <see langword="null"/>.
        /// </returns>
        Task HandleEventAsync(IProfilerEvent stepEvent);

        /// <summary>
        ///     A method that is invoked when LiveProfiler is ready to dispatch events to this instance.
        /// </summary>
        void Start();

        /// <summary>
        ///     A method that is invoked when LiveProfiler has stopped sending events to this instance,
        ///     such as when this instance has been detached from LiveProfiler by calling 
        ///     <see cref="ILiveProfiler.UnregisterEventSubscriber(IProfilerEventSubscriberAsync)"/>.
        /// </summary>
        void Stop();
    }
}
