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
        private const string CurrentStepKey = "AsyncProfiler.CurrentStep";

        public static void SetCurrentStep(Step step)
        {
            CallContext.LogicalSetData(CurrentStepKey, step);
        }

        public static Step GetCurrentStep()
        {
            return CallContext.LogicalGetData(CurrentStepKey) as Step;
        }
    }
}
