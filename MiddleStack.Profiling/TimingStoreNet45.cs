#if NET45
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    internal static class TimingStore
    {
        private const string CurrentTimingKey = "LiveProfiler.CurrentTiming";
        private static readonly ConcurrentDictionary<Guid, Timing> TimingsTable = new ConcurrentDictionary<Guid, Timing>();

        public static void SetCurrentTiming(Timing timing)
        {
            if (timing == null) throw new ArgumentNullException(nameof(timing));

            CallContext.LogicalSetData(CurrentTimingKey, timing.Id);
            TimingsTable[timing.Id] = timing;
        }

        public static void DeleteTiming(Timing timing)
        {
            TimingsTable.TryRemove(timing.Id, out timing);
        }

        public static void TestReset()
        {
            TimingsTable.Clear();
        }

        public static Timing GetCurrentTiming()
        {
            var id = CallContext.LogicalGetData(CurrentTimingKey) as Guid?;

            Timing timing = null;

            if (id != null)
            {
                TimingsTable.TryGetValue(id.Value, out timing);
            }

            return timing;
        }
    }
}
#endif