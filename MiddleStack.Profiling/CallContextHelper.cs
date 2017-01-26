using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    internal static class CallContextHelper
    {
        private const string CurrentStepKey = "LiveProfiler.CurrentStep";

        public static void SetCurrentStep(StepBase step)
        {
            CallContext.LogicalSetData(CurrentStepKey, step);
        }

        public static StepBase GetCurrentStep()
        {
            return CallContext.LogicalGetData(CurrentStepKey) as StepBase;
        }
    }
}
