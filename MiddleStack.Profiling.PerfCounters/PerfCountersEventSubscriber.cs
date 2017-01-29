using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiddleStack.Profiling.Events;

namespace MiddleStack.Profiling.PerfCounters
{
    /// <summary>
    ///     A LiveProfiler event handler that records the completion of transactions
    ///     and steps in performance counters.
    /// </summary>
    /// <remarks>
    ///     <para>Consumers must invoke <see cref="Install"/> to install the performance counter categories
    ///     prior to using this event handler.</para>
    ///     <para>Two performance counter categories are installed:</para>
    ///     <list type="bullet">
    ///         <listheader>
    ///             <term>Category</term>
    ///             <description>Description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>LiveProfiler Transactions</term>
    ///             <description>
    ///                 <para>The name of this category can be customized. The category contains instances, 
    ///                 corresponding to both the transaction categories, as well as the category + name combination.
    ///                 By default, the name-specific instances are limited to 100 to limit memory use.</para>
    ///                 <para>The following counters are present:</para>
    ///                 <list type="bullet">
    ///                     <listheader>
    ///                     </listheader>
    ///                     <item>Completed/Sec</item>
    ///                     <item>Successful/Sec</item>
    ///                     <item>Failed/Sec</item>
    ///                     <item>Total</item>
    ///                 </list>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>LiveProfiler Steps</term>
    ///             <description>
    ///                 <para>The name of this category can be customized. The category contains instances, 
    ///                 corresponding to both the step categories, as well as the category + name combination.
    ///                 By default, the name-specific instances are disabled, and only categories are recorded.</para>
    ///                 <para>The following counters are present:</para>
    ///                 <list type="bullet">
    ///                     <listheader>
    ///                     </listheader>
    ///                     <item>Completed/Sec</item>
    ///                     <item>Successful/Sec</item>
    ///                     <item>Failed/Sec</item>
    ///                     <item>Total</item>
    ///                 </list>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public class PerfCountersEventSubscriber: IProfilerEventSubscriber
    {
        private readonly bool _recordTransactions = true;
        private readonly int _maxTransactionNames = 100;
        private readonly bool _recordSteps = false;
        private readonly int _maxStepNames = 0;
        private readonly string _transactionPerfCounterCategory = "LiveProfiler Transactions";
        private readonly string _stepPerfCounterCategory = "LiveProfiler Steps";

        public void HandleEvent(IProfilerEvent stepEvent)
        {
            throw new NotImplementedException();
        }

        public void Install()
        {
        }

        public void Uninstall()
        {
        }
    }
}
