using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Bootstrapper;

namespace MiddleStack.Profiling.Nancy
{
    /// <summary>
    ///     Contain methods to enable profiling of Nancy requests.
    /// </summary>
    public static class NancyModuleExtensions
    {
        private const string TransactionKey = "__LiveProfilerTransaction";

        /// <summary>
        ///     Creats a LiveProfiler transaction for each Nancy request handled by this module. 
        ///     Call this method within the 
        ///     module's constructor method. Any additional module event hooks should
        ///     be contained in the <paramref name="additionalModuleHooks"/> delegate.
        /// </summary>
        /// <param name="module">
        ///     The <see cref="NancyModule"/> object on which to enable profiling.
        /// </param>
        /// <param name="additionalModuleHooks">
        ///     The delegate that adds module event hooks onto the module. Assign all aditional event hooks
        ///     in this delegate, if you desire to have LiveProfiler include the actions taken by these event
        ///     hooks in the profiler transaction.
        /// </param>
        /// <param name="correlationIdGetter">
        ///     A delegate that obtains the correlation Id of transaction from the 
        ///     <see cref="NancyContext"/> object in each request.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="module"/> is <see langword="null"/>.
        /// </exception>
        public static void UseLiveProfiler(this NancyModule module, Action<NancyModule> additionalModuleHooks = null, Func<NancyContext, string> correlationIdGetter = null)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            module.Before += (ctx) =>
            {
                var name = GetName(ctx);
                var displayName = GetDisplayName(ctx);
                var correlationId = correlationIdGetter?.Invoke(ctx);
                var parameters = new
                {
                    Parameters = TranslateParameters(ctx.Request),
                    Headers = TranslateHeaders(ctx.Request.Headers)
                };

                var transaction = LiveProfiler.Instance.Transaction(
                    "Nancy",
                    name, 
                    displayName,
                    parameters,
                    correlationId,
                    TransactionMode.StepOrTransaction);

                ctx.Items.Add(TransactionKey, transaction);

                return null;
            };

            additionalModuleHooks?.Invoke(module);

            module.After += ctx =>
            {
                object obj;
                ITiming transaction;

                if (ctx.Items.TryGetValue(TransactionKey, out obj)
                    && (transaction = obj as ITiming) != null)
                {
                    var result = new
                    {
                        StatusCode = ctx.Response.StatusCode,
                        ContentType = ctx.Response.ContentType,
                        ReasonPhrase = ctx.Response.ReasonPhrase
                    };

                    if ((int) ctx.Response.StatusCode < 500)
                    {
                        transaction.Success(result);
                    }
                    else
                    {
                        transaction.Failure(result);
                    }
                }
            };

            module.OnError += (ctx, x) =>
            {
                object obj;
                ITiming transaction;

                if (ctx.Items.TryGetValue(TransactionKey, out obj)
                    && (transaction = obj as ITiming) != null)
                {
                    transaction.Failure(x);
                }

                return null;
            };
        }

        private static object TranslateParameters(Request request)
        {
            var dictionary = new Dictionary<string, string>(request.    );
        }

        private static object TranslateHeaders(RequestHeaders headers)
        {
            var dictionary = new Dictionary<string,string>(headers.Count());

            foreach (var headerKey in headers.Keys)
            {
                dictionary.Add(headerKey, String.Join(",", headers[headerKey]));
            }

            return dictionary;
        }

        private static string GetDisplayName(NancyContext ctx)
        {
            return $"{ctx.Request.Method.ToUpperInvariant()} {ctx.Request.Url}";
        }

        private static string GetName(NancyContext ctx)
        {
            return $"{ctx.ResolvedRoute.Description.Method.ToUpperInvariant()} {ctx.ResolvedRoute.Description.Path}";
        }
    }
}
