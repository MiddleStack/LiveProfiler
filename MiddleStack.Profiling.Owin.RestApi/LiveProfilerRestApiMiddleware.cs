using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MiddleStack.Profiling.Owin.RestApi
{
    internal class LiveProfilerRestApiMiddleware: OwinMiddleware
    {
        private readonly OwinMiddleware _next;
        private readonly string _basePath;

        public LiveProfilerRestApiMiddleware(OwinMiddleware next, string basePath) : base(next)
        {
            _next = next;

            basePath = basePath?.Trim();

            if (basePath?.StartsWith("/") == false)
            {
                basePath = "/" + basePath;
            }

            if (basePath?.EndsWith("/") == true)
            {
                basePath = basePath.TrimEnd('/');
            }

            _basePath = basePath;
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.Method == "GET"
                && context.Request.Path == new PathString($"{_basePath}/v1/transactions/recent"))
            {
                await Recent(context, false);
            }
            else if (context.Request.Method == "GET"
                && context.Request.Path == new PathString($"{_basePath}/v1/transactions/inflight"))
            {
                await Recent(context, true);
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private async Task Recent(IOwinContext context, bool inflightOnly)
        {
            var recentTransactions = new RecentTransactions
            {
                Transactions = LiveProfiler.Instance.GetRecentTransactions(inflightOnly)
            };

            context.Response.ContentType = "application/json";

            var json = JsonConvert.SerializeObject(recentTransactions, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            await context.Response.WriteAsync(json).ConfigureAwait(false);
        }
    }
}
