using System;
using Owin;

namespace MiddleStack.Profiling.Owin.RestApi
{
    public static class AppBuilderExtensions
    {
        /// <summary>
        ///     Adds REST API that provides access to LiveProfiler data.
        /// </summary>
        /// <remarks>
        ///     <para>The endpoints offered are:</para>
        ///     <list type="bullet">
        ///         <item>GET /liveprofiler/api/v1/transactions/recent</item>
        ///         <item>GET /liveprofiler/api/v1/transactions/inflight</item>
        ///     </list>
        /// </remarks>
        /// <param name="appBuilder">
        ///     The <see cref="IAppBuilder"/> to which to add the LiveProfiler API endpoints.
        /// </param>
        /// <param name="basePath">
        ///     The starting portion of the path to the API endpoints. By default, it's /liveprofiler/api.
        ///     But users can override this to customize routing.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The argument <paramref name="appBuilder"/> is <see langword="null"/>.
        /// </exception>
        public static void UseLiveProfilerRestApi(this IAppBuilder appBuilder, string basePath = "/liveprofiler/api")
        {
            if(appBuilder == null) throw new ArgumentNullException(nameof(appBuilder));

            appBuilder.Use<LiveProfilerRestApiMiddleware>(basePath);
        }
    }
}
