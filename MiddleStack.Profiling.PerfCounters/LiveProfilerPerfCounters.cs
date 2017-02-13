using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    ///     prior to using this event handler. If the categories are already installed, calling this method 
    ///     reinstalls the categories if the existing categories do not have the correct number and types
    ///     of counters.</para>
    ///     <para>Two performance counter categories are installed:</para>
    ///     <list type="table">
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
    ///                     <item>Avg. Duration</item>
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
    ///                     <item>Avg. Duration</item>
    ///                     <item>Total</item>
    ///                 </list>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public class LiveProfilerPerfCounters: IProfilerEventSubscriber
    {
        private const string DefaultTransactionsCategoryName = "LiveProfiler Transactions";
        private const string TransactionsCategoryDescription =
            "Performance counters relating to transactions profiled by LiveProfiler.";
        private const string DefaultStepsCategoryName = "LiveProfiler Steps";
        private const string StepsCategoryDescription =
            "Performance counters relating to steps profiled by LiveProfiler.";

        private readonly PerfCounterMode _transactionsMode = PerfCounterMode.CategoriesOnly;
        private readonly PerfCounterMode _stepsMode = PerfCounterMode.None;
        private readonly string _transactionsPerfCounterCategoryName;
        private readonly string _stepsPerfCounterCategoryName;
        private static readonly LiveProfilerPerfCountersConfig DefaultConfig = new LiveProfilerPerfCountersConfig();
        private readonly PerfCounter _transactionsCompletedPerSec, _transactionsSuccessfulPerSec, _transactionsFailedPerSec, _transactionsAvgDuration, _transactionsTotal;
        private readonly PerfCounter _stepsCompletedPerSec, _stepsSuccessfulPerSec, _stepsFailedPerSec, _stepsAvgDuration, _stepsTotal;
        private readonly bool _transactionsCategoryInstalled;
        private readonly bool _stepsCategoryInstalled;

        /// <summary>
        ///     Initializes a new instance of <see cref="LiveProfilerPerfCounters"/>.
        /// </summary>
        /// <param name="transactionsPerfCountersCategoryName">
        ///     Optional. The custom name for the performance counter category under which
        ///     transactions-specific metrics are recorded. If unspecified, the default name of 
        ///     "LiveProfiler Transactions" is used.
        /// </param>
        /// <param name="stepsPerfCountersCategoryName">
        ///     Optional. The custom name for the performance counter category under which
        ///     steps-specific metrics are recorded. If unspecified, the default name of 
        ///     "LiveProfiler Steps" is used.
        /// </param>
        /// <param name="config">
        ///     Optional. The configuration parameters to use when writing to perf counters.
        ///     If unspecified, the default values are used. 
        /// </param>
        public LiveProfilerPerfCounters(
            string transactionsPerfCountersCategoryName = null,
            string stepsPerfCountersCategoryName = null,
            LiveProfilerPerfCountersConfig config = null)
        {
            _transactionsMode = config?.TransactionsMode ?? DefaultConfig.TransactionsMode.Value;
            _stepsMode = config?.StepsMode ?? DefaultConfig.StepsMode.Value;
            _transactionsPerfCounterCategoryName = transactionsPerfCountersCategoryName ?? DefaultTransactionsCategoryName;
            _stepsPerfCounterCategoryName = stepsPerfCountersCategoryName ?? DefaultStepsCategoryName;

            _transactionsCategoryInstalled = IsCategoryInstalled(_transactionsPerfCounterCategoryName);
            _stepsCategoryInstalled = IsCategoryInstalled(_stepsPerfCounterCategoryName);

            if (_transactionsMode != PerfCounterMode.None)
            {
                CreateCounter(ref _transactionsCompletedPerSec, _transactionsPerfCounterCategoryName, "Completed/Sec",
                    "Number of transactions that completed per second, either successfully or unsuccessfully.",
                    PerformanceCounterType.RateOfCountsPerSecond32);
                CreateCounter(ref _transactionsSuccessfulPerSec, _transactionsPerfCounterCategoryName,
                    "Successful/Sec", "Number of transactions that completed successfully per second.",
                    PerformanceCounterType.RateOfCountsPerSecond32);
                CreateCounter(ref _transactionsFailedPerSec, _transactionsPerfCounterCategoryName, "Failed/Sec",
                    "Number of transactions that completed with failure per second.",
                    PerformanceCounterType.RateOfCountsPerSecond32);
                CreateCounter(ref _transactionsAvgDuration, _transactionsPerfCounterCategoryName, "Avg. Duration",
                    "The average duration of the transaction to completion.",
                    PerformanceCounterType.AverageTimer32, PerformanceCounterType.AverageBase);
                CreateCounter(ref _transactionsTotal, _transactionsPerfCounterCategoryName, "Total",
                    "The total number of transactions that have completed.",
                    PerformanceCounterType.NumberOfItems64);
            }

            if (_stepsMode != PerfCounterMode.None)
            {
                CreateCounter(ref _stepsCompletedPerSec, _stepsPerfCounterCategoryName, "Completed/Sec", 
                    "Number of steps that completed per second, either successfully or unsuccessfully.", 
                    PerformanceCounterType.RateOfCountsPerSecond32);
                CreateCounter(ref _stepsSuccessfulPerSec, _stepsPerfCounterCategoryName, 
                    "Successful/Sec", "Number of steps that completed successfully per second.", 
                    PerformanceCounterType.RateOfCountsPerSecond32);
                CreateCounter(ref _stepsFailedPerSec, _stepsPerfCounterCategoryName, "Failed/Sec", 
                    "Number of steps that completed with failure per second.",
                    PerformanceCounterType.RateOfCountsPerSecond32);
                CreateCounter(ref _stepsAvgDuration, _stepsPerfCounterCategoryName, "Avg. Duration", 
                    "The average duration of the step to completion.", 
                    PerformanceCounterType.AverageTimer32, PerformanceCounterType.AverageBase);
                CreateCounter(ref _stepsTotal, _stepsPerfCounterCategoryName, "Total",
                    "The total number of steps that have completed.",
                    PerformanceCounterType.NumberOfItems64);
            }
        }

        private void CreateCounter(ref PerfCounter counter, string categoryName, string counterName, string description,
            PerformanceCounterType counterType, PerformanceCounterType? baseCounterType = null)
        {
            counter = new PerfCounter(categoryName, counterName, description, counterType, baseCounterType);
        }

        private bool IsCategoryInstalled(string categoryName)
        {
            var categories = PerformanceCounterCategory.GetCategories().ToDictionary(c => c.CategoryName, c => c, StringComparer.InvariantCultureIgnoreCase);

            return categories.ContainsKey(categoryName);
        }

        void IProfilerEventSubscriber.HandleEvent(IProfilerEvent stepEvent)
        {
            var transactionFinishEvent = stepEvent as ITransactionFinishEvent;

            if (transactionFinishEvent != null && _transactionsCategoryInstalled && _transactionsMode != PerfCounterMode.None)
            {
                var instanceNames = _transactionsMode == PerfCounterMode.CategoriesOnly
                    ? new[] { transactionFinishEvent.Category }
                    : new [] {transactionFinishEvent.Category, $"{transactionFinishEvent.Category}-{transactionFinishEvent.Name}" };

                _transactionsCompletedPerSec.Increment(instanceNames, 1);

                if (transactionFinishEvent.IsSuccess)
                {
                    _transactionsSuccessfulPerSec.Increment(instanceNames, 1);
                }
                else
                {
                    _transactionsFailedPerSec.Increment(instanceNames, 1);
                }

                _transactionsAvgDuration.Increment(instanceNames, GetPerfCounterTicks(transactionFinishEvent.Duration), 1);

                _transactionsTotal.Increment(instanceNames, 1);
            }

            var stepFinishEvent = stepEvent as IStepFinishEvent;

            if (stepFinishEvent != null && _stepsCategoryInstalled && _stepsMode != PerfCounterMode.None)
            {
                var instanceNames = _stepsMode == PerfCounterMode.CategoriesOnly
                    ? new[] { stepFinishEvent.Category }
                    : new[] { stepFinishEvent.Category, $"{stepFinishEvent.Category}-{stepFinishEvent.Name}" };

                _stepsCompletedPerSec.Increment(instanceNames, 1);

                if (stepFinishEvent.IsSuccess)
                {
                    _stepsSuccessfulPerSec.Increment(instanceNames, 1);
                }
                else
                {
                    _stepsFailedPerSec.Increment(instanceNames, 1);
                }

                _stepsAvgDuration.Increment(instanceNames, GetPerfCounterTicks(stepFinishEvent.Duration), 1);

                _stepsTotal.Increment(instanceNames, 1);
            }
        }

        /// <summary>
        ///     Installs perf counters categories, the names of which are specified in the configruation 
        ///     object received by the constructor.
        /// </summary>
        public void Install()
        {
            if (_transactionsMode != PerfCounterMode.None)
            {
                EnsureCategory(_transactionsPerfCounterCategoryName,
                    TransactionsCategoryDescription,
                    _transactionsCompletedPerSec,
                    _transactionsSuccessfulPerSec,
                    _transactionsFailedPerSec,
                    _transactionsAvgDuration,
                    _transactionsTotal);
            }

            if (_stepsMode != PerfCounterMode.None)
            {
                EnsureCategory(_stepsPerfCounterCategoryName,
                    StepsCategoryDescription,
                    _stepsCompletedPerSec,
                    _stepsSuccessfulPerSec,
                    _stepsFailedPerSec,
                    _stepsAvgDuration,
                    _stepsTotal);
            }
        }

        /// <summary>
        ///     Uninstalls perf counter categories.
        /// </summary>
        public void Uninstall()
        {
            if (PerformanceCounterCategory.Exists(_transactionsPerfCounterCategoryName))
            {
                PerformanceCounterCategory.Delete(_transactionsPerfCounterCategoryName);
            }

            if (PerformanceCounterCategory.Exists(_stepsPerfCounterCategoryName))
            {
                PerformanceCounterCategory.Delete(_stepsPerfCounterCategoryName);
            }
        }

        private void EnsureCategory(string categoryName, string categoryDescription, params PerfCounter[] counters)
        {
            if (!IsCategoryInstalled(categoryName, counters))
            {
                if (PerformanceCounterCategory.Exists(categoryName))
                {
                    PerformanceCounterCategory.Delete(categoryName);
                }

                PerformanceCounterCategory.Create(categoryName, categoryDescription,
                    PerformanceCounterCategoryType.MultiInstance,
                    new CounterCreationDataCollection(counters.SelectMany(c => c.GetCounterCreationData()).ToArray()));
            }
        }

        private bool IsCategoryInstalled(string categoryName, params PerfCounter[] counters)
        {
            var transactionsCategory = PerformanceCounterCategory.GetCategories()
                .FirstOrDefault(c => c.CategoryName.Equals(categoryName,
                StringComparison.InvariantCultureIgnoreCase));

            if (transactionsCategory == null) return false;

            var installedCounters = transactionsCategory.GetCounters().ToDictionary(c => c.CounterName, c => c, StringComparer.InvariantCultureIgnoreCase);

            foreach (var counter in counters)
            {
                PerformanceCounter installedCounter;

                if (!installedCounters.TryGetValue(counter.Name, out installedCounter)
                    || installedCounter.CounterType != counter.CounterType)
                {
                    return false;
                }

                if (counter.BaseCounterType != null
                    && (installedCounters.TryGetValue(counter.BaseName, out installedCounter)
                        || installedCounter.CounterType != counter.CounterType))
                {
                    return false;
                }
            }

            return true;
        }

        private long GetPerfCounterTicks(TimeSpan duration)
        {
            return duration.Ticks * Stopwatch.Frequency / TimeSpan.TicksPerSecond;
        }
    }
}
