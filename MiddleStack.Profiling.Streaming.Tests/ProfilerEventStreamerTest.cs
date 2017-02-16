using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using MiddleStack.Profiling.Events;
using MiddleStack.Profiling.StreamingServer;
using MiddleStack.Profiling.Testing;
using MiddleStack.Testing;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Owin;

namespace MiddleStack.Profiling.Streaming.Tests
{
    [TestFixture]
    public class ProfilerEventStreamerTest
    {
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
        private IDisposable _host;
        private string _baseUrl;
        private const string AppName = "F9B35F32C2C348F386AFA238D4DF594D";
        private const string HostName = "D171CACE96CD4FFF8C0BD7E1431A44DC";

        [SetUp]
        public void Initialize()
        {
            var port = IPTestingUtilities.FindAvailableTcpPort();
            _baseUrl = $"http://localhost:{port}";

            _host = WebApp.Start(_baseUrl, app =>
            {
                app.UseCors(CorsOptions.AllowAll);
                app.Use<WebMiddleware>();
                app.MapSignalR();
            });
        }

        [TearDown]
        public void Cleanup()
        {
            _host?.Dispose();
            LiveProfiler.Instance.TestingReset();
        }

        [Test]
        public void ProfilerEventStreamer_ConsumerConnectsFirstAndThenEventSource_ConsumerReceivesMessages()
        {
            var messageList = new List<dynamic>();
            const int expectedMessageCount = 8;
            int messageCount = 0;
            var completion = new TaskCompletionSource<bool>();

            using (var hubConnection = new HubConnection(_baseUrl))
            {
                // Consumer connection
                var hubProxy = hubConnection.CreateHubProxy("EventConsumerHub");
                hubProxy.On("event", message =>
                {
                    messageList.Add(message);
                    if (Interlocked.Increment(ref messageCount) == expectedMessageCount)
                    {
                        completion.SetResult(true);
                    }
                });
                hubConnection.Start().Wait();

                // Event source connection
                var streamer = new ProfilerEventStreamer(new StreamingConfiguration
                {
                    AppName = AppName,
                    HostName = HostName,
                    ServerUrl = new Uri(_baseUrl)
                });

                LiveProfiler.Instance.RegisterEventSubscriber(streamer);    

                try
                {
                    // Transactions
                    using (var transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0, CorrelationId0))
                    {
                        using (var step = LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1))
                        {
                            step.Failure(Result1);
                        }

                        transaction.Success(Result0);
                    }

                    using (var transaction = LiveProfiler.Instance.Transaction(Category2, Name2, DisplayName2, Parameter2, CorrelationId2))
                    {
                        using (var step = LiveProfiler.Instance.Step(Category3, Name3, DisplayName3, Parameter3))
                        {
                            step.Failure(Result3);
                        }

                        transaction.Success(Result2);
                    }

                    // Wait for the expected events to come in.
                    completion.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

                    // Verify messages
                    messageList.Should().HaveCount(expectedMessageCount);

                    ((string)messageList[0].type).Should().Be(ProfilerEventType.TransactionStart.ToString());
                    ((string)messageList[0].category).Should().Be(Category0);
                    ((string)messageList[0].name).Should().Be(Name0);
                    ((string)messageList[0].displayName).Should().Be(DisplayName0);
                    ((string)messageList[0].parameters).Should().Be(Parameter0);
                    ((string)messageList[0].id).Should().NotBeNull();
                    ((DateTimeOffset?) messageList[0].start).Should().NotBeNull();

                    ((string)messageList[1].type).Should().Be(ProfilerEventType.StepStart.ToString());
                    ((string)messageList[1].category).Should().Be(Category1);
                    ((string)messageList[1].name).Should().Be(Name1);
                    ((string)messageList[1].displayName).Should().Be(DisplayName1);
                    ((string)messageList[1].parameters).Should().Be(Parameter1);
                    ((string)messageList[1].id).Should().NotBeNull();
                    ((string)messageList[1].parentId).Should().Be((string) messageList[0].id);
                    ((string)messageList[1].relativeStart).Should().NotBeNull();
                    ((DateTimeOffset?)messageList[1].start).Should().NotBeNull();

                    ((string)messageList[2].type).Should().Be(ProfilerEventType.StepFinish.ToString());
                    ((string)messageList[2].id).Should().Be((string)messageList[1].id);
                    ((string)messageList[2].duration).Should().NotBeNull();
                    ((string)messageList[2].result).Should().Be(Result1);
                    ((bool)messageList[2].isSuccess).Should().Be(false);

                    ((string)messageList[3].type).Should().Be(ProfilerEventType.TransactionFinish.ToString());
                    ((string)messageList[3].id).Should().Be((string)messageList[0].id);
                    ((string)messageList[3].duration).Should().NotBeNull();
                    ((string)messageList[3].result).Should().Be(Result0);
                    ((bool)messageList[3].isSuccess).Should().Be(true);

                    ((string)messageList[4].type).Should().Be(ProfilerEventType.TransactionStart.ToString());
                    ((string)messageList[4].category).Should().Be(Category2);
                    ((string)messageList[4].name).Should().Be(Name2);
                    ((string)messageList[4].displayName).Should().Be(DisplayName2);
                    ((string)messageList[4].parameters).Should().Be(Parameter2);
                    ((string)messageList[4].id).Should().NotBeNull();
                    ((DateTimeOffset?)messageList[4].start).Should().NotBeNull();

                    ((string)messageList[5].type).Should().Be(ProfilerEventType.StepStart.ToString());
                    ((string)messageList[5].category).Should().Be(Category3);
                    ((string)messageList[5].name).Should().Be(Name3);
                    ((string)messageList[5].displayName).Should().Be(DisplayName3);
                    ((string)messageList[5].parameters).Should().Be(Parameter3);
                    ((string)messageList[5].id).Should().NotBeNull();
                    ((string)messageList[5].parentId).Should().Be((string)messageList[4].id);
                    ((string)messageList[5].relativeStart).Should().NotBeNull();
                    ((DateTimeOffset?)messageList[5].start).Should().NotBeNull();

                    ((string)messageList[6].type).Should().Be(ProfilerEventType.StepFinish.ToString());
                    ((string)messageList[6].id).Should().Be((string)messageList[5].id);
                    ((string)messageList[6].duration).Should().NotBeNull();
                    ((string)messageList[6].result).Should().Be(Result3);
                    ((bool)messageList[6].isSuccess).Should().Be(false);

                    ((string)messageList[7].type).Should().Be(ProfilerEventType.TransactionFinish.ToString());
                    ((string)messageList[7].id).Should().Be((string)messageList[4].id);
                    ((string)messageList[7].duration).Should().NotBeNull();
                    ((string)messageList[7].result).Should().Be(Result2);
                    ((bool)messageList[7].isSuccess).Should().Be(true);
                }
                finally
                {
                    LiveProfiler.Instance.UnregisterEventSubscriber(streamer);
                }

            }
        }

        [Test]
        public void ProfilerEventStreamer_EventSourceConnectsFirstAndThenConsumer_ConsumerReceivesMessages()
        {
            var messageList = new List<dynamic>();
            const int expectedMessageCount = 8;
            int messageCount = 0;
            var completion = new TaskCompletionSource<bool>();

            using (var hubConnection = new HubConnection(_baseUrl))
            {
                // Event source connection
                var streamer = new ProfilerEventStreamer(new StreamingConfiguration
                {
                    AppName = AppName,
                    HostName = HostName,
                    ServerUrl = new Uri(_baseUrl)
                });

                LiveProfiler.Instance.RegisterEventSubscriber(streamer);

                // Consumer connection
                var hubProxy = hubConnection.CreateHubProxy("EventConsumerHub");
                hubProxy.On("event", message =>
                {
                    messageList.Add(message);
                    if (Interlocked.Increment(ref messageCount) == expectedMessageCount)
                    {
                        completion.SetResult(true);
                    }
                });
                hubConnection.Start().Wait();

                try
                {
                    // Transactions
                    using (var transaction = LiveProfiler.Instance.Transaction(Category0, Name0, DisplayName0, Parameter0, CorrelationId0))
                    {
                        using (var step = LiveProfiler.Instance.Step(Category1, Name1, DisplayName1, Parameter1))
                        {
                            step.Failure(Result1);
                        }

                        transaction.Success(Result0);
                    }

                    using (var transaction = LiveProfiler.Instance.Transaction(Category2, Name2, DisplayName2, Parameter2, CorrelationId2))
                    {
                        using (var step = LiveProfiler.Instance.Step(Category3, Name3, DisplayName3, Parameter3))
                        {
                            step.Failure(Result3);
                        }

                        transaction.Success(Result2);
                    }

                    // Wait for the expected events to come in.
                    completion.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

                    // Verify messages
                    messageList.Should().HaveCount(expectedMessageCount);

                    ((string)messageList[0].type).Should().Be(ProfilerEventType.TransactionStart.ToString());
                    ((string)messageList[0].category).Should().Be(Category0);
                    ((string)messageList[0].name).Should().Be(Name0);
                    ((string)messageList[0].displayName).Should().Be(DisplayName0);
                    ((string)messageList[0].parameters).Should().Be(Parameter0);
                    ((string)messageList[0].id).Should().NotBeNull();
                    ((DateTimeOffset?) messageList[0].start).Should().NotBeNull();

                    ((string)messageList[1].type).Should().Be(ProfilerEventType.StepStart.ToString());
                    ((string)messageList[1].category).Should().Be(Category1);
                    ((string)messageList[1].name).Should().Be(Name1);
                    ((string)messageList[1].displayName).Should().Be(DisplayName1);
                    ((string)messageList[1].parameters).Should().Be(Parameter1);
                    ((string)messageList[1].id).Should().NotBeNull();
                    ((string)messageList[1].parentId).Should().Be((string) messageList[0].id);
                    ((string)messageList[1].relativeStart).Should().NotBeNull();
                    ((DateTimeOffset?)messageList[1].start).Should().NotBeNull();

                    ((string)messageList[2].type).Should().Be(ProfilerEventType.StepFinish.ToString());
                    ((string)messageList[2].id).Should().Be((string)messageList[1].id);
                    ((string)messageList[2].duration).Should().NotBeNull();
                    ((string)messageList[2].result).Should().Be(Result1);
                    ((bool)messageList[2].isSuccess).Should().Be(false);

                    ((string)messageList[3].type).Should().Be(ProfilerEventType.TransactionFinish.ToString());
                    ((string)messageList[3].id).Should().Be((string)messageList[0].id);
                    ((string)messageList[3].duration).Should().NotBeNull();
                    ((string)messageList[3].result).Should().Be(Result0);
                    ((bool)messageList[3].isSuccess).Should().Be(true);

                    ((string)messageList[4].type).Should().Be(ProfilerEventType.TransactionStart.ToString());
                    ((string)messageList[4].category).Should().Be(Category2);
                    ((string)messageList[4].name).Should().Be(Name2);
                    ((string)messageList[4].displayName).Should().Be(DisplayName2);
                    ((string)messageList[4].parameters).Should().Be(Parameter2);
                    ((string)messageList[4].id).Should().NotBeNull();
                    ((DateTimeOffset?)messageList[4].start).Should().NotBeNull();

                    ((string)messageList[5].type).Should().Be(ProfilerEventType.StepStart.ToString());
                    ((string)messageList[5].category).Should().Be(Category3);
                    ((string)messageList[5].name).Should().Be(Name3);
                    ((string)messageList[5].displayName).Should().Be(DisplayName3);
                    ((string)messageList[5].parameters).Should().Be(Parameter3);
                    ((string)messageList[5].id).Should().NotBeNull();
                    ((string)messageList[5].parentId).Should().Be((string)messageList[4].id);
                    ((string)messageList[5].relativeStart).Should().NotBeNull();
                    ((DateTimeOffset?)messageList[5].start).Should().NotBeNull();

                    ((string)messageList[6].type).Should().Be(ProfilerEventType.StepFinish.ToString());
                    ((string)messageList[6].id).Should().Be((string)messageList[5].id);
                    ((string)messageList[6].duration).Should().NotBeNull();
                    ((string)messageList[6].result).Should().Be(Result3);
                    ((bool)messageList[6].isSuccess).Should().Be(false);

                    ((string)messageList[7].type).Should().Be(ProfilerEventType.TransactionFinish.ToString());
                    ((string)messageList[7].id).Should().Be((string)messageList[4].id);
                    ((string)messageList[7].duration).Should().NotBeNull();
                    ((string)messageList[7].result).Should().Be(Result2);
                    ((bool)messageList[7].isSuccess).Should().Be(true);
                }
                finally
                {
                    LiveProfiler.Instance.UnregisterEventSubscriber(streamer);
                }

            }
        }

        [Test]
        public void ProfilerEventStreamer_RapidEventFiring_MessagesAreAlwaysDeliveredInOrder()
        {
            var messageList = new List<dynamic>();
            var transactionCount = 1000;
            var expectedMessageCount = transactionCount*4;
            int messageCount = 0;
            var completion = new TaskCompletionSource<bool>();

            using (var hubConnection = new HubConnection(_baseUrl))
            {
                // Consumer connection
                var hubProxy = hubConnection.CreateHubProxy("EventConsumerHub");
                hubProxy.On("event", message =>
                {
                    messageList.Add(message);
                    if (Interlocked.Increment(ref messageCount) == expectedMessageCount)
                    {
                        completion.SetResult(true);
                    }
                });
                hubConnection.Start().Wait();

                // Event source connection
                var streamer = new ProfilerEventStreamer(new StreamingConfiguration
                {
                    AppName = AppName,
                    HostName = HostName,
                    ServerUrl = new Uri(_baseUrl)
                });

                LiveProfiler.Instance.RegisterEventSubscriber(streamer);

                try
                {
                    // Transactions
                    Parallel.For(0, transactionCount, i =>
                    {
                        using (var transaction = LiveProfiler.Instance.Transaction(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 
                            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()))
                        {
                            using (var step = LiveProfiler.Instance.Step(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 
                                Guid.NewGuid().ToString(), Guid.NewGuid().ToString()))
                            {
                                step.Failure(Guid.NewGuid().ToString());
                            }

                            transaction.Success(Guid.NewGuid().ToString());
                        }
                    });

                    // Wait for the expected events to come in.
                    completion.Task.Wait(TimeSpan.FromSeconds(30)).Should().BeTrue();

                    // Verify messages
                    messageList.Should().HaveCount(expectedMessageCount);

                    for (var i = 0; i < messageList.Count; i++)
                    {
                        var message = messageList[i];

                        if (((string)message.type).Contains("Start"))
                        {
                            var finishMessageIndex = messageList.FindIndex(m => (string)m.id == (string)message.id && ((string)m.type).Contains("Finish"));

                            finishMessageIndex.Should().BeGreaterThan(i);
                        }
                    }
                }
                finally
                {
                    LiveProfiler.Instance.UnregisterEventSubscriber(streamer);
                }

            }
        }
    }
}
