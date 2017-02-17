using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    internal class ProfilerContext: IProfilerContext
    {
        public ProfilerContext(ITimingInfo currentTiming)
        {
            CurrentTiming = currentTiming;
        }

        public ITimingInfo CurrentTiming { get; }
    }
}
