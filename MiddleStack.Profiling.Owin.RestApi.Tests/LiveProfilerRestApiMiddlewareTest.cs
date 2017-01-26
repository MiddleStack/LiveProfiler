using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Owin.Hosting;
using MiddleStack.Testing;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Owin;

namespace MiddleStack.Profiling.Owin.RestApi.Tests
{
    [TestFixture]
    public class LiveProfilerRestApiMiddlewareTest
    {
        private IDisposable _server;
        private string _url;

        [SetUp]
        public void Setup()
        {
            LiveProfiler.ResetForTesting();

            const string basePath = "/B1E727D5DA714C57A4D7154E497105DB";

            var port = IPTestingUtilities.FindAvailableTcpPort();
            var baseUrl = "http://localhost:" + port;
            _url = baseUrl + basePath;

            try
            {
                _server = WebApp.Start(baseUrl, app =>
                {
                    app.Map(basePath, app2 => app2.Use<LiveProfilerRestApiMiddleware>());
                });
            }
            catch (Exception x)
            {
                throw new ApplicationException(_url, x);
            }
        }

        [TearDown]
        public void TearDown()
        {
            _server?.Dispose();
        }

        [Test]
        public void LiveProfilerRestApiMiddleware_GetRecentTransactions_NoTransactions_ReturnsEmptyObject()
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(_url + "/liveprofiler/api/v1.0/transactions/recent").Result;

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

                var json = response.Content.ReadAsStringAsync().Result;
                var recentTransactions = JsonConvert.DeserializeObject<RecentTransactions>(json);

                recentTransactions.Transactions.Should().BeEmpty();
            }
        }

        [Test]
        public void LiveProfilerRestApiMiddleware_GetRecentTransactions_WithBothInflightAndCompletedTransactions_ReturnsAllTransactions()
        {
            using (var client = new HttpClient())
            {
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
                    var transaction = LiveProfiler.Instance.NewTransaction(i.ToString(), i.ToString(), forceNew:true);
                    LiveProfiler.Instance.Step(i.ToString(), i.ToString());

                    inflightTransactions.Add(transaction);
                }

                var response = client.GetAsync(_url + "/liveprofiler/api/v1.0/transactions/recent").Result;

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

                var json = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(json);
                var recentTransactions = JsonConvert.DeserializeObject<RecentTransactions>(json);

                recentTransactions.Transactions.Select(t => t.Id)
                    .Should().Equal(inflightTransactions.Reverse().Concat(finishedTransactions.Reverse()).Select(t => t.GetTransactionSnapshot().Id));
            }
        }

        [Test]
        public void LiveProfilerRestApiMiddleware_GetInflightTransactions_NoTransactions_ReturnsEmptyObject()
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(_url + "/liveprofiler/api/v1.0/transactions/inflight").Result;

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

                var json = response.Content.ReadAsStringAsync().Result;
                var recentTransactions = JsonConvert.DeserializeObject<RecentTransactions>(json);

                recentTransactions.Transactions.Should().BeEmpty();
            }
        }

        [Test]
        public void LiveProfilerRestApiMiddleware_GetInflightTransactions_WithBothInflightAndCompletedTransactions_ReturnsAllInflightTransactions()
        {
            using (var client = new HttpClient())
            {
                for (var i = 0; i < 10; i++)
                {
                    using (LiveProfiler.Instance.NewTransaction(i.ToString(), i.ToString()))
                    {
                        using (LiveProfiler.Instance.Step(i.ToString(), i.ToString()))
                        {
                        }
                    }
                }

                IList<ITransaction> inflightTransactions = new List<ITransaction>();

                for (var i = 0; i < 10; i++)
                {
                    var transaction = LiveProfiler.Instance.NewTransaction(i.ToString(), i.ToString(), forceNew:true);
                    LiveProfiler.Instance.Step(i.ToString(), i.ToString());

                    inflightTransactions.Add(transaction);
                }

                var response = client.GetAsync(_url + "/liveprofiler/api/v1.0/transactions/inflight").Result;

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

                var json = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(json);
                var recentTransactions = JsonConvert.DeserializeObject<RecentTransactions>(json);

                recentTransactions.Transactions.Select(t => t.Id)
                    .Should().Equal(inflightTransactions.Reverse().Select(t => t.GetTransactionSnapshot().Id));
            }
        }
    }
}
