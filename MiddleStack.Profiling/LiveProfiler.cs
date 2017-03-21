using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MiddleStack.Profiling.Events;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Profiles code in a thread-safe manner that supports asynchrony and 
    ///     concurrency.
    /// </summary>
    public class LiveProfiler: ILiveProfiler
    {
        private const int MaxTrackedTransactionCount = 100;
        private static readonly TimeSpan EventQueueCheckInterval = TimeSpan.FromMilliseconds(100);
        private readonly Queue<Transaction> _recentTransactions = new Queue<Transaction>();
        private readonly ConcurrentDictionary<object, SubscriberInfo> _eventSubscriberMap 
            = new ConcurrentDictionary<object, SubscriberInfo>();
        private static readonly InertTiming InertTiming = new InertTiming();

        /// <summary>
        ///     Gets the singleton instance of <see cref="ILiveProfiler"/>.
        /// </summary>
        /// <value>
        ///     The singleton instance of <see cref="ILiveProfiler"/>.
        ///     Never <see langword="null"/>.
        /// </value>
        public static ILiveProfiler Instance { get; } = new LiveProfiler();

        ITiming ILiveProfiler.Step(string category, string name, string displayName, object parameters, Func<IProfilerContext, bool> predicate)
        {
            if (String.IsNullOrWhiteSpace(category)) throw new ArgumentNullException(nameof(category));
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            var currentTiming = TimingStore.GetCurrentTiming();

            if (currentTiming != null
                && (predicate == null || predicate(new ProfilerContext(currentTiming))))
            {
                var step = DoStep(category, name, displayName, parameters, currentTiming);

                if (step != null)
                {
                    return step;
                }
            }

            return InertTiming;
        }

        private Step DoStep(string category, string name, string displayName, object parameters, Timing currentStep)
        {
            Step step = null;

            lock (currentStep.SyncRoot)
            {
                if (currentStep.State == TransactionState.Inflight)
                {
                    step = new Step(this, category, name, displayName, parameters, currentStep);

                    TimingStore.SetCurrentTiming(step);
                    RegisterEvent(new StepStartEvent(step, step.Version), step);
                }
            }
            return step;
        }

        ITiming ILiveProfiler.Transaction(string category, string name, string displayName, object parameters, string correlationId, TransactionMode mode)
        {
            if (String.IsNullOrWhiteSpace(category)) throw new ArgumentNullException(nameof(category));
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            var currentStep = TimingStore.GetCurrentTiming();

            if (currentStep != null && currentStep.State == TransactionState.Inflight && mode == TransactionMode.New)
            {
                throw new InvalidOperationException($"An outstanding transaction is still inflight in the present context. The new transaction {name}, category {category}, cannot be created.");
            }

            if (currentStep != null && mode == TransactionMode.StepOrTransaction)
            {
                var step = DoStep(category, name, displayName, parameters, currentStep);

                if (step != null)
                {
                    return step;
                }
            }

            var transaction = new Transaction(this, category, name, displayName, parameters, correlationId);

            lock (transaction.SyncRoot)
            {
                TimingStore.SetCurrentTiming(transaction);
                RegisterEvent(new TransactionStartEvent(transaction, transaction.Version), transaction);
            }

            return transaction;
        }

        IList<TransactionSnapshot> ILiveProfiler.GetRecentTransactions(bool inflightOnly)
        {
            IList<TransactionSnapshot> snapshots;

            lock (_recentTransactions)
            {
                snapshots = _recentTransactions.OrderByDescending(t => t.Sequence).Select(t => t.GetTransactionSnapshot()).ToArray();
            }

            if (inflightOnly)
            {
                snapshots = snapshots.Where(t => t.State == TransactionState.Inflight).ToArray();
            }

            return snapshots;
        }

        void ILiveProfiler.RegisterEventSubscriber(IProfilerEventSubscriber eventSubscriber)
        {
            if (eventSubscriber == null) throw new ArgumentNullException(nameof(eventSubscriber));

            AddEventSubscriber(eventSubscriber);
        }

        void ILiveProfiler.RegisterEventSubscriber(IProfilerEventSubscriberAsync eventSubscriber)
        {
            if (eventSubscriber == null) throw new ArgumentNullException(nameof(eventSubscriber));

            AddEventSubscriber(eventSubscriber);
        }

        void ILiveProfiler.UnregisterEventSubscriber(IProfilerEventSubscriber eventSubscriber)
        {
            if (eventSubscriber == null) throw new ArgumentNullException(nameof(eventSubscriber));

            RemoveEventSubscriber(eventSubscriber);
        }

        void ILiveProfiler.UnregisterEventSubscriber(IProfilerEventSubscriberAsync eventSubscriber)
        {
            if (eventSubscriber == null) throw new ArgumentNullException(nameof(eventSubscriber));

            RemoveEventSubscriber(eventSubscriber);
        }

        internal void RegisterEvent(IProfilerEvent stepEvent, Timing step)
        {
            var transaction = step as Transaction;

            if (transaction != null && stepEvent is ITransactionStartEvent)
            {
                lock (_recentTransactions)
                {
                    while (_recentTransactions.Count >= MaxTrackedTransactionCount)
                    {
                        _recentTransactions.Dequeue();
                    }

                    _recentTransactions.Enqueue(transaction);
                }
            }

            foreach (var subscriberInfo in _eventSubscriberMap.Values)
            {
                subscriberInfo.Queue.Enqueue(stepEvent);
            }
        }

        internal void TestingReset()
        {
            TimingStore.TestReset();
            lock (((LiveProfiler) Instance)._recentTransactions)
            {
                ((LiveProfiler)Instance)._recentTransactions.Clear();
            }

            _eventSubscriberMap.Clear();
        }

        private void AddEventSubscriber(object subscriber)
        {
            var subscriberInfo = new SubscriberInfo
            {
                Queue = new ConcurrentQueue<IProfilerEvent>(),
                CompletionSource = new TaskCompletionSource<bool>()
            };

            if (_eventSubscriberMap.TryAdd(subscriber, subscriberInfo))
            {
                EventLoop(subscriber, subscriberInfo);
            }
        }

        private void RemoveEventSubscriber(object subscriber)
        {
            SubscriberInfo subscriberInfo;

            if (_eventSubscriberMap.TryRemove(subscriber, out subscriberInfo))
            {
                subscriberInfo.CompletionSource.SetResult(true);
            }
        }

        private async void EventLoop(object subscriber, SubscriberInfo subscriberInfo)
        {
            await Task.Yield();

            var syncSubscriber = subscriber as IProfilerEventSubscriber;
            var asyncSubscriber = subscriber as IProfilerEventSubscriberAsync;

            try
            {
                syncSubscriber?.Start();
                asyncSubscriber?.Start();
            }
            catch (Exception x)
            {
                Trace.WriteLine($"LiveProfiler: event subscriber {subscriber.GetType()} threw an exception on Start. Events will not be sent to this subscriber: {x}");
                return;
            }

            do
            {
                IProfilerEvent stepEvent;

                while (!subscriberInfo.CompletionSource.Task.IsCompleted && subscriberInfo.Queue.TryDequeue(out stepEvent))
                {
                    try
                    {
                        if (syncSubscriber != null)
                        {
                            syncSubscriber.HandleEvent(stepEvent);
                        }
                        else if (asyncSubscriber != null)
                        {
                            await asyncSubscriber.HandleEventAsync(stepEvent).ConfigureAwait(false);
                        }
                    }
                    catch (Exception x)
                    {
                        Trace.WriteLine($"LiveProfiler: event subscriber {subscriber.GetType()} threw an exception: {x}");
                    }
                }

                if (await Task.WhenAny(subscriberInfo.CompletionSource.Task, Task.Delay(EventQueueCheckInterval))
                        .ConfigureAwait(false) == subscriberInfo.CompletionSource.Task)
                {
                    try
                    {
                        syncSubscriber?.Stop();
                        asyncSubscriber?.Stop();
                    }
                    catch (Exception x)
                    {
                        Trace.WriteLine($"LiveProfiler: event subscriber {subscriber.GetType()} threw an exception on Stop: {x}");
                    }

                    return;
                }
            } while (true);
        }

        private class SubscriberInfo
        {
            public ConcurrentQueue<IProfilerEvent> Queue { get; set; }
            public TaskCompletionSource<bool> CompletionSource { get; set; }
        }
    }
}
