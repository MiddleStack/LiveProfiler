#if !NET45
using System;
using System.Threading;

namespace MiddleStack.Profiling
{
    internal static class TimingStore
    {
        private static readonly AsyncLocal<Timing> CurrentTiming = new AsyncLocal<Timing>();

        public static void SetCurrentTiming(Timing timing)
        {
            if (timing == null) throw new ArgumentNullException(nameof(timing));

            CurrentTiming.Value = timing;
        }

        public static void DeleteTiming(Timing timing)
        {
            if (CurrentTiming.Value == timing)
            {
                CurrentTiming.Value = null;
            }
        }

        public static void TestReset()
        {
            CurrentTiming.Value = null;
        }

        public static Timing GetCurrentTiming()
        {
            return CurrentTiming.Value;
        }
    }
}
#endif