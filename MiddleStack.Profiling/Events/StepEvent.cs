using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Events
{
    internal class StepEvent: IStepEvent
    {
        private readonly Step _step;
        private readonly int _version;

        public StepEvent(Step step, int version, StepEventType eventType)
        {
            _step = step;
            _version = version;
            EventType = eventType;
            IsTransaction = step.Parent == null;
            ParentId = step.Parent?.Id;
            Id = step.Id;
            Category = step.Category;
            Template = step.Template;
            Name = step.Name;
            Start = step.Start;
            Duration = step.Duration;
            IsFinished = step.IsFinished;
        }

        public StepEventType EventType { get; }
        public bool IsTransaction { get; }
        public Guid? ParentId { get; }
        public Guid Id { get; }
        public string Category { get; }
        public string Template { get; }
        public string Name { get; }
        public TimeSpan Start { get; }
        public TimeSpan? Duration { get; }
        public bool IsFinished { get; }
        public Snapshot GetTransactionSnapshot()
        {
            return _step.Transaction.GetSnapshot(_version);
        }
    }
}
