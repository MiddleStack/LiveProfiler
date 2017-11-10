using System;
using System.Threading;

namespace MiddleStack.Profiling
{
    internal static class TimingStore
    {
        private static readonly AsyncLocal<Timing> CurrentTiming = new AsyncLocal<Timing>();

        public static void SetCurrentTiming(Timing timing)
        {
            CurrentTiming.Value = timing;
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