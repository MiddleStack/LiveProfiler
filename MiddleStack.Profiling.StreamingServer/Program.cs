using System;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;

namespace MiddleStack.Profiling.StreamingServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebApp.Start("http://localhost:7700", app =>
            {
                app.UseCors(CorsOptions.AllowAll);
                app.Use<WebMiddleware>();
                app.Map("/signalr", app2 => app2.MapSignalR());
            }))
            {

                Console.Write("Press [ENTER] to quit: ");
                Console.ReadLine();
            }
        }
    }
}
