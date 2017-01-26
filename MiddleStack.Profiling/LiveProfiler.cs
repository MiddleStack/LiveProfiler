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
        private readonly ConcurrentDictionary<object, HandlerInfo> _eventHandlerMap 
            = new ConcurrentDictionary<object, HandlerInfo>();

        /// <summary>
        ///     Gets the singleton instance of <see cref="ILiveProfiler"/>.
        /// </summary>
        /// <value>
        ///     The singleton instance of <see cref="ILiveProfiler"/>.
        ///     Never <see langword="null"/>.
        /// </value>
        public static ILiveProfiler Instance { get; } = new LiveProfiler();

        IStep ILiveProfiler.Step(string category, string name, string template)
        {
            if (String.IsNullOrWhiteSpace(category)) throw new ArgumentNullException(nameof(category));
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (template != null && String.IsNullOrWhiteSpace(template)) throw new ArgumentNullException(nameof(template));

            var currentStep = CallContextHelper.GetCurrentStep();

            if (currentStep != null)
            {
                lock (currentStep.SyncRoot)
                {
                    if (!currentStep.IsFinished)
                    {
                        var step = new Step(this, category, name, template, currentStep);

                        CallContextHelper.SetCurrentStep(step);
                        RegisterEvent(new StepStartEvent(step, step.Version), step);

                        return step;
                    }
                }
            }

            return null;
        }

        ITransaction ILiveProfiler.NewTransaction(string category, string name, string template, string correlationId, bool forceNew)
        {
            if (String.IsNullOrWhiteSpace(category)) throw new ArgumentNullException(nameof(category));
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (template != null && String.IsNullOrWhiteSpace(template)) throw new ArgumentNullException(nameof(template));

            var currentStep = CallContextHelper.GetCurrentStep();

            if (currentStep != null && !currentStep.IsFinished && !forceNew)
            {
                throw new InvalidOperationException($"An outstanding transaction is still inflight in the present context. The new transaction {name}, category {category}, template {template} cannot be created.");
            }

            var transaction = new Transaction(this, category, name, template, correlationId);

            lock (transaction.SyncRoot)
            {
                CallContextHelper.SetCurrentStep(transaction);
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
                snapshots = snapshots.Where(t => !t.IsFinished).ToArray();
            }

            return snapshots;
        }

        void ILiveProfiler.RegisterEventHandler(IProfilerEventHandler eventHandler)
        {
            if (eventHandler == null) throw new ArgumentNullException(nameof(eventHandler));

            AddEventHandler(eventHandler);
        }

        void ILiveProfiler.RegisterEventHandler(IProfilerEventHandlerAsync eventHandler)
        {
            if (eventHandler == null) throw new ArgumentNullException(nameof(eventHandler));

            AddEventHandler(eventHandler);
        }

        void ILiveProfiler.UnregisterEventHandler(IProfilerEventHandler eventHandler)
        {
            if (eventHandler == null) throw new ArgumentNullException(nameof(eventHandler));

            RemoveEventHandler(eventHandler);
        }

        void ILiveProfiler.UnregisterEventHandler(IProfilerEventHandlerAsync eventHandler)
        {
            if (eventHandler == null) throw new ArgumentNullException(nameof(eventHandler));

            RemoveEventHandler(eventHandler);
        }

        internal void RegisterEvent(IProfilerEvent stepEvent, StepBase step)
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

            foreach (var handlerInfo in _eventHandlerMap.Values)
            {
                handlerInfo.Queue.Enqueue(stepEvent);
            }
        }

        internal static void ResetForTesting()
        {
            CallContextHelper.SetCurrentStep(null);
            lock (((LiveProfiler) Instance)._recentTransactions)
            {
                ((LiveProfiler)Instance)._recentTransactions.Clear();
            }
        }

        private void AddEventHandler(object handler)
        {
            var handlerInfo = new HandlerInfo
            {
                Queue = new ConcurrentQueue<IProfilerEvent>(),
                CompletionSource = new TaskCompletionSource<bool>()
            };

            if (_eventHandlerMap.TryAdd(handler, handlerInfo))
            {
                EventLoop(handler, handlerInfo);
            }
        }

        private void RemoveEventHandler(object handler)
        {
            HandlerInfo handlerInfo;

            if (_eventHandlerMap.TryRemove(handler, out handlerInfo))
            {
                handlerInfo.CompletionSource.SetResult(true);
            }
        }

        private async void EventLoop(object handler, HandlerInfo handlerInfo)
        {
            await Task.Yield();

            var syncHandler = handler as IProfilerEventHandler;
            var asyncHandler = handler as IProfilerEventHandlerAsync;

            do
            {
                IProfilerEvent stepEvent;

                while (!handlerInfo.CompletionSource.Task.IsCompleted && handlerInfo.Queue.TryDequeue(out stepEvent))
                {
                    try
                    {
                        if (syncHandler != null)
                        {
                            syncHandler.HandleEvent(stepEvent);
                        }
                        else if (asyncHandler != null)
                        {
                            await asyncHandler.HandleEventAsync(stepEvent).ConfigureAwait(false);
                        }
                    }
                    catch (Exception x)
                    {
                        Trace.WriteLine($"LiveProfiler: event handler {handler.GetType()} threw an exception: {x}");
                    }
                }

                if (await Task.WhenAny(handlerInfo.CompletionSource.Task, Task.Delay(EventQueueCheckInterval))
                        .ConfigureAwait(false) == handlerInfo.CompletionSource.Task)
                {
                    return;
                }
            } while (true);
        }

        private class HandlerInfo
        {
            public ConcurrentQueue<IProfilerEvent> Queue { get; set; }
            public TaskCompletionSource<bool> CompletionSource { get; set; }
        }
    }
}
