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
            ParentId = step.Parent?.Id;
            Category = step.Category;
            Template = step.Template;
            Name = step.Name;
            Start = step.Start;
            RelativeStart = step.RelativeStart;
        }

        public Guid? ParentId { get; }
        public string Category { get; }
        public string Template { get; }
        public string Name { get; }
        public DateTimeOffset Start { get; }
        public TimeSpan RelativeStart { get; }
    }
}
