using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using MiddleStack.Profiling.Events;

namespace MiddleStack.Profiling.StreamingServer
{
    public class EventIngestHub: Hub
    {
        private const string ClientStartEventsMethodName = "startEvents";
        private const string ClientStopEventsMethodName = "stopEvents";
        private static readonly string TransactionStartName = ProfilerEventType.TransactionStart.ToString();
        private static int _started = 0;
        private readonly ILog _log = LogManager.GetLogger(typeof(EventIngestHub).Name);

        internal static async Task StartEvents()
        {
            Interlocked.Exchange(ref _started, 1);
            try
            {
                var proxy = (IClientProxy)GlobalHost.ConnectionManager.GetHubContext<EventIngestHub>().Clients.All;
                await proxy.Invoke(ClientStartEventsMethodName).ConfigureAwait(false);
            }
            catch (Exception x)
            {
                Console.WriteLine(x);
            }
        }

        internal static async Task StopEvents()
        {
            Interlocked.Exchange(ref _started, 0);
            var proxy = (IClientProxy)GlobalHost.ConnectionManager.GetHubContext<EventIngestHub>().Clients.All;
            await proxy.Invoke(ClientStopEventsMethodName).ConfigureAwait(false);
        }

        public override async Task OnConnected()
        {
            await base.OnConnected().ConfigureAwait(false);

            var appName = Context.Request.QueryString["appName"];
            var hostName = Context.Request.QueryString["hostName"];

            _log.Info($"Event source connected: {hostName}-{appName}");
            if (_started > 0)
            {
                _log.Info($"Active consumers present. Activating event streaming on {hostName}-{appName}.");
                var proxy = Clients.Caller as IClientProxy;

                if (proxy != null)
                {
                    await proxy.Invoke(ClientStartEventsMethodName).ConfigureAwait(false);
                }
            }
        }

        public override Task OnReconnected()
        {
            var appName = Context.Request.QueryString["appName"];
            var hostName = Context.Request.QueryString["hostName"];

            _log.Info($"Event source reconnected: {hostName}-{appName}");

            return base.OnReconnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var appName = Context.Request.QueryString["appName"];
            var hostName = Context.Request.QueryString["hostName"];

            _log.Info($"Event source disconnected: {hostName}-{appName}");

            return base.OnDisconnected(stopCalled);
        }

        public async Task Event(IDictionary<string, object> eventMessage)
        {
            if ((string)eventMessage["type"] == TransactionStartName)
            {
                eventMessage["appName"] = Context.Request.QueryString["appName"];
                var hostName = Context.Request.QueryString["hostName"];
                eventMessage["hostName"] = hostName == Environment.MachineName ? "localhost" : hostName;
            }

            await EventConsumerHub.PublishEvent(eventMessage).ConfigureAwait(false);
        }
    }
}
