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
        public static void AssertChildlessStep(this SnapshotBase step, string category, string name, string displayName, object parameters, 
            TimeSpan start, TransactionState state, TimeSpan? duration = null, string correlationId = null, object result = null)
        {
            step.Should().NotBeNull();
            step.Category.Should().Be(category);
            step.Name.Should().Be(name);
            step.DisplayName.Should().Be(displayName);
            step.Parameters.Should().Be(parameters);
            step.Result.Should().Be(result);
            (step as TransactionSnapshot)?.CorrelationId.Should().Be(correlationId);

            ((step as StepSnapshot)?.RelativeStart ?? TimeSpan.Zero).Should().BeCloseTo(start, 50);
            if (duration != null)
            {
                step.Duration.Should().BeCloseTo(duration.Value, 50);
            }
            step.State.Should().Be(state);
            step.Steps.Should().BeNull();
        }

        public static void AssertStep(this SnapshotBase step, string category, string name, string displayName, object parameters,
            TimeSpan start, TransactionState state, TimeSpan? duration = null, int? childrenCount = null, 
            Action<IList<StepSnapshot>> childrenAssertion = null, object result = null)
        {
            step.Should().NotBeNull();
            step.Category.Should().Be(category);
            step.Name.Should().Be(name);
            step.DisplayName.Should().Be(displayName);
            step.Parameters.Should().Be(parameters);
            step.Result.Should().Be(result);
            ((step as StepSnapshot)?.RelativeStart ?? TimeSpan.Zero).Should().BeCloseTo(start, 50);
            if (duration != null)
            {
                step.Duration.Should().BeCloseTo(duration.Value, 50);
            }
            step.State.Should().Be(state);

            if (childrenCount != null)
            {
                step.Steps.Should().NotBeNull();
                step.Steps.Length.Should().Be(childrenCount);
            }

            childrenAssertion?.Invoke(step.Steps);
        }
    }
}
