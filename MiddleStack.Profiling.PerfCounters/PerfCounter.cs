using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.PerfCounters
{
    internal class PerfCounter
    {
        private readonly Dictionary<string, PerformanceCounter> _counters = new Dictionary<string, PerformanceCounter>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<string, PerformanceCounter> _baseCounters;

        public PerfCounter(string category, 
            string name,
            string description,
            PerformanceCounterType counterType,
            PerformanceCounterType? baseCounterType = null)
        {
            Category = category;
            Name = name;
            Description = description;
            CounterType = counterType;
            BaseCounterType = baseCounterType;

            if (baseCounterType != null)
            {
                _baseCounters = new Dictionary<string, PerformanceCounter>(StringComparer.InvariantCultureIgnoreCase);
            }
        }

        public string Category { get; }
        public string Name { get; }
        public string BaseName
        {
            [DebuggerStepThrough]
            get
            {
                return Name + "_base";
            }
        }

        public string Description { get; }
        public PerformanceCounterType CounterType { get; }
        public PerformanceCounterType? BaseCounterType { get; }

        public void Increment(IEnumerable<string> instanceNames, long incrementBy, long? incrementBaseBy = null)
        {
            foreach (var instanceName in instanceNames)
            {
                PerformanceCounter counter;

                if (!_counters.TryGetValue(instanceName, out counter))
                {
                    counter = new PerformanceCounter(Category, Name, instanceName, false);
                    _counters.Add(instanceName, counter);
                }

                counter.IncrementBy(incrementBy);

                if (_baseCounters != null && incrementBaseBy != null)
                {
                    PerformanceCounter baseCounter;

                    if (!_baseCounters.TryGetValue(instanceName, out baseCounter))
                    {
                        baseCounter = new PerformanceCounter(Category, BaseName, instanceName, false);
                        _baseCounters.Add(instanceName, baseCounter);
                    }

                    baseCounter.IncrementBy(incrementBaseBy.Value);
                }
            }
        }

        internal IEnumerable<CounterCreationData> GetCounterCreationData()
        {
            yield return new CounterCreationData(Name, Description, CounterType);

            if (BaseCounterType.HasValue)
            {
                yield return new CounterCreationData(BaseName, String.Empty, BaseCounterType.Value);
            }
        }
    }
}
