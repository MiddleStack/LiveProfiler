using System;
using System.Runtime.InteropServices;

namespace MiddleStack.Profiling.Testing
{
    /// <summary>
    ///     Extension methods for <see cref="ILiveProfiler"/> that are exclusively for use in unit and integration tests.
    /// </summary>
    public static class LiveProfilerTestingExtensions
    {
        /// <summary>
        ///     Clears all transactions, both inflight and recent, from the specified <see cref="ILiveProfiler"/>
        ///     instance. For use in tests only.
        /// </summary>
        /// <param name="profiler">
        ///     The profiler to reset. 
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="profiler"/> is <see langword="null"/>.
        /// </exception>
        public static void TestingReset(this ILiveProfiler profiler)
        {
            if (profiler == null) throw new ArgumentNullException(nameof(profiler));

            var impl = profiler as LiveProfiler;
            impl?.TestingReset();
        }
    }
}
