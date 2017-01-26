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
    public class LiveProfilerTest
    {
        private readonly TimeSpan _duration0 = TimeSpan.FromMilliseconds(125);
        private readonly TimeSpan _duration1 = TimeSpan.FromMilliseconds(201);
        private readonly TimeSpan _duration2 = TimeSpan.FromMilliseconds(190);
        private readonly TimeSpan _duration3 = TimeSpan.FromMilliseconds(90);
        private const string Category0 = "F31552DCD4C246B9AA59883ACD7739A5";
        private const string Name0 = "915057F4C8C4411C8F9A818A9A1ED6AB";
        private const string Template0 = "BAD44AA98E6B4E3E8EE42901DA141FA2";
        private const string CorrelationId0 = "68917BE3A0154A17A76299E32EEDACCB";
        private const string Category1 = "0200937234BF41EF9E0D7F5E68D481D5";
        private const string Name1 = "A86545B4C7B94D51BE567CA00AF190D0";
        private const string Template1 = "D5754E1AEA154B70A82057065B86D75D";
        private const string CorrelationId1 = "349BDEB2BAEF4E1BA169467C3992EDFC";
        private const string Category2 = "A5EC6C4A674043DD84DE9BB71EC654E5";
        private const string Name2 = "FC5F289F04AE4B978BEE9C753E7221D2";
        private const string Template2 = "0566A7BCD84C4B509E23185671EDDFFF";
        private const string CorrelationId2 = "DE7E9A3B99B84D1F9926E24EDF02A8EC";
        private const string Category3 = "89065DF4A71E4605B4B17345BA813928";
        private const string Name3 = "4F79F002757646798792A8A461E8BF11";
        private const string Template3 = "1188378A2EDA4807B2A9F65E886AAFA0";
        private const string CorrelationId3 = "91B968425C174D5B9BAEF775A3666A3A";

        [SetUp]
        public void Initialize()
        {
            LiveProfiler.ResetForTesting();
        }

        [Test]
        public void LiveProfiler_OneSyncStep_ReturnsOneSnapshot()
        {
            ITransaction transaction;
            TransactionSnapshot unfinishedSnapshot;

            using (transaction = LiveProfiler.Instance.NewTransaction(
                    Category0, Name0, Template0))
            {
                Thread.Sleep(_duration0);
                unfinishedSnapshot = transaction.GetTransactionSnapshot();
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, true, _duration0);
            unfinishedSnapshot.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, false);
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_CorrelationIdSpecified_ReturnsSnapshotWithCorrelationId()
        {
            ITransaction transaction;

            using (transaction = LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0, CorrelationId0))
            {
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, true, TimeSpan.Zero, correlationId:CorrelationId0);
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_TransactionDisposedWithUnfinishedSteps_ThrowsException()
        {
            Action thrower = () =>
            {
                using (LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0, CorrelationId0))
                {
                    Thread.Sleep(_duration0);
                    LiveProfiler.Instance.Step(Category1, Name1, Template1);
                    Thread.Sleep(_duration1);
                    LiveProfiler.Instance.Step(Category2, Name2, Template2);
                }
            };

            thrower.ShouldThrow<InvalidOperationException>();   
        }

        [Test]
        public void LiveProfiler_OneAsyncSyncStep_ReturnsOneSnapshot()
        {
            ITransaction transaction = null;
            TransactionSnapshot unfinishedSnapshot = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (transaction = LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0))
                {
                    await Task.Delay(_duration0);
                    unfinishedSnapshot = transaction.GetTransactionSnapshot();

                    await Task.Yield();
                }
            };

            action().Wait();

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, true, _duration0);
            unfinishedSnapshot.AssertChildlessStep(Category0, Name0, Template0, TimeSpan.Zero, false);
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_AsyncNestedSteps_ReturnsNestedSnapshots()
        {
            ITransaction transaction = null;
            TransactionSnapshot unfinishedSnapshot0 = null;
            TransactionSnapshot unfinishedSnapshot1 = null;
            TransactionSnapshot unfinishedSnapshot2 = null;
            TransactionSnapshot unfinishedSnapshot3 = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (transaction = LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0))
                {
                    await Task.Delay(_duration0);
                    unfinishedSnapshot0 = transaction.GetTransactionSnapshot();

                    using (LiveProfiler.Instance.Step(Category1, Name1, Template1))
                    {
                        await Task.Delay(_duration1);
                        unfinishedSnapshot1 = transaction.GetTransactionSnapshot();

                        await Task.Yield();
                        using (LiveProfiler.Instance.Step(Category2, Name2, Template2))
                        {
                            await Task.Delay(_duration2);
                            unfinishedSnapshot2 = transaction.GetTransactionSnapshot();
                        }

                        await Task.Yield();
                        using (LiveProfiler.Instance.Step(Category3, Name3, Template3))
                        {
                            await Task.Delay(_duration3);
                            unfinishedSnapshot3 = transaction.GetTransactionSnapshot();
                        }
                    }

                    await Task.Yield();
                }
            };

            action().Wait();

            var finishedSnapshot = transaction.GetTransactionSnapshot();

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
        public void LiveProfiler_SyncNestedSteps_ReturnsNestedSnapshots()
        {
            ITransaction transaction = null;
            TransactionSnapshot unfinishedSnapshot0 = null;
            TransactionSnapshot unfinishedSnapshot1 = null;
            TransactionSnapshot unfinishedSnapshot2 = null;
            TransactionSnapshot unfinishedSnapshot3 = null;

            using (transaction = LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0))
            {
                Thread.Sleep(_duration0);
                unfinishedSnapshot0 = transaction.GetTransactionSnapshot();

                using (LiveProfiler.Instance.Step(Category1, Name1, Template1))
                {
                    Thread.Sleep(_duration1);
                    unfinishedSnapshot1 = transaction.GetTransactionSnapshot();

                    using (LiveProfiler.Instance.Step(Category2, Name2, Template2))
                    {
                        Thread.Sleep(_duration2);
                        unfinishedSnapshot2 = transaction.GetTransactionSnapshot();
                    }

                    using (LiveProfiler.Instance.Step(Category3, Name3, Template3))
                    {
                        Thread.Sleep(_duration3);
                        unfinishedSnapshot3 = transaction.GetTransactionSnapshot();
                    }
                }
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

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
        public void LiveProfiler_AsyncParallelNestedSteps_ReturnsNestedSnapshots()
        {
            ITransaction transaction = null;
            TransactionSnapshot unfinishedSnapshot0 = null;
            TransactionSnapshot unfinishedSnapshot1 = null;
            TransactionSnapshot unfinishedSnapshot2 = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (transaction = LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0))
                {
                    await Task.Delay(_duration0);
                    unfinishedSnapshot0 = transaction.GetTransactionSnapshot();

                    using (LiveProfiler.Instance.Step(Category1, Name1, Template1))
                    {
                        await Task.Delay(_duration1);
                        unfinishedSnapshot1 = transaction.GetTransactionSnapshot();

                        Func<Task> parallel0 = async () =>
                        {
                            await Task.Yield();
                            using (LiveProfiler.Instance.Step(Category2, Name2, Template2))
                            {
                                await Task.Delay(_duration2);
                            }
                        };

                        Func<Task> parallel1 = async () =>
                        {
                            await Task.Yield();
                            using (LiveProfiler.Instance.Step(Category3, Name3, Template3))
                            {
                                await Task.Delay(_duration3);
                            }
                        };

                        await Task.WhenAll(parallel0(), parallel1());
                    }

                    await Task.Yield();

                    unfinishedSnapshot2 = transaction.GetTransactionSnapshot();
                }
            };

            action().Wait();

            var finishedSnapshot = transaction.GetTransactionSnapshot();

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
        public void LiveProfiler_AsyncParallelNestedStepsWithNoSyncContext_ReturnsNestedSnapshots()
        {
            ITransaction transaction = null;
            TransactionSnapshot unfinishedSnapshot0 = null;
            TransactionSnapshot unfinishedSnapshot1 = null;
            TransactionSnapshot unfinishedSnapshot2 = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (transaction = LiveProfiler.Instance.NewTransaction(
                        Category0, Name0, Template0))
                {
                    await Task.Delay(_duration0).ConfigureAwait(false);
                    unfinishedSnapshot0 = transaction.GetTransactionSnapshot();

                    using (LiveProfiler.Instance.Step(Category1, Name1, Template1))
                    {
                        await Task.Delay(_duration1).ConfigureAwait(false);
                        unfinishedSnapshot1 = transaction.GetTransactionSnapshot();

                        Func<Task> parallel0 = async () =>
                        {
                            await Task.Yield();
                            using (LiveProfiler.Instance.Step(Category2, Name2, Template2))
                            {
                                await Task.Delay(_duration2).ConfigureAwait(false);
                            }
                        };

                        Func<Task> parallel1 = async () =>
                        {
                            await Task.Yield();
                            using (LiveProfiler.Instance.Step(Category3, Name3, Template3))
                            {
                                await Task.Delay(_duration3).ConfigureAwait(false);
                            }
                        };

                        await Task.WhenAll(parallel0(), parallel1()).ConfigureAwait(false);
                    }

                    await Task.Yield();

                    unfinishedSnapshot2 = transaction.GetTransactionSnapshot();
                }
            };

            action().Wait();

            var finishedSnapshot = transaction.GetTransactionSnapshot();

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
        public void LiveProfiler_Step_NullCategory_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Step(null, Name0, Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("category");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_EmptyCategory_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Step("    ", Name0, Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("category");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_NullName_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Step(Category0, null, Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("name");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_EmptyName_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Step(Category0, "    ", Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("name");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_EmptyTemplate_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Step(Category0, Name0, "    ");

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("template");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_NoTemplate_ResultsInTransactionWithoutTemplate()
        {
            ITransaction transaction;

            using (transaction = LiveProfiler.Instance.NewTransaction(Category0, Name0))
            {
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, null, TimeSpan.Zero, true, TimeSpan.Zero);
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_NullCategory_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.NewTransaction(null, Name0, Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("category");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_EmptyCategory_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.NewTransaction("    ", Name0, Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("category");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_NullName_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.NewTransaction(Category0, null, Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("name");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_EmptyName_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.NewTransaction(Category0, "    ", Template0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("name");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_EmptyTemplate_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.NewTransaction(Category0, Name0, "    ");

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("template");
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_NoTemplate_ResultsInTransactionWithoutTemplate()
        {
            ITransaction transaction;

            using (transaction = LiveProfiler.Instance.NewTransaction(Category0, Name0))
            {
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, null, TimeSpan.Zero, true, TimeSpan.Zero);
            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_TransactionAlreadyInflightAndForceNewIsFalse_Throws()
        {
            using (var transaction = LiveProfiler.Instance.NewTransaction(Category0, Name0))
            {
                CallContextHelper.GetCurrentStep().Should().BeSameAs(transaction);

                Action thrower = () => LiveProfiler.Instance.NewTransaction(Category1, Name1);
                thrower.ShouldThrow<InvalidOperationException>();

                CallContextHelper.GetCurrentStep().Should().BeSameAs(transaction);
            }
        }

        [Test]
        public void LiveProfiler_RegisterEventHandler_NullEventHandler_Throws()
        {
            Action thrower = () => LiveProfiler.Instance.RegisterEventHandler((IProfilerEventHandler)null);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("eventHandler");
        }

        [Test]
        public void LiveProfiler_RegisterEventHandler_NullAsyncEventHandler_Throws()
        {
            Action thrower = () => LiveProfiler.Instance.RegisterEventHandler((IProfilerEventHandlerAsync)null);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("eventHandler");
        }

        [Test]
        public void LiveProfiler_UnregisterEventHandler_NullEventHandler_Throws()
        {
            Action thrower = () => LiveProfiler.Instance.UnregisterEventHandler((IProfilerEventHandler)null);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("eventHandler");
        }

        [Test]
        public void LiveProfiler_UnregisterEventHandler_NullAsyncEventHandler_Throws()
        {
            Action thrower = () => LiveProfiler.Instance.UnregisterEventHandler((IProfilerEventHandlerAsync)null);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("eventHandler");
        }

        [Test]
        public void LiveProfiler_MultipleSimultaneousTransactionsInBothThreadPoolAndNonThreadPoolThreads_TransactionsDontLeakIntoEachOther()
        {
            var transactionSnapshots = new ConcurrentBag<TransactionSnapshot>();
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
                        ITransaction transaction;

                        using (transaction = LiveProfiler.Instance.NewTransaction(iStr, iStr, iStr))
                        {
                            await Task.Delay(10);

                            var childTasks = new List<Task>();

                            for (var j = 0; j < childStepCount; j++)
                            {
                                Func<Task> childAction = async () =>
                                {
                                    await Task.Yield();
                                    using (LiveProfiler.Instance.Step(iStr, iStr, iStr))
                                    {
                                        await Task.Delay(10);
                                    }
                                };
                                childTasks.Add(childAction());
                            }

                            await Task.WhenAll(childTasks);
                        }

                        transactionSnapshots.Add(transaction.GetTransactionSnapshot());
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
                            ITransaction transaction;

                            using (transaction = LiveProfiler.Instance.NewTransaction(iStr, iStr, iStr))
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
                                            using (LiveProfiler.Instance.Step(iStr, iStr, iStr))
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

                            transactionSnapshots.Add(transaction.GetTransactionSnapshot());
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
        public void LiveProfiler_ForceNewTransaction_ReplacesExistingTransaction()
        {
            ITransaction transaction0, transaction1;

            using (transaction0 = LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0))
            {
                Thread.Sleep(_duration0);
                using (transaction1 = LiveProfiler.Instance.NewTransaction(Category1, Name1, Template1, forceNew:true))
                {
                    Thread.Sleep(_duration1);
                    using (LiveProfiler.Instance.Step(Category2, Name2, Template2))
                    {
                        Thread.Sleep(_duration2);
                    }
                }
            }

            var finishedSnapshot0 = transaction0.GetTransactionSnapshot();
            var finishedSnapshot1 = transaction1.GetTransactionSnapshot();

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
        public void LiveProfiler_EventHandlersSpecified_RapidEventsInSingleTransaction_AllHandlersReceiveEventsInCorrectOrder()
        {
            // arrange
            const int stepCount = 100;
            var syncCompletionSource = new TaskCompletionSource<bool>();
            var asyncCompletionSource = new TaskCompletionSource<bool>();
            var syncHandler = new Mock<IProfilerEventHandler>();
            var asyncHandler = new Mock<IProfilerEventHandlerAsync>();

            var syncEvents = new List<IProfilerEvent>();
            var asyncEvents = new List<IProfilerEvent>();

            syncHandler.Setup(h => h.HandleEvent(It.IsAny<IProfilerEvent>()))
                .Callback<IProfilerEvent>(evt =>
                {
                    lock (syncEvents) syncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) syncCompletionSource.SetResult(true);
                });

            asyncHandler.Setup(h => h.HandleEventAsync(It.IsAny<IProfilerEvent>()))
                .Returns<IProfilerEvent>(evt =>
                {
                    lock (asyncEvents) asyncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) asyncCompletionSource.SetResult(true);
                    return Task.FromResult(true);
                });

            // execute
            try
            {
                LiveProfiler.Instance.RegisterEventHandler(syncHandler.Object);
                LiveProfiler.Instance.RegisterEventHandler(asyncHandler.Object);

                using (LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0, CorrelationId0))
                {
                    for (var i = 0; i < stepCount; i++)
                    {
                        var str = i.ToString();
                        using (LiveProfiler.Instance.Step(str, str, str))
                        {
                        }
                    }
                }

                syncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                asyncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

                // verify
                syncEvents.Should().HaveCount(stepCount * 2 + 2);

                var transactionStartEvent = syncEvents.First() as ITransactionStartEvent;
                transactionStartEvent.Should().NotBeNull();

                transactionStartEvent.Name.Should().Be(Name0);
                transactionStartEvent.Category.Should().Be(Category0);
                transactionStartEvent.Template.Should().Be(Template0);
                transactionStartEvent.Id.Should().NotBeEmpty();
                transactionStartEvent.Start.Should().BeCloseTo(DateTimeOffset.Now, 5000);
                transactionStartEvent.CorrelationId.Should().Be(CorrelationId0);
                transactionStartEvent.GetTransactionSnapshot().Should().NotBeNull();

                var childStepEvents = syncEvents.Skip(1).Take(stepCount * 2).ToArray();

                for (int i = 0; i < stepCount * 2; i += 2)
                {
                    var startEvent = childStepEvents[i] as IStepStartEvent;
                    var finishEvent = childStepEvents[i + 1] as IStepFinishEvent;

                    startEvent.Should().NotBeNull();
                    finishEvent.Should().NotBeNull();

                    var name = (i/2).ToString();

                    startEvent.Name.Should().Be(name);
                    startEvent.Category.Should().Be(name);
                    startEvent.Template.Should().Be(name);
                    startEvent.Id.Should().NotBeEmpty();
                    startEvent.ParentId.Should().Be(syncEvents.First().Id);
                    startEvent.RelativeStart.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
                    startEvent.Start.Should().BeCloseTo(DateTimeOffset.Now, 5000);
                    startEvent.GetTransactionSnapshot().Should().NotBeNull();

                    finishEvent.Duration.Should().NotBeNull();
                    finishEvent.Id.Should().NotBeEmpty();
                    finishEvent.GetTransactionSnapshot().Should().NotBeNull();
                }

                var transactionFinishEvent = syncEvents.Last() as ITransactionFinishEvent;

                transactionFinishEvent.Should().NotBeNull();

                transactionFinishEvent.Duration.Should().NotBeNull();
                transactionFinishEvent.Id.Should().NotBeEmpty();
                transactionFinishEvent.GetTransactionSnapshot().Should().NotBeNull();
            }
            finally
            {
                LiveProfiler.Instance.UnregisterEventHandler(syncHandler.Object);
                LiveProfiler.Instance.UnregisterEventHandler(asyncHandler.Object);
            }
        }

        [Test]
        public void LiveProfiler_EventHandlersThrowExceptions_ExceptionsAreSwallowedAndEventsContinueToBeDelivered()
        {
            // arrange
            var syncCompletionSource = new TaskCompletionSource<bool>();
            var asyncCompletionSource = new TaskCompletionSource<bool>();
            var syncHandler = new Mock<IProfilerEventHandler>();
            var asyncHandler = new Mock<IProfilerEventHandlerAsync>();

            syncHandler.Setup(h => h.HandleEvent(It.IsAny<IProfilerEvent>()))
                .Callback<IProfilerEvent>(evt =>
                {
                    if (evt is ITransactionStartEvent) throw new Exception();
                    if (evt is ITransactionFinishEvent) syncCompletionSource.SetResult(true);
                });

            asyncHandler.Setup(h => h.HandleEventAsync(It.IsAny<IProfilerEvent>()))
                .Returns<IProfilerEvent>(evt =>
                {
                    if (evt is ITransactionStartEvent) throw new Exception();
                    if (evt is ITransactionFinishEvent) asyncCompletionSource.SetResult(true);
                    return Task.FromResult(true);
                });

            // execute
            try
            {
                LiveProfiler.Instance.RegisterEventHandler(syncHandler.Object);
                LiveProfiler.Instance.RegisterEventHandler(asyncHandler.Object);

                using (LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0, CorrelationId0))
                {
                }

                // verify
                syncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                asyncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            }
            finally
            {
                LiveProfiler.Instance.UnregisterEventHandler(syncHandler.Object);
                LiveProfiler.Instance.UnregisterEventHandler(asyncHandler.Object);
            }
        }

        [Test]
        public void LiveProfiler_TransactionDisposedMultipleTimes_ShouldNotCauseMultipleEvents()
        {
            // arrange
            var syncCompletionSource = new TaskCompletionSource<bool>();
            var asyncCompletionSource = new TaskCompletionSource<bool>();
            var syncHandler = new Mock<IProfilerEventHandler>();
            var asyncHandler = new Mock<IProfilerEventHandlerAsync>();

            var syncEvents = new List<IProfilerEvent>();
            var asyncEvents = new List<IProfilerEvent>();

            syncHandler.Setup(h => h.HandleEvent(It.IsAny<IProfilerEvent>()))
                .Callback<IProfilerEvent>(evt =>
                {
                    lock (syncEvents) syncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) syncCompletionSource.SetResult(true);
                });

            asyncHandler.Setup(h => h.HandleEventAsync(It.IsAny<IProfilerEvent>()))
                .Returns<IProfilerEvent>(evt =>
                {
                    lock (asyncEvents) asyncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) asyncCompletionSource.SetResult(true);
                    return Task.FromResult(true);
                });

            // execute
            try
            {
                LiveProfiler.Instance.RegisterEventHandler(syncHandler.Object);
                LiveProfiler.Instance.RegisterEventHandler(asyncHandler.Object);

                ITransaction transaction;

                using (transaction = LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0))
                {
                }

                transaction.Dispose();

                syncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                asyncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

                // verify
                syncEvents.Should().HaveCount(2);

                var transactionStartEvent = syncEvents.First() as ITransactionStartEvent;
                transactionStartEvent.Should().NotBeNull();

                transactionStartEvent.Name.Should().Be(Name0);
                transactionStartEvent.Category.Should().Be(Category0);
                transactionStartEvent.Template.Should().Be(Template0);
                transactionStartEvent.Id.Should().NotBeEmpty();

                var transactionFinishEvent = syncEvents.Last() as ITransactionFinishEvent;

                transactionFinishEvent.Should().NotBeNull();

                transactionFinishEvent.Duration.Should().NotBeNull();
                transactionFinishEvent.Id.Should().NotBeEmpty();
            }
            finally
            {
                LiveProfiler.Instance.UnregisterEventHandler(syncHandler.Object);
                LiveProfiler.Instance.UnregisterEventHandler(asyncHandler.Object);
            }
        }

        [Test]
        public void LiveProfiler_StepDisposedMultipleTimes_ShouldNotCauseMultipleEvents()
        {
            // arrange
            var syncCompletionSource = new TaskCompletionSource<bool>();
            var asyncCompletionSource = new TaskCompletionSource<bool>();
            var syncHandler = new Mock<IProfilerEventHandler>();
            var asyncHandler = new Mock<IProfilerEventHandlerAsync>();

            var syncEvents = new List<IProfilerEvent>();
            var asyncEvents = new List<IProfilerEvent>();

            syncHandler.Setup(h => h.HandleEvent(It.IsAny<IProfilerEvent>()))
                .Callback<IProfilerEvent>(evt =>
                {
                    lock (syncEvents) syncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) syncCompletionSource.SetResult(true);
                });

            asyncHandler.Setup(h => h.HandleEventAsync(It.IsAny<IProfilerEvent>()))
                .Returns<IProfilerEvent>(evt =>
                {
                    lock (asyncEvents) asyncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) asyncCompletionSource.SetResult(true);
                    return Task.FromResult(true);
                });

            // execute
            try
            {
                LiveProfiler.Instance.RegisterEventHandler(syncHandler.Object);
                LiveProfiler.Instance.RegisterEventHandler(asyncHandler.Object);

                using (LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0))
                {
                    IStep step;
                    using (step = LiveProfiler.Instance.Step(Category1, Name1, Template1))
                    {
                    }

                    step.Dispose();
                }

                syncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                asyncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

                // verify
                syncEvents.Should().HaveCount(4);

                var transactionStartEvent = syncEvents.First() as ITransactionStartEvent;
                transactionStartEvent.Should().NotBeNull();

                transactionStartEvent.Name.Should().Be(Name0);
                transactionStartEvent.Category.Should().Be(Category0);
                transactionStartEvent.Template.Should().Be(Template0);
                transactionStartEvent.Id.Should().NotBeEmpty();

                var startEvent = syncEvents[1] as IStepStartEvent;
                var finishEvent = syncEvents[2] as IStepFinishEvent;

                startEvent.Should().NotBeNull();
                finishEvent.Should().NotBeNull();

                startEvent.Name.Should().Be(Name1);
                startEvent.Category.Should().Be(Category1);
                startEvent.Template.Should().Be(Template1);
                startEvent.Id.Should().NotBeEmpty();
                startEvent.ParentId.Should().Be(transactionStartEvent.Id);

                finishEvent.Duration.Should().NotBeNull();
                finishEvent.Id.Should().NotBeEmpty();

                var transactionFinishEvent = syncEvents.Last() as ITransactionFinishEvent;

                transactionFinishEvent.Should().NotBeNull();

                transactionFinishEvent.Duration.Should().NotBeNull();
                transactionFinishEvent.Id.Should().NotBeEmpty();
            }
            finally
            {
                LiveProfiler.Instance.UnregisterEventHandler(syncHandler.Object);
                LiveProfiler.Instance.UnregisterEventHandler(asyncHandler.Object);
            }
        }

        [Test]
        public void LiveProfiler_Step_NoCurrentTransaction_ReturnsNullStep()
        {
            CallContextHelper.GetCurrentStep().Should().BeNull();

            using (var step = LiveProfiler.Instance.Step(Category0, Name0, Template0))
            {
                step.Should().BeNull();
                CallContextHelper.GetCurrentStep().Should().BeNull();
            }

            CallContextHelper.GetCurrentStep().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_CurrentTransactionFinished_ReturnsNullStep()
        {
            ITransaction transaction;

            using (transaction = LiveProfiler.Instance.NewTransaction(Category0, Name0, Template0))
            {
            }

            transaction.GetTransactionSnapshot().IsFinished.Should().BeTrue();

            CallContextHelper.SetCurrentStep((Transaction)transaction);

            using (var step = LiveProfiler.Instance.Step(Category0, Name0, Template0))
            {
                step.Should().BeNull();
            }

            CallContextHelper.GetCurrentStep().Should().Be(transaction);
        }

        [Test]
        public void LiveProfiler_GetRecentTransactions_RequestAllAndNoTransactions_ReturnsEmpty()
        {
            LiveProfiler.Instance.GetRecentTransactions().Should().BeEmpty();
        }

        [Test]
        public void LiveProfiler_GetRecentTransactions_RequestAllAndTransactionsPresent_ReturnInDescendingOrder()
        {
            // arrange
            IList<ITransaction> finishedTransactions = new List<ITransaction>();

            for (var i = 0; i < 10; i++)
            {
                ITransaction transaction;
                using (transaction = LiveProfiler.Instance.NewTransaction(i.ToString(), i.ToString()))
                {
                    using (LiveProfiler.Instance.Step(i.ToString(), i.ToString()))
                    {
                    }
                }

                finishedTransactions.Add(transaction);
            }

            IList<ITransaction> inflightTransactions = new List<ITransaction>();

            for (var i = 0; i < 10; i++)
            {
                var transaction = LiveProfiler.Instance.NewTransaction(i.ToString(), i.ToString(), forceNew: true);
                LiveProfiler.Instance.Step(i.ToString(), i.ToString());

                inflightTransactions.Add(transaction);
            }

            // execute
            var result = LiveProfiler.Instance.GetRecentTransactions();

            // verify
            result.Select(t => t.Id)
                .Should().Equal(inflightTransactions.Reverse().Concat(finishedTransactions.Reverse()).Select(t => t.GetTransactionSnapshot().Id));
        }

        [Test]
        public void LiveProfiler_GetRecentTransactions_RequestInflightOnlyAndTransactionsPresent_ReturnInflightTransactionsOnlyInDescendingOrder()
        {
            // arrange
            IList<ITransaction> finishedTransactions = new List<ITransaction>();

            for (var i = 0; i < 10; i++)
            {
                ITransaction transaction;
                using (transaction = LiveProfiler.Instance.NewTransaction(i.ToString(), i.ToString()))
                {
                    using (LiveProfiler.Instance.Step(i.ToString(), i.ToString()))
                    {
                    }
                }

                finishedTransactions.Add(transaction);
            }

            IList<ITransaction> inflightTransactions = new List<ITransaction>();

            for (var i = 0; i < 10; i++)
            {
                var transaction = LiveProfiler.Instance.NewTransaction(i.ToString(), i.ToString(), forceNew: true);
                LiveProfiler.Instance.Step(i.ToString(), i.ToString());

                inflightTransactions.Add(transaction);
            }

            // execute
            var result = LiveProfiler.Instance.GetRecentTransactions(true);

            // verify
            result.Select(t => t.Id)
                .Should().Equal(inflightTransactions.Reverse().Select(t => t.GetTransactionSnapshot().Id));
        }

        [Test]
        public void LiveProfiler_GetRecentTransactions_RequestInflightOnlyAndAllTransactionsFinished_ReturnsEmpty()
        {
            // arrange
            for (var i = 0; i < 10; i++)
            {
                using (LiveProfiler.Instance.NewTransaction(i.ToString(), i.ToString()))
                {
                    using (LiveProfiler.Instance.Step(i.ToString(), i.ToString()))
                    {
                    }
                }
            }

            // execute
            var result = LiveProfiler.Instance.GetRecentTransactions(true);

            // verify
            result.Should().BeEmpty();
        }

        [Test]
        public void LiveProfiler_GetRecentTransactions_RecentTransactionsExceed100_ReturnsMostRecent100InDescendingOrder()
        {
            // arrange
            IList<ITransaction> finishedTransactions = new List<ITransaction>();

            for (var i = 0; i < 120; i++)
            {
                ITransaction transaction;
                using (transaction = LiveProfiler.Instance.NewTransaction(i.ToString(), i.ToString()))
                {
                    using (LiveProfiler.Instance.Step(i.ToString(), i.ToString()))
                    {
                    }
                }

                finishedTransactions.Add(transaction);
            }

            // execute
            var result = LiveProfiler.Instance.GetRecentTransactions();

            // verify
            result.Select(t => t.Id)
                .Should().Equal(finishedTransactions.Reverse().Take(100).Select(t => t.GetTransactionSnapshot().Id));
        }
    }
}
