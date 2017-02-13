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
            IsSuccess = step.State == TransactionState.Success;
            Result = step.Result;
        }

        public TimeSpan Duration { get; }
        public bool IsSuccess { get; }
        public object Result { get; set; }
        public override ProfilerEventType Type => ProfilerEventType.StepFinish;
    }
}
