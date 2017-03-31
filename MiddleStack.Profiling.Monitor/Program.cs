using System;
using System.Diagnostics;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;

namespace MiddleStack.Profiling.Monitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var baseUrl = "http://localhost:7700";

            GlobalHost.HubPipeline.AddModule(new ErrorHandlingPipelineModule());

            using (WebApp.Start(baseUrl, app =>
            {
                app.UseCors(CorsOptions.AllowAll);
                app.Use<WebMiddleware>();
                app.MapSignalR();
            }))
            {
                Console.Write($"Listening on {baseUrl}\nPress [ENTER] to quit: ");
                Console.ReadLine();
            }
        }

        public class ErrorHandlingPipelineModule : HubPipelineModule
        {
            protected override void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
            {
                Debug.WriteLine("=> Exception " + exceptionContext.Error.Message);
                if (exceptionContext.Error.InnerException != null)
                {
                    Debug.WriteLine("=> Inner Exception " + exceptionContext.Error.InnerException.Message);
                }
                base.OnIncomingError(exceptionContext, invokerContext);

            }

            protected override bool OnBeforeConnect(IHub hub)
            {
                return base.OnBeforeConnect(hub);
            }
        }
    }
}
