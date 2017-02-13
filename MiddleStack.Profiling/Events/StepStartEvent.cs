using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    internal class StepStartEvent: ProfilerEventBase, IStepStartEvent
    {
        public StepStartEvent(Step step, int version): base(step, version)
        {
            ParentId = step.Parent.Id;
            RelativeStart = step.RelativeStart;
        }

        public Guid ParentId { get; }
        public TimeSpan RelativeStart { get; }
        public override ProfilerEventType Type => ProfilerEventType.StepStart;
    }
}
