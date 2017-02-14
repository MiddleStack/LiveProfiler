using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace MiddleStack.Profiling.StreamingServer
{
    public class EventConsumerHub: Hub
    {
        private static int _connectionCount;
        private ILog _log = LogManager.GetLogger(typeof(EventConsumerHub).Name);

        public override async Task OnConnected()
        {
            await base.OnConnected().ConfigureAwait(false);

            var connectionCount = Interlocked.Increment(ref _connectionCount);

            _log.Info($"Client connected. Connection count: {connectionCount}");

            if (connectionCount == 1)
            {
                _log.Info($"Activating event sources");
                await EventIngestHub.StartEvents().ConfigureAwait(false);
            }
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            var connectionCount = Interlocked.Decrement(ref _connectionCount);

            _log.Info($"Client disconnected. Connection count: {connectionCount}");

            if (connectionCount < 1) // last
            {
                _log.Info($"Deactivating event sources");
                await EventIngestHub.StopEvents().ConfigureAwait(false);
            }

            await base.OnDisconnected(stopCalled).ConfigureAwait(false);
        }

        internal static async Task PublishEvent(IDictionary<string, object> eventMessage)
        {
            var proxy = (IClientProxy)GlobalHost.ConnectionManager.GetHubContext<EventConsumerHub>().Clients.All;
            await proxy.Invoke("event", eventMessage).ConfigureAwait(false);
        }
    }
}
