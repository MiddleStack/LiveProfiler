using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace MiddleStack.Profiling.StreamingServer
{
    internal class EventBroadcast: Hub
    {
        private static int _connectionCount;

        public override async Task OnConnected()
        {
            await base.OnConnected().ConfigureAwait(false);

            if (Interlocked.Increment(ref _connectionCount) == 1)
            {
                var ingestHub = new EventIngest();
                await ingestHub.StartEvents().ConfigureAwait(false);
            }
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            if (Interlocked.Decrement(ref _connectionCount) < 1) // last
            {
                var ingestHub = new EventIngest();
                await ingestHub.StopEvents().ConfigureAwait(false);
            }

            await base.OnDisconnected(stopCalled).ConfigureAwait(false);
        }

        internal async Task PublishEvent(IDictionary<string, object> eventMessage)
        {
            IClientProxy proxy = Clients.All;

            await proxy.Invoke("event", eventMessage).ConfigureAwait(false);
        }
    }
}
