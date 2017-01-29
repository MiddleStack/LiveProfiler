using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Owin.Hosting;
using MiddleStack.Profiling.Events;
using MiddleStack.Testing;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Owin;

namespace MiddleStack.Profiling.Nancy.Tests
{
    [TestFixture]
    public class NancyProfilingTest
    {
        private const string CorrelationId = "63327CF8B70C43BB8ABC3F31D6642E8A";
        private Mock<IProfilerEventSubscriber> _eventSubscriber;
        private ConcurrentBag<TransactionSnapshot> _transactions;
        private HttpClient _httpClient;
        private IDisposable _server;
        private string _urlBase;
        private TaskCompletionSource<bool> _transactionCompletionSource;

        [SetUp]
        public void Setup()
        {
            var basePath = "/" + Guid.NewGuid().ToString("n");

            _transactionCompletionSource = new TaskCompletionSource<bool>();

            _eventSubscriber = new Mock<IProfilerEventSubscriber>();
            _transactions = new ConcurrentBag<TransactionSnapshot>();

            _eventSubscriber.Setup(s => s.HandleEvent(It.IsNotNull<IProfilerEvent>())).Callback<IProfilerEvent>(
                evt =>
                {
                    var transactionFinished = evt as ITransactionFinishEvent;

                    if (transactionFinished != null)
                    {
                        try
                        {
                            var snapshot = transactionFinished.GetTransactionSnapshot();
                            _transactions.Add(snapshot);

                        }
                        finally
                        {
                            _transactionCompletionSource.SetResult(true);
                        }
                    }
                });

            LiveProfiler.Instance.RegisterEventSubscriber(_eventSubscriber.Object);

            var port = IPTestingUtilities.FindAvailableTcpPort();
            _urlBase = "http://localhost:" + port;

            try
            {
                _server = WebApp.Start(_urlBase, app =>
                {
                    app.UseNancy(options =>
                    {
                        options.Bootstrapper = new TestingBootstrapper();
                    });
                });
            }
            catch (Exception x)
            {
                throw new ApplicationException(_urlBase, x);
            }

            _httpClient = new HttpClient();
        }

        [TearDown]
        public void TearDown()
        {
            LiveProfiler.Instance.UnregisterEventSubscriber(_eventSubscriber.Object);
            _server?.Dispose();
            _httpClient?.Dispose();
        }

        [Test]
        public void NancyProfiling_Get200Response_ReturnsSuccessfulTransaction()
        {
            // before
            _transactions.Should().BeEmpty();

            // execute
            var url = _urlBase + TestingModule.GetPathPrefix + "/200?CorrelationId=" + CorrelationId;

            var result = _httpClient.GetAsync(url).Result;

            // verify
            _transactionCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            _transactions.Should().HaveCount(1);
            var transaction = _transactions.First();

            var json = JsonConvert.SerializeObject(transaction, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            });

            Trace.WriteLine(json);

            transaction.Category.Should().Be("Nancy.Request");
            transaction.Name.Should().Be("GET " + TestingModule.GetPath);
            transaction.DisplayName.Should().Be("GET " + url);
            transaction.CorrelationId.Should().Be(CorrelationId);
            transaction.Parameters.Should().BeNull();
            transaction.State.Should().Be(TransactionState.Success);
            transaction.Result.Should().NotBeNull();

            dynamic transactionResult = JObject.FromObject(transaction.Result);

            ((int)transactionResult.StatusCode).Should().Be((int)result.StatusCode);
            ((string)transactionResult.ContentType).Should().Be(result.Content.Headers.ContentType.ToString());
        }

        [Test]
        public void NancyProfiling_Get404Response_ReturnsSuccessfulTransaction()
        {
            // before
            _transactions.Should().BeEmpty();

            // execute
            var url = _urlBase + TestingModule.GetPathPrefix + "/404?CorrelationId=" + CorrelationId;

            var result = _httpClient.GetAsync(url).Result;

            // verify
            _transactionCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            result.StatusCode.Should().Be(HttpStatusCode.NotFound);

            _transactions.Should().HaveCount(1);
            var transaction = _transactions.First();

            var json = JsonConvert.SerializeObject(transaction, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            });

            Trace.WriteLine(json);

            transaction.Category.Should().Be("Nancy.Request");
            transaction.Name.Should().Be("GET " + TestingModule.GetPath);
            transaction.DisplayName.Should().Be("GET " + url);
            transaction.CorrelationId.Should().Be(CorrelationId);
            transaction.Parameters.Should().BeNull();
            transaction.State.Should().Be(TransactionState.Success);
            transaction.Result.Should().NotBeNull();

            dynamic transactionResult = JObject.FromObject(transaction.Result);

            ((int)transactionResult.StatusCode).Should().Be((int)result.StatusCode);
            ((string)transactionResult.ContentType).Should().Be(result.Content.Headers.ContentType.ToString());
        }

        [Test]
        public void NancyProfiling_Get500Response_ReturnsFailedTransaction()
        {
            // before
            _transactions.Should().BeEmpty();

            // execute
            var url = _urlBase + TestingModule.GetPathPrefix + "/500?CorrelationId=" + CorrelationId;

            var result = _httpClient.GetAsync(url).Result;

            // verify
            _transactionCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            _transactions.Should().HaveCount(1);
            var transaction = _transactions.First();

            var json = JsonConvert.SerializeObject(transaction, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            });

            Trace.WriteLine(json);

            transaction.Category.Should().Be("Nancy.Request");
            transaction.Name.Should().Be("GET " + TestingModule.GetPath);
            transaction.DisplayName.Should().Be("GET " + url);
            transaction.CorrelationId.Should().Be(CorrelationId);
            transaction.Parameters.Should().BeNull();
            transaction.State.Should().Be(TransactionState.Failure);
            transaction.Result.Should().NotBeNull();

            dynamic transactionResult = JObject.FromObject(transaction.Result);

            ((int)transactionResult.StatusCode).Should().Be((int)result.StatusCode);
            ((string)transactionResult.ContentType).Should().Be(result.Content.Headers.ContentType.ToString());
        }

        [Test]
        public void NancyProfiling_EndpointThrowsException_ReturnsFailedTransaction()
        {
            // before
            _transactions.Should().BeEmpty();

            // execute
            var url = _urlBase + TestingModule.GetPathPrefix + "/200?CorrelationId=" + CorrelationId + "&Throw=true";

            var result = _httpClient.GetAsync(url).Result;

            // verify
            _transactionCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            _transactions.Should().HaveCount(1);
            var transaction = _transactions.First();

            var json = JsonConvert.SerializeObject(transaction, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            });

            Trace.WriteLine(json);

            transaction.Category.Should().Be("Nancy.Request");
            transaction.Name.Should().Be("GET " + TestingModule.GetPath);
            transaction.DisplayName.Should().Be("GET " + url);
            transaction.CorrelationId.Should().Be(CorrelationId);
            transaction.Parameters.Should().BeNull();
            transaction.State.Should().Be(TransactionState.Failure);
            transaction.Result.Should().BeAssignableTo<Exception>();
        }

        [Test]
        public void NancyProfiling_Post200Response_ReturnsSuccessfulTransaction()
        {
            // before
            _transactions.Should().BeEmpty();

            // execute
            var url = _urlBase + TestingModule.PostPathPrefix + "/200?CorrelationId=" + CorrelationId;
            const string requestContentType = "application/json";
            var content = new StringContent("{}", Encoding.UTF8, requestContentType);

            var result = _httpClient.PostAsync(url, content).Result;

            // verify
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            _transactionCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            _transactions.Should().HaveCount(1);
            var transaction = _transactions.First();

            var json = JsonConvert.SerializeObject(transaction, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            });

            Trace.WriteLine(json);

            transaction.Category.Should().Be("Nancy.Request");
            transaction.Name.Should().Be("POST " + TestingModule.PostPath);
            transaction.DisplayName.Should().Be("POST " + url);
            transaction.CorrelationId.Should().Be(CorrelationId);
            transaction.Parameters.Should().NotBeNull();
            transaction.State.Should().Be(TransactionState.Success);
            transaction.Result.Should().NotBeNull();

            dynamic requestParameters = JObject.FromObject(transaction.Parameters);
            ((string) requestParameters.ContentType).Should().Contain(requestContentType);

            dynamic transactionResult = JObject.FromObject(transaction.Result);
            ((int)transactionResult.StatusCode).Should().Be((int)result.StatusCode);
            ((string)transactionResult.ContentType).Should().Be(result.Content.Headers.ContentType.ToString());
        }

        [Test]
        public void NancyProfiling_Post400Response_ReturnsSuccessfulTransaction()
        {
            // before
            _transactions.Should().BeEmpty();

            // execute
            var url = _urlBase + TestingModule.PostPathPrefix + "/400?CorrelationId=" + CorrelationId;
            const string requestContentType = "application/json";
            var content = new StringContent("{}", Encoding.UTF8, requestContentType);

            var result = _httpClient.PostAsync(url, content).Result;

            // verify
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            _transactionCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            _transactions.Should().HaveCount(1);
            var transaction = _transactions.First();

            var json = JsonConvert.SerializeObject(transaction, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            });

            Trace.WriteLine(json);

            transaction.Category.Should().Be("Nancy.Request");
            transaction.Name.Should().Be("POST " + TestingModule.PostPath);
            transaction.DisplayName.Should().Be("POST " + url);
            transaction.CorrelationId.Should().Be(CorrelationId);
            transaction.Parameters.Should().NotBeNull();
            transaction.State.Should().Be(TransactionState.Success);
            transaction.Result.Should().NotBeNull();

            dynamic requestParameters = JObject.FromObject(transaction.Parameters);
            ((string) requestParameters.ContentType).Should().Contain(requestContentType);

            dynamic transactionResult = JObject.FromObject(transaction.Result);
            ((int)transactionResult.StatusCode).Should().Be((int)result.StatusCode);
            ((string)transactionResult.ContentType).Should().Be(result.Content.Headers.ContentType.ToString());
        }

        [Test]
        public void NancyProfiling_Post500Response_ReturnsFailedTransaction()
        {
            // before
            _transactions.Should().BeEmpty();

            // execute
            var url = _urlBase + TestingModule.PostPathPrefix + "/500?CorrelationId=" + CorrelationId;
            const string requestContentType = "application/json";
            var content = new StringContent("{}", Encoding.UTF8, requestContentType);

            var result = _httpClient.PostAsync(url, content).Result;

            // verify
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            _transactionCompletionSource.Task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            _transactions.Should().HaveCount(1);
            var transaction = _transactions.First();

            var json = JsonConvert.SerializeObject(transaction, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            });

            Trace.WriteLine(json);

            transaction.Category.Should().Be("Nancy.Request");
            transaction.Name.Should().Be("POST " + TestingModule.PostPath);
            transaction.DisplayName.Should().Be("POST " + url);
            transaction.CorrelationId.Should().Be(CorrelationId);
            transaction.Parameters.Should().NotBeNull();
            transaction.State.Should().Be(TransactionState.Failure);
            transaction.Result.Should().NotBeNull();

            dynamic requestParameters = JObject.FromObject(transaction.Parameters);
            ((string) requestParameters.ContentType).Should().Contain(requestContentType);

            dynamic transactionResult = JObject.FromObject(transaction.Result);
            ((int)transactionResult.StatusCode).Should().Be((int)result.StatusCode);
            ((string)transactionResult.ContentType).Should().Be(result.Content.Headers.ContentType.ToString());
        }
    }
}
