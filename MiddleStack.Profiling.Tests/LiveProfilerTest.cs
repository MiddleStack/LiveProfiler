using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MiddleStack.Profiling.Events;
using MiddleStack.Profiling.Testing;
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
        private const string DisplayName0 = "5513BBB2A4AC48F6B64FD77FA7FA2E0C";
        private const string Parameter0 = "BAD44AA98E6B4E3E8EE42901DA141FA2";
        private const string CorrelationId0 = "68917BE3A0154A17A76299E32EEDACCB";
        private const string Result0 = "BAF4FD940CAB41BC911979A67DE48E57";
        private const string Category1 = "0200937234BF41EF9E0D7F5E68D481D5";
        private const string Name1 = "A86545B4C7B94D51BE567CA00AF190D0";
        private const string DisplayName1 = "D5B1F2F6AABB4A759A09DBC24F5C1010";
        private const string Parameter1 = "D5754E1AEA154B70A82057065B86D75D";
        private const string CorrelationId1 = "349BDEB2BAEF4E1BA169467C3992EDFC";
        private const string Result1 = "3C10A00962F04914916D4FFC2964B980";
        private const string Category2 = "A5EC6C4A674043DD84DE9BB71EC654E5";
        private const string Name2 = "FC5F289F04AE4B978BEE9C753E7221D2";
        private const string DisplayName2 = "AE6CDE0B14554F01AF558BDEECE8368C";
        private const string Parameter2 = "0566A7BCD84C4B509E23185671EDDFFF";
        private const string CorrelationId2 = "DE7E9A3B99B84D1F9926E24EDF02A8EC";
        private const string Result2 = "29CA31DDC9D04A0294762EDDA73165B4";
        private const string Category3 = "89065DF4A71E4605B4B17345BA813928";
        private const string Name3 = "4F79F002757646798792A8A461E8BF11";
        private const string DisplayName3 = "1A33DCADF27848AB9E68E0984A34FE47";
        private const string Parameter3 = "1188378A2EDA4807B2A9F65E886AAFA0";
        private const string CorrelationId3 = "91B968425C174D5B9BAEF775A3666A3A";
        private const string Result3 = "37F32BEA9FEE469D924A5FE5C9D39B1D";

        [SetUp]
        public void Initialize()
        {
            LiveProfiler.Instance.TestingReset();
        }

        [Test]
        public void LiveProfiler_OneSyncStep_ReturnsOneSnapshot()
        {
            ITiming transaction;
            TransactionSnapshot unfinishedSnapshot;

            using (transaction = LiveProfiler.Instance.Transaction(
                    Category0, Name0, DisplayName0, Parameter0))
            {
                Thread.Sleep(_duration0);
                unfinishedSnapshot = transaction.GetTransactionSnapshot();
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, _duration0);
            unfinishedSnapshot.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight);
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_CorrelationIdSpecified_ReturnsSnapshotWithCorrelationId()
        {
            ITiming transaction;

            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0, CorrelationId0))
            {
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, TimeSpan.Zero, correlationId:CorrelationId0);
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_TransactionDisposedWithUnfinishedSteps_ThrowsException()
        {
            Action thrower = () =>
            {
                using (LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0, CorrelationId0))
                {
                    Thread.Sleep(_duration0);
                    LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1);
                    Thread.Sleep(_duration1);
                    LiveProfiler.Instance.Step(Category2, Name2, DisplayName2, Parameter2);
                }
            };

            thrower.ShouldThrow<InvalidOperationException>();   
        }

        [Test]
        public void LiveProfiler_TransactionSuccessWithResult_MarksTransactionAsSuccessWithResult()
        {
            ITiming transaction;
            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
            {
                Thread.Sleep(_duration0);
                transaction.Success(Result0);
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, _duration0, result: Result0);
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_TransactionFailureWithResult_MarksTransactionAsFailureWithResult()
        {
            ITiming transaction;
            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
            {
                Thread.Sleep(_duration0);
                transaction.Failure(Result0);
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Failure, _duration0, result: Result0);
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_StepSuccessWithResult_MarksTransactionAsSuccessWithResult()
        {
            ITiming transaction;
            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
            {
                using (var step = LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1))
                {
                    Thread.Sleep(_duration0);
                    step.Success(Result0);
                }
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, _duration0, 1,
                children =>
                {
                    children[0].AssertChildlessStep(Category1, Name1, DisplayName1, Parameter1, TimeSpan.Zero, TransactionState.Success, _duration0, result: Result0);
                });

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_StepFailureWithResult_MarksTransactionAsFailureWithResult()
        {
            ITiming transaction;
            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
            {
                using (var step = LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1))
                {
                    Thread.Sleep(_duration0);
                    step.Failure(Result0);
                }
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, _duration0, 1,
                children =>
                {
                    children[0].AssertChildlessStep(Category1, Name1, DisplayName1, Parameter1, TimeSpan.Zero, TransactionState.Failure, _duration0, result: Result0);
                });

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_OneAsyncSyncStep_ReturnsOneSnapshot()
        {
            ITiming transaction = null;
            TransactionSnapshot unfinishedSnapshot = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
                {
                    await Task.Delay(_duration0);
                    unfinishedSnapshot = transaction.GetTransactionSnapshot();

                    await Task.Yield();
                }
            };

            action().Wait();

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, _duration0);
            unfinishedSnapshot.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight);
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_AsyncNestedSteps_ReturnsNestedSnapshots()
        {
            ITiming transaction = null;
            TransactionSnapshot unfinishedSnapshot0 = null;
            TransactionSnapshot unfinishedSnapshot1 = null;
            TransactionSnapshot unfinishedSnapshot2 = null;
            TransactionSnapshot unfinishedSnapshot3 = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
                {
                    await Task.Delay(_duration0);
                    unfinishedSnapshot0 = transaction.GetTransactionSnapshot();

                    using (LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1))
                    {
                        await Task.Delay(_duration1);
                        unfinishedSnapshot1 = transaction.GetTransactionSnapshot();

                        await Task.Yield();
                        using (LiveProfiler.Instance.Step(Category2, Name2, DisplayName2, Parameter2))
                        {
                            await Task.Delay(_duration2);
                            unfinishedSnapshot2 = transaction.GetTransactionSnapshot();
                        }

                        await Task.Yield();
                        using (LiveProfiler.Instance.Step(Category3, Name3, DisplayName3, Parameter3))
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
            finishedSnapshot.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, 
                _duration0 + _duration1 + _duration2 + _duration3,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Success, 
                        _duration1 + _duration2 + _duration3, 2,
                        c1 =>
                        {
                            var child2 = c1[0];
                            var child3 = c1[1];

                            child2.AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0 + _duration1, TransactionState.Success, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, DisplayName3, Parameter3, _duration0 + _duration1 + _duration2, TransactionState.Success, _duration3);
                        });
                });

            unfinishedSnapshot0.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight);

            unfinishedSnapshot1.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertChildlessStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Inflight);
                });

            unfinishedSnapshot2.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Inflight,
                        null, 1,
                        c1 =>
                        {
                            var child2 = c1[0];

                            child2.AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0 + _duration1, TransactionState.Inflight);
                        });
                });

            unfinishedSnapshot3.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Inflight,
                        null, 2,
                        c1 =>
                        {
                            var child2 = c1[0];
                            var child3 = c1[1];

                            child2.AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0 + _duration1, TransactionState.Success, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, DisplayName3, Parameter3, _duration0 + _duration1 + _duration2, TransactionState.Inflight);
                        });
                });

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_SyncNestedSteps_ReturnsNestedSnapshots()
        {
            ITiming transaction = null;
            TransactionSnapshot unfinishedSnapshot0 = null;
            TransactionSnapshot unfinishedSnapshot1 = null;
            TransactionSnapshot unfinishedSnapshot2 = null;
            TransactionSnapshot unfinishedSnapshot3 = null;

            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
            {
                Thread.Sleep(_duration0);
                unfinishedSnapshot0 = transaction.GetTransactionSnapshot();

                using (LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1))
                {
                    Thread.Sleep(_duration1);
                    unfinishedSnapshot1 = transaction.GetTransactionSnapshot();

                    using (LiveProfiler.Instance.Step(Category2, Name2, DisplayName2, Parameter2))
                    {
                        Thread.Sleep(_duration2);
                        unfinishedSnapshot2 = transaction.GetTransactionSnapshot();
                    }

                    using (LiveProfiler.Instance.Step(Category3, Name3, DisplayName3, Parameter3))
                    {
                        Thread.Sleep(_duration3);
                        unfinishedSnapshot3 = transaction.GetTransactionSnapshot();
                    }
                }
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success,
                _duration0 + _duration1 + _duration2 + _duration3,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Success,
                        _duration1 + _duration2 + _duration3, 2,
                        c1 =>
                        {
                            var child2 = c1[0];
                            var child3 = c1[1];

                            child2.AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0 + _duration1, TransactionState.Success, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, DisplayName3, Parameter3, _duration0 + _duration1 + _duration2, TransactionState.Success, _duration3);
                        });
                });

            unfinishedSnapshot0.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight);

            unfinishedSnapshot1.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertChildlessStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Inflight);
                });

            unfinishedSnapshot2.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Inflight,
                        null, 1,
                        c1 =>
                        {
                            var child2 = c1[0];

                            child2.AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0 + _duration1, TransactionState.Inflight);
                        });
                });

            unfinishedSnapshot3.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Inflight,
                        null, 2,
                        c1 =>
                        {
                            var child2 = c1[0];
                            var child3 = c1[1];

                            child2.AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0 + _duration1, TransactionState.Success, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, DisplayName3, Parameter3, _duration0 + _duration1 + _duration2, TransactionState.Inflight);
                        });
                });

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_AsyncParallelNestedSteps_ReturnsNestedSnapshots()
        {
            ITiming transaction = null;
            TransactionSnapshot unfinishedSnapshot0 = null;
            TransactionSnapshot unfinishedSnapshot1 = null;
            TransactionSnapshot unfinishedSnapshot2 = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
                {
                    await Task.Delay(_duration0);
                    unfinishedSnapshot0 = transaction.GetTransactionSnapshot();

                    using (LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1))
                    {
                        await Task.Delay(_duration1);
                        unfinishedSnapshot1 = transaction.GetTransactionSnapshot();

                        Func<Task> parallel0 = async () =>
                        {
                            await Task.Yield();
                            using (LiveProfiler.Instance.Step(Category2, Name2, DisplayName2, Parameter2))
                            {
                                await Task.Delay(_duration2);
                            }
                        };

                        Func<Task> parallel1 = async () =>
                        {
                            await Task.Yield();
                            using (LiveProfiler.Instance.Step(Category3, Name3, DisplayName3, Parameter3))
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
            finishedSnapshot.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, 
                _duration0 + _duration1 + _duration2,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Success, 
                        _duration1 + _duration2, 2,
                        c1 =>
                        {
                            var child2 = c1.First(c => c.Name == Name2);
                            var child3 = c1.First(c => c.Name == Name3);

                            child2.AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0 + _duration1, TransactionState.Success, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, DisplayName3, Parameter3, _duration0 + _duration1, TransactionState.Success, _duration3);
                        });
                });

            unfinishedSnapshot0.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight);

            unfinishedSnapshot1.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertChildlessStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Inflight);
                });

            unfinishedSnapshot2.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Success,
                        _duration1 + _duration2, 2,
                        c1 =>
                        {
                            var child2 = c1.First(c => c.Name == Name2);
                            var child3 = c1.First(c => c.Name == Name3);

                            child2.AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0 + _duration1, TransactionState.Success, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, DisplayName3, Parameter3, _duration0 + _duration1, TransactionState.Success, _duration3);
                        });
                });

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_AsyncParallelNestedStepsWithNoSyncContext_ReturnsNestedSnapshots()
        {
            ITiming transaction = null;
            TransactionSnapshot unfinishedSnapshot0 = null;
            TransactionSnapshot unfinishedSnapshot1 = null;
            TransactionSnapshot unfinishedSnapshot2 = null;

            Func<Task> action = async () =>
            {
                await Task.Yield();

                using (transaction = LiveProfiler.Instance.Transaction(
                        Category0, Name0, DisplayName0, Parameter0))
                {
                    await Task.Delay(_duration0).ConfigureAwait(false);
                    unfinishedSnapshot0 = transaction.GetTransactionSnapshot();

                    using (LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1))
                    {
                        await Task.Delay(_duration1).ConfigureAwait(false);
                        unfinishedSnapshot1 = transaction.GetTransactionSnapshot();

                        Func<Task> parallel0 = async () =>
                        {
                            await Task.Yield();
                            using (LiveProfiler.Instance.Step(Category2, Name2, DisplayName2, Parameter2))
                            {
                                await Task.Delay(_duration2).ConfigureAwait(false);
                            }
                        };

                        Func<Task> parallel1 = async () =>
                        {
                            await Task.Yield();
                            using (LiveProfiler.Instance.Step(Category3, Name3, DisplayName3, Parameter3))
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
            finishedSnapshot.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, 
                _duration0 + _duration1 + _duration2,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Success, 
                        _duration1 + _duration2, 2,
                        c1 =>
                        {
                            var child2 = c1.First(c => c.Name == Name2);
                            var child3 = c1.First(c => c.Name == Name3);

                            child2.AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0 + _duration1, TransactionState.Success, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, DisplayName3, Parameter3, _duration0 + _duration1, TransactionState.Success, _duration3);
                        });
                });

            unfinishedSnapshot0.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight);

            unfinishedSnapshot1.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertChildlessStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Inflight);
                });

            unfinishedSnapshot2.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Inflight,
                null,
                1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, _duration0, TransactionState.Success,
                        _duration1 + _duration2, 2,
                        c1 =>
                        {
                            var child2 = c1.First(c => c.Name == Name2);
                            var child3 = c1.First(c => c.Name == Name3);

                            child2.AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0 + _duration1, TransactionState.Success, _duration2);
                            child3.AssertChildlessStep(Category3, Name3, DisplayName3, Parameter3, _duration0 + _duration1, TransactionState.Success, _duration3);
                        });
                });

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_NullCategory_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Step(null, Name0, Parameter0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("category");
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_EmptyCategory_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Step("    ", Name0, Parameter0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("category");
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_NullName_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Step(Category0, null, Parameter0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("name");
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_EmptyName_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Step(Category0, "    ", Parameter0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("name");
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_NoTemplate_ResultsInTransactionWithoutTemplate()
        {
            ITiming transaction;

            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0))
            {
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, DisplayName0, null, TimeSpan.Zero, TransactionState.Success, TimeSpan.Zero);
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_PredicateReturnsTrue_CreatesStep()
        {
            ITiming transaction;

            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0))
            {
                using (LiveProfiler.Instance.Step(
                    Category1, Name1, DisplayName1, Parameter1,
                    c => c.CurrentTiming.Category == Category0
                    && c.CurrentTiming.Name == Name0
                    && c.CurrentTiming.DisplayName == DisplayName0
                    && c.CurrentTiming.Type == TimingType.Transaction))
                {
                    using (LiveProfiler.Instance.Step(
                        Category2, Name2, DisplayName2, Parameter2,
                        c => c.CurrentTiming.Category == Category1
                        && c.CurrentTiming.Name == Name1
                        && c.CurrentTiming.DisplayName == DisplayName1
                        && c.CurrentTiming.Type == TimingType.Step))
                    {
                    }
                }
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertStep(Category0, Name0, DisplayName0, null, TimeSpan.Zero, TransactionState.Success, TimeSpan.Zero, 
                1, c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, TimeSpan.Zero, TransactionState.Success, TimeSpan.Zero, 
                        1, c1 =>
                        {
                            c1[0].AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, TimeSpan.Zero, TransactionState.Success, TimeSpan.Zero);
                        });
                });
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_PredicateReturnsFalse_DoesNotCreateStep()
        {
            ITiming transaction;
            ITiming step;

            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0))
            {
                using (step = LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1, c => false))
                {
                }
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            step.Should().BeOfType<InertTiming>();
            finishedSnapshot.AssertChildlessStep(Category0, Name0, DisplayName0, null, TimeSpan.Zero, TransactionState.Success, TimeSpan.Zero);
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_NullCategory_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Transaction(null, Name0, Parameter0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("category");
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_EmptyCategory_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Transaction("    ", Name0, Parameter0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("category");
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_NullName_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Transaction(Category0, null, Parameter0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("name");
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_EmptyName_ThrowsArgumentNullException()
        {
            Action thrower = () => LiveProfiler.Instance.Transaction(Category0, "    ", Parameter0);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("name");
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_NoTemplate_ResultsInTransactionWithoutTemplate()
        {
            ITiming transaction;

            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0))
            {
            }

            var finishedSnapshot = transaction.GetTransactionSnapshot();

            // verify
            finishedSnapshot.AssertChildlessStep(Category0, Name0, DisplayName0, null, TimeSpan.Zero, TransactionState.Success, TimeSpan.Zero);
            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_TransactionAlreadyInflightAndForceNewIsFalse_Throws()
        {
            using (var transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0))
            {
                CallContextHelper.GetCurrentTiming().Should().BeSameAs(transaction);

                Action thrower = () => LiveProfiler.Instance.Transaction(Category1, Name1, DisplayName1);
                thrower.ShouldThrow<InvalidOperationException>();

                CallContextHelper.GetCurrentTiming().Should().BeSameAs(transaction);
            }
        }

        [Test]
        public void LiveProfiler_RegisterEventSubscriber_NullEventSubscriber_Throws()
        {
            Action thrower = () => LiveProfiler.Instance.RegisterEventSubscriber((IProfilerEventSubscriber)null);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("eventSubscriber");
        }

        [Test]
        public void LiveProfiler_RegisterEventSubscriber_NullAsyncEventSubscriber_Throws()
        {
            Action thrower = () => LiveProfiler.Instance.RegisterEventSubscriber((IProfilerEventSubscriberAsync)null);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("eventSubscriber");
        }

        [Test]
        public void LiveProfiler_UnregisterEventSubscriber_NullEventSubscriber_Throws()
        {
            Action thrower = () => LiveProfiler.Instance.UnregisterEventSubscriber((IProfilerEventSubscriber)null);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("eventSubscriber");
        }

        [Test]
        public void LiveProfiler_UnregisterEventSubscriber_NullAsyncEventSubscriber_Throws()
        {
            Action thrower = () => LiveProfiler.Instance.UnregisterEventSubscriber((IProfilerEventSubscriberAsync)null);

            thrower.ShouldThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("eventSubscriber");
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
                        ITiming transaction;

                        using (transaction = LiveProfiler.Instance.Transaction(iStr, iStr, iStr))
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
                            ITiming transaction;

                            using (transaction = LiveProfiler.Instance.Transaction(iStr, iStr, iStr))
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
        public void LiveProfiler_Transaction_TransactionAlreadyInFlightAndModeIsReplace_ReplacesExistingTransaction()
        {
            ITiming transaction0, transaction1;

            using (transaction0 = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
            {
                Thread.Sleep(_duration0);
                using (transaction1 = LiveProfiler.Instance.Transaction(Category1, Name1, DisplayName1, Parameter1, mode: TransactionMode.Replace))
                {
                    Thread.Sleep(_duration1);
                    using (LiveProfiler.Instance.Step(Category2, Name2, DisplayName2, Parameter2))
                    {
                        Thread.Sleep(_duration2);
                    }
                }
            }

            var finishedSnapshot0 = transaction0.GetTransactionSnapshot();
            var finishedSnapshot1 = transaction1.GetTransactionSnapshot();

            // verify
            finishedSnapshot0.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, _duration0 + _duration1 + _duration2);
            finishedSnapshot1.AssertStep(Category1, Name1, DisplayName1, Parameter1, TimeSpan.Zero, TransactionState.Success, _duration1 + _duration2, 1,
                c =>
                {
                    c[0].AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration1, TransactionState.Success, _duration2);
                });

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_TransactionNotInFlightAndModeIsReplace_CreatesNewTransaction()
        {
            ITiming transaction0, transaction1;

            using (transaction0 = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
            {
                Thread.Sleep(_duration0);
                transaction0.Dispose();
                using (transaction1 = LiveProfiler.Instance.Transaction(Category1, Name1, DisplayName1, Parameter1, mode: TransactionMode.Replace))
                {
                    Thread.Sleep(_duration1);
                    using (LiveProfiler.Instance.Step(Category2, Name2, DisplayName2, Parameter2))
                    {
                        Thread.Sleep(_duration2);
                    }
                }
            }

            var finishedSnapshot0 = transaction0.GetTransactionSnapshot();
            var finishedSnapshot1 = transaction1.GetTransactionSnapshot();

            // verify
            finishedSnapshot0.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, _duration0);
            finishedSnapshot1.AssertStep(Category1, Name1, DisplayName1, Parameter1, TimeSpan.Zero, TransactionState.Success, _duration1 + _duration2, 1,
                c =>
                {
                    c[0].AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration1, TransactionState.Success, _duration2);
                });

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_TransactionAlreadyInFlightAndModeIsStepOrTransaction_ReplacesExistingTransaction()
        {
            ITiming transaction0, transaction1;

            using (transaction0 = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
            {
                using (transaction1 = LiveProfiler.Instance.Transaction(Category1, Name1, DisplayName1, Parameter1, mode: TransactionMode.StepOrTransaction))
                {
                    Thread.Sleep(_duration0);
                    using (LiveProfiler.Instance.Step(Category2, Name2, DisplayName2, Parameter2))
                    {
                        Thread.Sleep(_duration1);
                    }
                }
            }

            var finishedSnapshot0 = transaction0.GetTransactionSnapshot();
            var finishedSnapshot1 = transaction1.GetTransactionSnapshot();

            // verify
            finishedSnapshot0.AssertStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, _duration0 + _duration1, 1,
                c0 =>
                {
                    c0[0].AssertStep(Category1, Name1, DisplayName1, Parameter1, TimeSpan.Zero, TransactionState.Success, _duration0 + _duration1, 1,
                        c1 =>
                        {
                            c1[0].AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0,
                                TransactionState.Success, _duration1);
                        });
                });

            finishedSnapshot0.ShouldBeEquivalentTo(finishedSnapshot1);

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Transaction_TransactionNotInFlightAndModeIsStepOrTransaction_CreatesNewTransaction()
        {
            ITiming transaction0, transaction1;

            using (transaction0 = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
            {
                transaction0.Success();
                using (transaction1 = LiveProfiler.Instance.Transaction(Category1, Name1, DisplayName1, Parameter1, mode: TransactionMode.StepOrTransaction))
                {
                    Thread.Sleep(_duration0);
                    using (LiveProfiler.Instance.Step(Category2, Name2, DisplayName2, Parameter2))
                    {
                        Thread.Sleep(_duration1);
                    }
                }
            }

            var finishedSnapshot0 = transaction0.GetTransactionSnapshot();
            var finishedSnapshot1 = transaction1.GetTransactionSnapshot();

            // verify
            finishedSnapshot0.AssertChildlessStep(Category0, Name0, DisplayName0, Parameter0, TimeSpan.Zero, TransactionState.Success, TimeSpan.Zero);
            finishedSnapshot1.AssertStep(Category1, Name1, DisplayName1, Parameter1, TimeSpan.Zero, TransactionState.Success, _duration0 + _duration1, 1,
                c =>
                {
                    c[0].AssertChildlessStep(Category2, Name2, DisplayName2, Parameter2, _duration0, TransactionState.Success, _duration1);
                });

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_EventSubscribersSpecified_RapidEventsInSingleTransaction_AllSubscribersReceiveEventsInCorrectOrder()
        {
            // arrange
            const int stepCount = 100;
            var syncCompletionSource = new TaskCompletionSource<bool>();
            var asyncCompletionSource = new TaskCompletionSource<bool>();
            var syncSubscriber = new Mock<IProfilerEventSubscriber>();
            var asyncSubscriber = new Mock<IProfilerEventSubscriberAsync>();

            var syncEvents = new List<IProfilerEvent>();
            var asyncEvents = new List<IProfilerEvent>();

            syncSubscriber.Setup(h => h.HandleEvent(It.IsAny<IProfilerEvent>()))
                .Callback<IProfilerEvent>(evt =>
                {
                    lock (syncEvents) syncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) syncCompletionSource.SetResult(true);
                });

            asyncSubscriber.Setup(h => h.HandleEventAsync(It.IsAny<IProfilerEvent>()))
                .Returns<IProfilerEvent>(evt =>
                {
                    lock (asyncEvents) asyncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) asyncCompletionSource.SetResult(true);
                    return Task.FromResult(true);
                });

            // execute
            try
            {
                LiveProfiler.Instance.RegisterEventSubscriber(syncSubscriber.Object);
                LiveProfiler.Instance.RegisterEventSubscriber(asyncSubscriber.Object);

                syncSubscriber.Verify(s => s.Start());
                asyncSubscriber.Verify(s => s.Start());

                using (var transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0, CorrelationId0))
                {
                    for (var i = 0; i < stepCount; i++)
                    {
                        var str = i.ToString();
                        using (var step = LiveProfiler.Instance.Step(str, str, str, str))
                        {
                            Thread.Sleep(10);
                            step.Failure(str);
                        }
                    }
                    transaction.Success(Result0);
                }

                syncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                asyncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

                // verify
                syncEvents.Should().HaveCount(stepCount * 2 + 2);

                var transactionStartEvent = syncEvents.First() as ITransactionStartEvent;
                transactionStartEvent.Should().NotBeNull();

                transactionStartEvent.Name.Should().Be(Name0);
                transactionStartEvent.Category.Should().Be(Category0);
                transactionStartEvent.DisplayName.Should().Be(DisplayName0);
                transactionStartEvent.Parameters.Should().Be(Parameter0);
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
                    startEvent.DisplayName.Should().Be(name);
                    startEvent.Category.Should().Be(name);
                    startEvent.Parameters.Should().Be(name);
                    startEvent.Id.Should().NotBeEmpty();
                    startEvent.ParentId.Should().Be(syncEvents.First().Id);
                    startEvent.RelativeStart.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
                    startEvent.Start.Should().BeCloseTo(DateTimeOffset.Now, 5000);
                    startEvent.GetTransactionSnapshot().Should().NotBeNull();

                    finishEvent.Name.Should().Be(name);
                    finishEvent.DisplayName.Should().Be(name);
                    finishEvent.Category.Should().Be(name);
                    finishEvent.Parameters.Should().Be(name);
                    finishEvent.Result.Should().Be(name);
                    finishEvent.Id.Should().NotBeEmpty();
                    finishEvent.IsSuccess.Should().BeFalse();
                    finishEvent.GetTransactionSnapshot().Should().NotBeNull();
                    finishEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
                }

                var transactionFinishEvent = syncEvents.Last() as ITransactionFinishEvent;

                transactionFinishEvent.Should().NotBeNull();

                transactionFinishEvent.Name.Should().Be(Name0);
                transactionFinishEvent.DisplayName.Should().Be(DisplayName0);
                transactionFinishEvent.Category.Should().Be(Category0);
                transactionFinishEvent.Parameters.Should().Be(Parameter0);
                transactionFinishEvent.Result.Should().Be(Result0);
                transactionFinishEvent.Id.Should().NotBeEmpty();
                transactionFinishEvent.IsSuccess.Should().BeTrue();
                transactionFinishEvent.CorrelationId.Should().Be(CorrelationId0);
                transactionFinishEvent.GetTransactionSnapshot().Should().NotBeNull();
                transactionFinishEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            }
            finally
            {
                LiveProfiler.Instance.UnregisterEventSubscriber(syncSubscriber.Object);
                LiveProfiler.Instance.UnregisterEventSubscriber(asyncSubscriber.Object);
            }

            syncSubscriber.Verify(s => s.Stop());
            asyncSubscriber.Verify(s => s.Stop());
        }

        [Test]
        public void LiveProfiler_EventSubscribersThrowExceptions_ExceptionsAreSwallowedAndEventsContinueToBeDelivered()
        {
            // arrange
            var syncCompletionSource = new TaskCompletionSource<bool>();
            var asyncCompletionSource = new TaskCompletionSource<bool>();
            var syncSubscriber = new Mock<IProfilerEventSubscriber>();
            var asyncSubscriber = new Mock<IProfilerEventSubscriberAsync>();

            syncSubscriber.Setup(h => h.HandleEvent(It.IsAny<IProfilerEvent>()))
                .Callback<IProfilerEvent>(evt =>
                {
                    if (evt is ITransactionStartEvent) throw new Exception();
                    if (evt is ITransactionFinishEvent) syncCompletionSource.SetResult(true);
                });

            asyncSubscriber.Setup(h => h.HandleEventAsync(It.IsAny<IProfilerEvent>()))
                .Returns<IProfilerEvent>(evt =>
                {
                    if (evt is ITransactionStartEvent) throw new Exception();
                    if (evt is ITransactionFinishEvent) asyncCompletionSource.SetResult(true);
                    return Task.FromResult(true);
                });

            // execute
            try
            {
                LiveProfiler.Instance.RegisterEventSubscriber(syncSubscriber.Object);
                LiveProfiler.Instance.RegisterEventSubscriber(asyncSubscriber.Object);

                using (LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0, CorrelationId0))
                {
                }

                // verify
                syncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                asyncCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            }
            finally
            {
                LiveProfiler.Instance.UnregisterEventSubscriber(syncSubscriber.Object);
                LiveProfiler.Instance.UnregisterEventSubscriber(asyncSubscriber.Object);
            }
        }

        [Test]
        public void LiveProfiler_TransactionDisposedMultipleTimes_ShouldNotCauseMultipleEvents()
        {
            // arrange
            var syncCompletionSource = new TaskCompletionSource<bool>();
            var asyncCompletionSource = new TaskCompletionSource<bool>();
            var syncSubscriber = new Mock<IProfilerEventSubscriber>();
            var asyncSubscriber = new Mock<IProfilerEventSubscriberAsync>();

            var syncEvents = new List<IProfilerEvent>();
            var asyncEvents = new List<IProfilerEvent>();

            syncSubscriber.Setup(h => h.HandleEvent(It.IsAny<IProfilerEvent>()))
                .Callback<IProfilerEvent>(evt =>
                {
                    lock (syncEvents) syncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) syncCompletionSource.SetResult(true);
                });

            asyncSubscriber.Setup(h => h.HandleEventAsync(It.IsAny<IProfilerEvent>()))
                .Returns<IProfilerEvent>(evt =>
                {
                    lock (asyncEvents) asyncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) asyncCompletionSource.SetResult(true);
                    return Task.FromResult(true);
                });

            // execute
            try
            {
                LiveProfiler.Instance.RegisterEventSubscriber(syncSubscriber.Object);
                LiveProfiler.Instance.RegisterEventSubscriber(asyncSubscriber.Object);

                ITiming transaction;

                using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
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
                transactionStartEvent.Parameters.Should().Be(Parameter0);
                transactionStartEvent.Id.Should().NotBeEmpty();

                var transactionFinishEvent = syncEvents.Last() as ITransactionFinishEvent;

                transactionFinishEvent.Should().NotBeNull();

                transactionFinishEvent.Id.Should().NotBeEmpty();
                transactionFinishEvent.IsSuccess.Should().BeTrue();
                transactionFinishEvent.Result.Should().BeNull();
            }
            finally
            {
                LiveProfiler.Instance.UnregisterEventSubscriber(syncSubscriber.Object);
                LiveProfiler.Instance.UnregisterEventSubscriber(asyncSubscriber.Object);
            }
        }

        [Test]
        public void LiveProfiler_StepDisposedMultipleTimes_ShouldNotCauseMultipleEvents()
        {
            // arrange
            var syncCompletionSource = new TaskCompletionSource<bool>();
            var asyncCompletionSource = new TaskCompletionSource<bool>();
            var syncSubscriber = new Mock<IProfilerEventSubscriber>();
            var asyncSubscriber = new Mock<IProfilerEventSubscriberAsync>();

            var syncEvents = new List<IProfilerEvent>();
            var asyncEvents = new List<IProfilerEvent>();

            syncSubscriber.Setup(h => h.HandleEvent(It.IsAny<IProfilerEvent>()))
                .Callback<IProfilerEvent>(evt =>
                {
                    lock (syncEvents) syncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) syncCompletionSource.SetResult(true);
                });

            asyncSubscriber.Setup(h => h.HandleEventAsync(It.IsAny<IProfilerEvent>()))
                .Returns<IProfilerEvent>(evt =>
                {
                    lock (asyncEvents) asyncEvents.Add(evt);
                    if (evt is ITransactionFinishEvent) asyncCompletionSource.SetResult(true);
                    return Task.FromResult(true);
                });

            // execute
            try
            {
                LiveProfiler.Instance.RegisterEventSubscriber(syncSubscriber.Object);
                LiveProfiler.Instance.RegisterEventSubscriber(asyncSubscriber.Object);

                using (LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
                {
                    ITiming step;
                    using (step = LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1))
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
                transactionStartEvent.Parameters.Should().Be(Parameter0);
                transactionStartEvent.Id.Should().NotBeEmpty();

                var startEvent = syncEvents[1] as IStepStartEvent;
                var finishEvent = syncEvents[2] as IStepFinishEvent;

                startEvent.Should().NotBeNull();
                finishEvent.Should().NotBeNull();

                startEvent.Name.Should().Be(Name1);
                startEvent.Category.Should().Be(Category1);
                startEvent.Parameters.Should().Be(Parameter1);
                startEvent.Id.Should().NotBeEmpty();
                startEvent.ParentId.Should().Be(transactionStartEvent.Id);

                finishEvent.IsSuccess.Should().BeTrue();
                finishEvent.Result.Should().BeNull();
                finishEvent.Id.Should().NotBeEmpty();

                var transactionFinishEvent = syncEvents.Last() as ITransactionFinishEvent;

                transactionFinishEvent.Should().NotBeNull();

                transactionFinishEvent.IsSuccess.Should().BeTrue();
                transactionFinishEvent.Result.Should().BeNull();
                transactionFinishEvent.Id.Should().NotBeEmpty();
            }
            finally
            {
                LiveProfiler.Instance.UnregisterEventSubscriber(syncSubscriber.Object);
                LiveProfiler.Instance.UnregisterEventSubscriber(asyncSubscriber.Object);
            }
        }

        [Test]
        public void LiveProfiler_Step_NoCurrentTransaction_ReturnsInertStep()
        {
            CallContextHelper.GetCurrentTiming().Should().BeNull();

            using (var step = LiveProfiler.Instance.Step(Category0, Name0, DisplayName0, Parameter0))
            {
                step.Should().BeOfType<InertTiming>();
                CallContextHelper.GetCurrentTiming().Should().BeNull();
            }

            CallContextHelper.GetCurrentTiming().Should().BeNull();
        }

        [Test]
        public void LiveProfiler_Step_CurrentTransactionFinished_ReturnsInertStep()
        {
            ITiming transaction;

            using (transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0))
            {
            }

            transaction.GetTransactionSnapshot().State.Should().Be(TransactionState.Success);

            CallContextHelper.SetCurrentStep((Transaction)transaction);

            using (var step = LiveProfiler.Instance.Step(Category0, Name0, DisplayName0, Parameter0))
            {
                step.Should().BeOfType<InertTiming>();
            }

            CallContextHelper.GetCurrentTiming().Should().Be(transaction);
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
            IList<ITiming> finishedTransactions = new List<ITiming>();

            for (var i = 0; i < 10; i++)
            {
                ITiming transaction;
                using (transaction = LiveProfiler.Instance.Transaction(i.ToString(), i.ToString()))
                {
                    using (LiveProfiler.Instance.Step(i.ToString(), i.ToString()))
                    {
                    }
                }

                finishedTransactions.Add(transaction);
            }

            IList<ITiming> inflightTransactions = new List<ITiming>();

            for (var i = 0; i < 10; i++)
            {
                var transaction = LiveProfiler.Instance.Transaction(i.ToString(), i.ToString(), mode: TransactionMode.Replace);
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
            IList<ITiming> finishedTransactions = new List<ITiming>();

            for (var i = 0; i < 10; i++)
            {
                ITiming transaction;
                using (transaction = LiveProfiler.Instance.Transaction(i.ToString(), i.ToString()))
                {
                    using (LiveProfiler.Instance.Step(i.ToString(), i.ToString()))
                    {
                    }
                }

                finishedTransactions.Add(transaction);
            }

            IList<ITiming> inflightTransactions = new List<ITiming>();

            for (var i = 0; i < 10; i++)
            {
                var transaction = LiveProfiler.Instance.Transaction(i.ToString(), i.ToString(), mode: TransactionMode.Replace);
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
                using (LiveProfiler.Instance.Transaction(i.ToString(), i.ToString()))
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
            IList<ITiming> finishedTransactions = new List<ITiming>();

            for (var i = 0; i < 120; i++)
            {
                ITiming transaction;
                using (transaction = LiveProfiler.Instance.Transaction(i.ToString(), i.ToString()))
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
