using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace MiddleStack.Profiling.Tests
{
    internal static class StepExtensions
    {
        public static void AssertChildlessStep(this Snapshot step, string category, 
            string name, string prototype, TimeSpan start, bool isFinished, TimeSpan? duration = null)
        {
            step.Should().NotBeNull();
            step.Category.Should().Be(category);
            step.Name.Should().Be(name);
            step.Template.Should().Be(prototype);
            step.Start.Should().BeCloseTo(start, 50);
            if (duration != null)
            {
                step.Duration.Should().BeCloseTo(duration.Value, 50);
            }
            else
            {
                step.Duration.Should().BeNull();
            }
            step.IsFinished.Should().Be(isFinished);
            step.Steps.Should().BeNull();
        }

        public static void AssertStep(this Snapshot step, string category,
            string name, string prototype, TimeSpan start, bool isFinished, TimeSpan? duration = null,
            int? childrenCount = null,
            Action<IList<Snapshot>> childrenAssertion = null)
        {
            step.Should().NotBeNull();
            step.Category.Should().Be(category);
            step.Name.Should().Be(name);
            step.Template.Should().Be(prototype);
            step.Start.Should().BeCloseTo(start, 50);
            if (duration != null)
            {
                step.Duration.Should().BeCloseTo(duration.Value, 50);
            }
            else
            {
                step.Duration.Should().BeNull();
            }
            step.IsFinished.Should().Be(isFinished);

            if (childrenCount != null)
            {
                step.Steps.Should().NotBeNull();
                step.Steps.Length.Should().Be(childrenCount);
            }

            childrenAssertion?.Invoke(step.Steps);
        }
    }
}
