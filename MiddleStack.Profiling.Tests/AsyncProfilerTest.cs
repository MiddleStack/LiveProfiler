using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MiddleStack.Profiling.Events;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace MiddleStack.Profiling.Tests
{
    [TestFixture]
    public class AsyncProfilerTest
    {
        private readonly TimeSpan _duration0 = TimeSpan.FromMilliseconds(125);
        private readonly TimeSpan _duration1 = TimeSpan.FromMilliseconds(201);
        private readonly TimeSpan _duration2 = TimeSpan.FromMilliseconds(190);
        private readonly TimeSpan _duration3 = TimeSpan.FromMilliseconds(90);
        private const string Category0 = "F31552DCD4C246B9AA59883ACD7739A5";
        private const string Name0 = "915057F4C8C4411C8F9A818A9A1ED6AB";
        private const string Template0 = "BAD44AA98E6B4E3E8EE42901DA141FA2";
        private const string Category1 = "0200937234BF41EF9E0D7F5E68D481D5";
        private const string Name1 = "A86545B4C7B94D51BE567CA00AF190D0";
        private const string Template1 = "D5754E1AEA154B70A82057065B86D75D";
        private const string Category2 = "A5EC6C4A674043DD84DE9BB71EC654E5";
        private const string Name2 = "FC5F289F04AE4B978BEE9C753E7221D2";
        private const string Template2 = "0566A7BCD84C4B509E23185671EDDFFF";
        private const string Category3 = "89065DF4A71E4605B4B17345BA813928";
        private const string Name3 = "4F79F002757646798792A8A461E8BF11";
        private const string Template3 = "1188378A2EDA4807B2A9F65E886AAFA0";

        [Test]
        public void AsyncProfiler_Step_OneSyncStep_ReturnsOneSnapshot()
        {
            IStep rootStep;
            Snapshot unfinishedSnapshot;

            using (rootStep = AsyncProfiler.Instance.Step(
                    Category0, Name0, Template0))
            {
                Thread.Sleep(_duration0);
                unfinishedSnapshot = rootStep.GetTransactionSnapshot();
            }

            var finishedSnapshot = rootStep.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, true, _duration0);
            unfinishedSnapshot.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, false);
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_OneAsyncSyncStep_ReturnsOneSnapshot()
        {
            IStep rootStep = null;
            Snapshot unfinishedSnapshot = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (rootStep = AsyncProfiler.Instance.Step(
                        Category0, Name0, Template0))
                {
                    await Task.Delay(_duration0);
                    unfinishedSnapshot = rootStep.GetTransactionSnapshot();

                    await Task.Yield();
                }
            };

            action().Wait();

            var finishedSnapshot = rootStep.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, true, _duration0);
            unfinishedSnapshot.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, false);
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_AsyncNestedSteps_ReturnsNestedSnapshots()
        {
            IStep rootStep = null;
            Snapshot unfinishedSnapshot0 = null;
            Snapshot unfinishedSnapshot1 = null;
            Snapshot unfinishedSnapshot2 = null;
            Snapshot unfinishedSnapshot3 = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (rootStep = AsyncProfiler.Instance.Step(
                        Category0, Name0, Template0))
                {
                    await Task.Delay(_duration0);
                    unfinishedSnapshot0 = rootStep.GetTransactionSnapshot();

                    using (AsyncProfiler.Instance.Step(Category1, Name1, Template1))
                    {
                        await Task.Delay(_duration1);
                        unfinishedSnapshot1 = rootStep.GetTransactionSnapshot();

                        await Task.Yield();
                        using (AsyncProfiler.Instance.Step(Category2, Name2, Template2))
                        {
                            await Task.Delay(_duration2);
                            unfinishedSnapshot2 = rootStep.GetTransactionSnapshot();
                        }

                        await Task.Yield();
                        using (AsyncProfiler.Instance.Step(Category3, Name3, Template3))
                        {
                            await Task.Delay(_duration3);
                            unfinishedSnapshot3 = rootStep.GetTransactionSnapshot();
                        }
                    }

                    await Task.Yield();
                }
            };

            action().Wait();

            var finishedSnapshot = rootStep.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, true, 
                _duration0 + _duration1 + _duration2 + _duration3,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, Template1, _duration0, true, 
                        _duration1 + _duration2 + _duration3, 2,
                        c1 =>
                        {
                            var child2 = c1[0];
                            var child3 = c1[1];

                            child2.AssertChildlessStep(Category2, Name2, Template2, _duration0 + _duration1, true, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, Template3, _duration0 + _duration1 + _duration2, true, _duration3);
                        });
                });

            unfinishedSnapshot0.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, false);

            unfinishedSnapshot1.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, false,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertChildlessStep(Category1, Name1, Template1, _duration0, false);
                });

            unfinishedSnapshot2.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, false,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, Template1, _duration0, false,
                        null, 1,
                        c1 =>
                        {
                            var child2 = c1[0];

                            child2.AssertChildlessStep(Category2, Name2, Template2, _duration0 + _duration1, false);
                        });
                });

            unfinishedSnapshot3.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, false,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, Template1, _duration0, false,
                        null, 2,
                        c1 =>
                        {
                            var child2 = c1[0];
                            var child3 = c1[1];

                            child2.AssertChildlessStep(Category2, Name2, Template2, _duration0 + _duration1, true, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, Template3, _duration0 + _duration1 + _duration2, false);
                        });
                });

            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_SyncNestedSteps_ReturnsNestedSnapshots()
        {
            IStep rootStep = null;
            Snapshot unfinishedSnapshot0 = null;
            Snapshot unfinishedSnapshot1 = null;
            Snapshot unfinishedSnapshot2 = null;
            Snapshot unfinishedSnapshot3 = null;

            using (rootStep = AsyncProfiler.Instance.Step(
                    Category0, Name0, Template0))
            {
                Thread.Sleep(_duration0);
                unfinishedSnapshot0 = rootStep.GetTransactionSnapshot();

                using (AsyncProfiler.Instance.Step(Category1, Name1, Template1))
                {
                    Thread.Sleep(_duration1);
                    unfinishedSnapshot1 = rootStep.GetTransactionSnapshot();

                    using (AsyncProfiler.Instance.Step(Category2, Name2, Template2))
                    {
                        Thread.Sleep(_duration2);
                        unfinishedSnapshot2 = rootStep.GetTransactionSnapshot();
                    }

                    using (AsyncProfiler.Instance.Step(Category3, Name3, Template3))
                    {
                        Thread.Sleep(_duration3);
                        unfinishedSnapshot3 = rootStep.GetTransactionSnapshot();
                    }
                }
            }

            var finishedSnapshot = rootStep.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, true,
                _duration0 + _duration1 + _duration2 + _duration3,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, Template1, _duration0, true,
                        _duration1 + _duration2 + _duration3, 2,
                        c1 =>
                        {
                            var child2 = c1[0];
                            var child3 = c1[1];

                            child2.AssertChildlessStep(Category2, Name2, Template2, _duration0 + _duration1, true, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, Template3, _duration0 + _duration1 + _duration2, true, _duration3);
                        });
                });

            unfinishedSnapshot0.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, false);

            unfinishedSnapshot1.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, false,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertChildlessStep(Category1, Name1, Template1, _duration0, false);
                });

            unfinishedSnapshot2.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, false,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, Template1, _duration0, false,
                        null, 1,
                        c1 =>
                        {
                            var child2 = c1[0];

                            child2.AssertChildlessStep(Category2, Name2, Template2, _duration0 + _duration1, false);
                        });
                });

            unfinishedSnapshot3.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, false,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, Template1, _duration0, false,
                        null, 2,
                        c1 =>
                        {
                            var child2 = c1[0];
                            var child3 = c1[1];

                            child2.AssertChildlessStep(Category2, Name2, Template2, _duration0 + _duration1, true, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, Template3, _duration0 + _duration1 + _duration2, false);
                        });
                });

            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_AsyncParallelNestedSteps_ReturnsNestedSnapshots()
        {
            IStep rootStep = null;
            Snapshot unfinishedSnapshot0 = null;
            Snapshot unfinishedSnapshot1 = null;
            Snapshot unfinishedSnapshot2 = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (rootStep = AsyncProfiler.Instance.Step(
                        Category0, Name0, Template0))
                {
                    await Task.Delay(_duration0);
                    unfinishedSnapshot0 = rootStep.GetTransactionSnapshot();

                    using (AsyncProfiler.Instance.Step(Category1, Name1, Template1))
                    {
                        await Task.Delay(_duration1);
                        unfinishedSnapshot1 = rootStep.GetTransactionSnapshot();

                        Func<Task> parallel0 = async () =>
                        {
                            await Task.Yield();
                            using (AsyncProfiler.Instance.Step(Category2, Name2, Template2))
                            {
                                await Task.Delay(_duration2);
                            }
                        };

                        Func<Task> parallel1 = async () =>
                        {
                            await Task.Yield();
                            using (AsyncProfiler.Instance.Step(Category3, Name3, Template3))
                            {
                                await Task.Delay(_duration3);
                            }
                        };

                        await Task.WhenAll(parallel0(), parallel1());
                    }

                    await Task.Yield();

                    unfinishedSnapshot2 = rootStep.GetTransactionSnapshot();
                }
            };

            action().Wait();

            var finishedSnapshot = rootStep.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, true, 
                _duration0 + _duration1 + _duration2,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, Template1, _duration0, true, 
                        _duration1 + _duration2, 2,
                        c1 =>
                        {
                            var child2 = c1.First(c => c.Name == Name2);
                            var child3 = c1.First(c => c.Name == Name3);

                            child2.AssertChildlessStep(Category2, Name2, Template2, _duration0 + _duration1, true, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, Template3, _duration0 + _duration1, true, _duration3);
                        });
                });

            unfinishedSnapshot0.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, false);

            unfinishedSnapshot1.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, false,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertChildlessStep(Category1, Name1, Template1, _duration0, false);
                });

            unfinishedSnapshot2.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, false,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, Template1, _duration0, true,
                        _duration1 + _duration2, 2,
                        c1 =>
                        {
                            var child2 = c1.First(c => c.Name == Name2);
                            var child3 = c1.First(c => c.Name == Name3);

                            child2.AssertChildlessStep(Category2, Name2, Template2, _duration0 + _duration1, true, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, Template3, _duration0 + _duration1, true, _duration3);
                        });
                });

            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_AsyncParallelNestedStepsWithNoSyncContext_ReturnsNestedSnapshots()
        {
            IStep rootStep = null;
            Snapshot unfinishedSnapshot0 = null;
            Snapshot unfinishedSnapshot1 = null;
            Snapshot unfinishedSnapshot2 = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (rootStep = AsyncProfiler.Instance.Step(
                        Category0, Name0, Template0))
                {
                    await Task.Delay(_duration0).ConfigureAwait(false);
                    unfinishedSnapshot0 = rootStep.GetTransactionSnapshot();

                    using (AsyncProfiler.Instance.Step(Category1, Name1, Template1))
                    {
                        await Task.Delay(_duration1).ConfigureAwait(false);
                        unfinishedSnapshot1 = rootStep.GetTransactionSnapshot();

                        Func<Task> parallel0 = async () =>
                        {
                            await Task.Yield();
                            using (AsyncProfiler.Instance.Step(Category2, Name2, Template2))
                            {
                                await Task.Delay(_duration2).ConfigureAwait(false);
                            }
                        };

                        Func<Task> parallel1 = async () =>
                        {
                            await Task.Yield();
                            using (AsyncProfiler.Instance.Step(Category3, Name3, Template3))
                            {
                                await Task.Delay(_duration3).ConfigureAwait(false);
                            }
                        };

                        await Task.WhenAll(parallel0(), parallel1()).ConfigureAwait(false);
                    }

                    await Task.Yield();

                    unfinishedSnapshot2 = rootStep.GetTransactionSnapshot();
                }
            };

            action().Wait();

            var finishedSnapshot = rootStep.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, true, 
                _duration0 + _duration1 + _duration2,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, Template1, _duration0, true, 
                        _duration1 + _duration2, 2,
                        c1 =>
                        {
                            var child2 = c1.First(c => c.Name == Name2);
                            var child3 = c1.First(c => c.Name == Name3);

                            child2.AssertChildlessStep(Category2, Name2, Template2, _duration0 + _duration1, true, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, Template3, _duration0 + _duration1, true, _duration3);
                        });
                });

            unfinishedSnapshot0.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, false);

            unfinishedSnapshot1.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, false,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertChildlessStep(Category1, Name1, Template1, _duration0, false);
                });

            unfinishedSnapshot2.AssertStep(Category0, Name0, Template0, TimeSpan.Zero, false,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, Template1, _duration0, true,
                        _duration1 + _duration2, 2,
                        c1 =>
                        {
                            var child2 = c1.First(c => c.Name == Name2);
                            var child3 = c1.First(c => c.Name == Name3);

                            child2.AssertChildlessStep(Category2, Name2, Template2, _duration0 + _duration1, true, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, Template3, _duration0 + _duration1, true, _duration3);
                        });
                });

            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_NullCategory_ThrowsArgumentNullException()
        {
            Action thrower = () => AsyncProfiler.Instance.Step(null, Name0, Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("category");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_EmptyCategory_ThrowsArgumentNullException()
        {
            Action thrower = () => AsyncProfiler.Instance.Step("    ", Name0, Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("category");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_NullName_ThrowsArgumentNullException()
        {
            Action thrower = () => AsyncProfiler.Instance.Step(Category0, null, Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("name");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_EmptyName_ThrowsArgumentNullException()
        {
            Action thrower = () => AsyncProfiler.Instance.Step(Category0, "    ", Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("name");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_EmptyTemplate_ThrowsArgumentNullException()
        {
            Action thrower = () => AsyncProfiler.Instance.Step(Category0, Name0, "    ");

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("template");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_NoTemplate_ResultsInTransactionWithoutTemplate()
        {
            IStep rootStep;

            using (rootStep = AsyncProfiler.Instance.Step(Category0, Name0))
            {
            }

            var finishedSnapshot = rootStep.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, null, TimeSpan.Zero, true, TimeSpan.Zero);
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_Step_MultipleSimultaneousTransactionsInBothThreadPoolAndNonThreadPoolThreads_TransactionsDontLeakIntoEachOther()
        {
            var transactionSnapshots = new ConcurrentBag<Snapshot>();
            var transactionCount = Environment.ProcessorCount * 2;
            var childStepCount = 100;

            var tasks = new List<Task>();

            for (var i = 0; i < transactionCount; i++)
            {
                var iStr = i.ToString();

                if (i % 2 == 0)
                {
                    // thread pool threads
                    Func<Task> action = async () =>
                    {
                        await Task.Yield();
                        IStep rootStep;

                        using (rootStep = AsyncProfiler.Instance.Step(iStr, iStr, iStr))
                        {
                            await Task.Delay(10);

                            var childTasks = new List<Task>();

                            for (var j = 0; j < childStepCount; j++)
                            {
                                Func<Task> childAction = async () =>
                                {
                                    await Task.Yield();
                                    using (AsyncProfiler.Instance.Step(iStr, iStr, iStr))
                                    {
                                        await Task.Delay(10);
                                    }
                                };
                                childTasks.Add(childAction());
                            }

                            await Task.WhenAll(childTasks);
                        }

                        transactionSnapshots.Add(rootStep.GetTransactionSnapshot());
                    };

                    tasks.Add(action());
                }
                else
                {
                    // Independent threads
                    var completionSource = new TaskCompletionSource<bool>();

                    new Thread(() =>
                    {
                        try
                        {
                            IStep rootStep;

                            using (rootStep = AsyncProfiler.Instance.Step(iStr, iStr, iStr))
                            {
                                Thread.Sleep(10);

                                var childTasks = new List<Task>();

                                for (var j = 0; j < childStepCount; j++)
                                {
                                    var childCompletionSource = new TaskCompletionSource<bool>();

                                    new Thread(() =>
                                    {
                                        try
                                        {
                                            using (AsyncProfiler.Instance.Step(iStr, iStr, iStr))
                                            {
                                                Thread.Sleep(10);
                                            }
                                            childCompletionSource.SetResult(true);
                                        }
                                        catch (Exception x)
                                        {
                                            childCompletionSource.SetException(x);
                                        }
                                    }).Start();

                                    childTasks.Add(childCompletionSource.Task);
                                }

                                Task.WhenAll(childTasks).Wait();
                            }

                            transactionSnapshots.Add(rootStep.GetTransactionSnapshot());
                            completionSource.SetResult(true);
                        }
                        catch (Exception x)
                        {
                            completionSource.SetException(x);
                        }
                    }).Start();

                    tasks.Add(completionSource.Task);
                }
            }

            Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(10));

            transactionSnapshots.Should().HaveCount(transactionCount);

            transactionSnapshots.GroupBy(s => s.Name).Should().HaveCount(transactionCount);

            foreach (var snapshot in transactionSnapshots)
            {
                snapshot.Steps.Should().HaveCount(childStepCount);

                snapshot.Steps.Count(s => s.Name == snapshot.Name).Should().Be(childStepCount);
            }
        }

        [Test]
        public void AsyncProfiler_Step_ForceNewTransaction_ReplacesExistingTransaction()
        {
            IStep rootStep0, rootStep1;

            using (rootStep0 = AsyncProfiler.Instance.Step(Category0, Name0, Template0))
            {
                Thread.Sleep(_duration0);
                using (rootStep1 = AsyncProfiler.Instance.Step(Category1, Name1, Template1, true))
                {
                    Thread.Sleep(_duration1);
                    using (AsyncProfiler.Instance.Step(Category2, Name2, Template2))
                    {
                        Thread.Sleep(_duration2);
                    }
                }
            }

            var finishedSnapshot0 = rootStep0.GetTransactionSnapshot();
            var finishedSnapshot1 = rootStep1.GetTransactionSnapshot();

            // verify
            finishedSnapshot0.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, true, _duration0 + _duration1 + _duration2);
            finishedSnapshot1.AssertStep(Category1, Name1, Template1, TimeSpan.Zero, true, _duration1 + _duration2, 1,
                c =>
                {
                    c[0].AssertChildlessStep(Category2, Name2, Template2, _duration1, true, _duration2);
                });

            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void AsyncProfiler_EventHandlers_RapidEventsInSingleTransaction_AllHandlersReceiveEventsInCorrectOrder()
        {
            // arrange
            const int stepCount = 100;
            var syncCompletionSource = new TaskCompletionSource<bool>();
            var asyncCompletionSource = new TaskCompletionSource<bool>();
            var syncHandler = new Mock<IStepEventHandler>();
            var asyncHandler = new Mock<IStepEventHandlerAsync>();

            var syncEvents = new List<IStepEvent>();
            var asyncEvents = new List<IStepEvent>();

            syncHandler.Setup(h => h.HandleEvent(It.IsAny<IStepEvent>()))
                .Callback<IStepEvent>(evt =>
                {
                    lock (syncEvents) syncEvents.Add(evt);
                    if (evt.IsTransaction && evt.IsFinished) syncCompletionSource.SetResult(true);
                });

            asyncHandler.Setup(h => h.HandleEventAsync(It.IsAny<IStepEvent>()))
                .Returns<IStepEvent>(evt =>
                {
                    lock (asyncEvents) asyncEvents.Add(evt);
                    if (evt.IsTransaction && evt.IsFinished) asyncCompletionSource.SetResult(true);
                    return Task.FromResult(true);
                });

            // execute
            try
            {
                AsyncProfiler.Instance.RegisterStepEventHandler(syncHandler.Object);
                AsyncProfiler.Instance.RegisterStepEventHandler(asyncHandler.Object);

                using (AsyncProfiler.Instance.Step(Category0, Name0, Template0))
                {
                    for (var i = 0; i < stepCount; i++)
                    {
                        var str = i.ToString();
                        using (AsyncProfiler.Instance.Step(str, str, str))
                        {
                        }
                    }
                }

                syncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                asyncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

                // verify
                syncEvents.Should().HaveCount(stepCount * 2 + 2);

                syncEvents.First().Name.Should().Be(Name0);
                syncEvents.First().Category.Should().Be(Category0);
                syncEvents.First().Template.Should().Be(Template0);
                syncEvents.First().Duration.Should().BeNull();
                syncEvents.First().EventType.Should().Be(StepEventType.Started);
                syncEvents.First().Id.Should().NotBeEmpty();
                syncEvents.First().ParentId.Should().BeNull();
                syncEvents.First().IsFinished.Should().BeFalse();
                syncEvents.First().IsTransaction.Should().BeTrue();

                var childStepEvents = syncEvents.Skip(1).Take(stepCount * 2).ToArray();

                for (int i = 0; i < stepCount * 2; i += 2)
                {
                    var startEvent = childStepEvents[i];
                    var finishEvent = childStepEvents[i + 1];

                    var name = (i/2).ToString();

                    startEvent.Name.Should().Be(name);
                    startEvent.Category.Should().Be(name);
                    startEvent.Template.Should().Be(name);
                    startEvent.Duration.Should().BeNull();
                    startEvent.EventType.Should().Be(StepEventType.Started);
                    startEvent.Id.Should().NotBeEmpty();
                    startEvent.ParentId.Should().Be(syncEvents.First().Id);
                    startEvent.IsFinished.Should().BeFalse();
                    startEvent.IsTransaction.Should().BeFalse();

                    finishEvent.Name.Should().Be(name);
                    finishEvent.Category.Should().Be(name);
                    finishEvent.Template.Should().Be(name);
                    finishEvent.Duration.Should().NotBeNull();
                    finishEvent.EventType.Should().Be(StepEventType.Finished);
                    finishEvent.Id.Should().NotBeEmpty();
                    finishEvent.ParentId.Should().Be(syncEvents.First().Id);
                    finishEvent.IsFinished.Should().BeTrue();
                    finishEvent.IsTransaction.Should().BeFalse();
                }

                syncEvents.Last().Name.Should().Be(Name0);
                syncEvents.Last().Category.Should().Be(Category0);
                syncEvents.Last().Template.Should().Be(Template0);
                syncEvents.Last().Duration.Should().NotBeNull();
                syncEvents.Last().EventType.Should().Be(StepEventType.Finished);
                syncEvents.Last().Id.Should().NotBeEmpty();
                syncEvents.Last().ParentId.Should().BeNull();
                syncEvents.Last().IsFinished.Should().BeTrue();
                syncEvents.Last().IsTransaction.Should().BeTrue();
            }
            finally
            {
                AsyncProfiler.Instance.UnregisterStepEventHandler(syncHandler.Object);
                AsyncProfiler.Instance.UnregisterStepEventHandler(asyncHandler.Object);
            }
        }
    }
}
