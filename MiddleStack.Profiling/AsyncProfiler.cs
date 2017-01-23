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
    public class AsyncProfiler: IAsyncProfiler
    {
        private const int MaxTrackedTransactionCount = 100;
        private static readonly TimeSpan EventQueueCheckInterval = TimeSpan.FromMilliseconds(100);
        private readonly Queue<Step> _recentTransactions = new Queue<Step>();
        private readonly ConcurrentDictionary<object, HandlerInfo> _eventHandlerMap 
            = new ConcurrentDictionary<object, HandlerInfo>();

        /// <summary>
        ///     Gets the singleton instance of <see cref="IAsyncProfiler"/>.
        /// </summary>
        /// <value>
        ///     The singleton instance of <see cref="IAsyncProfiler"/>.
        ///     Never <see langword="null"/>.
        /// </value>
        public static IAsyncProfiler Instance { get; } = new AsyncProfiler();

        IStep IAsyncProfiler.Step(string category, string name, string template, bool forceNewTransaction)
        {
            if (String.IsNullOrWhiteSpace(category)) throw new ArgumentNullException(nameof(category));
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (template != null && String.IsNullOrWhiteSpace(template)) throw new ArgumentNullException(nameof(template));

            return new Step(this, category, name, template, forceNewTransaction);
        }

        IList<Snapshot> IAsyncProfiler.GetRecentTransactions(bool inflightOnly)
        {
            IEnumerable<Snapshot> snapshots;

            lock (_recentTransactions)
            {
                snapshots = _recentTransactions.Select(t => t.GetTransactionSnapshot());
            }

            if (inflightOnly)
            {
                snapshots = snapshots.Where(t => t.IsFinished);
            }

            return snapshots.ToArray();
        }

        void IAsyncProfiler.RegisterStepEventHandler(IStepEventHandler stepEventHandler)
        {
            if (stepEventHandler == null) throw new ArgumentNullException(nameof(stepEventHandler));

            AddEventHandler(stepEventHandler);
        }

        void IAsyncProfiler.RegisterStepEventHandler(IStepEventHandlerAsync stepEventHandler)
        {
            if (stepEventHandler == null) throw new ArgumentNullException(nameof(stepEventHandler));

            AddEventHandler(stepEventHandler);
        }

        void IAsyncProfiler.UnregisterStepEventHandler(IStepEventHandler stepEventHandler)
        {
            if (stepEventHandler == null) throw new ArgumentNullException(nameof(stepEventHandler));

            RemoveEventHandler(stepEventHandler);
        }

        void IAsyncProfiler.UnregisterStepEventHandler(IStepEventHandlerAsync stepEventHandler)
        {
            if (stepEventHandler == null) throw new ArgumentNullException(nameof(stepEventHandler));

            RemoveEventHandler(stepEventHandler);
        }

        internal void RegisterEvent(IStepEvent stepEvent, Step step)
        {
            if (stepEvent.IsTransaction && stepEvent.EventType == StepEventType.Started)
            {
                lock (_recentTransactions)
                {
                    _recentTransactions.Enqueue(step);

                    while (_recentTransactions.Count > MaxTrackedTransactionCount)
                    {
                        _recentTransactions.Dequeue();
                    }
                }
            }

            foreach (var handlerInfo in _eventHandlerMap.Values)
            {
                handlerInfo.Queue.Enqueue(stepEvent);
            }
        }

        private void AddEventHandler(object handler)
        {
            var handlerInfo = new HandlerInfo
            {
                Queue = new ConcurrentQueue<IStepEvent>(),
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

            var syncHandler = handler as IStepEventHandler;
            var asyncHandler = handler as IStepEventHandlerAsync;

            do
            {
                IStepEvent stepEvent;

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
                        Trace.WriteLine($"AsyncProfiler: event handler {handler.GetType()} threw an exception: {x}");
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
            public ConcurrentQueue<IStepEvent> Queue { get; set; }
            public TaskCompletionSource<bool> CompletionSource { get; set; }
        }
    }
}
