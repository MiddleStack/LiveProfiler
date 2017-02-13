using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using MiddleStack.Profiling.Events;

namespace MiddleStack.Profiling.StreamingServer
{
    internal class EventIngest: Hub
    {
        private const string ClientStartEventsMethodName = "startEvents";
        private const string ClientStopEventsMethodName = "stopEvents";
        private static readonly string TransactionStartName = ProfilerEventType.TransactionStart.ToString();
        private static int _started = 0;

        internal async Task StartEvents()
        {
            Interlocked.Exchange(ref _started, 1);
            IClientProxy proxy = Clients.All;
            await proxy.Invoke(ClientStartEventsMethodName).ConfigureAwait(false);
        }

        internal async Task StopEvents()
        {
            Interlocked.Exchange(ref _started, 0);
            IClientProxy proxy = Clients.All;
            await proxy.Invoke(ClientStopEventsMethodName).ConfigureAwait(false);
        }

        public override async Task OnConnected()
        {
            await base.OnConnected().ConfigureAwait(false);

            if (_started > 0)
            {
                IClientProxy proxy = Clients.Caller;
                await proxy.Invoke(ClientStartEventsMethodName).ConfigureAwait(false);
            }
        }

        public async Task Event(IDictionary<string, object> eventMessage)
        {
            if ((string)eventMessage["type"] == TransactionStartName)
            {
                eventMessage["appName"] = Context.Request.QueryString["appName"];
                eventMessage["hostName"] = Context.Request.QueryString["hostName"];
            }

            var broadcastHub = new EventBroadcast();
            await broadcastHub.PublishEvent(eventMessage).ConfigureAwait(false);
        }
    }
}
