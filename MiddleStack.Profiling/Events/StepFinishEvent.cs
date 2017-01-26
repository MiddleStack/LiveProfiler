using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    internal class StepFinishEvent: ProfilerEventBase, IStepFinishEvent
    {
        public StepFinishEvent(Step step, int version) : base(step, version)
        {
            Duration = step.Duration;
        }

        public TimeSpan? Duration { get; }
    }
}
