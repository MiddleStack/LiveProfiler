using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using MiddleStack.Profiling.Events;

namespace MiddleStack.Profiling.Streaming
{
    /// <summary>
    ///     Streams profiler events to a WebSocket-based streaming server, 
    ///     typically running on local host, tcp port 7700.
    /// </summary>
    public class ProfilerEventStreamer: IProfilerEventSubscriberAsync
    {
        private readonly Uri _serverUrl;
        private readonly bool _enabled;
        private readonly string _hostName;
        private readonly string _appName;
        private readonly Task<IHubProxy> _hubProxy;
        private int _sending;

        /// <summary>
        ///     Initializes a new instance of <see cref="ProfilerEventStreamer"/>.
        /// </summary>
        /// <param name="config">
        ///     Optional. The configuration object that overrides the default settings
        ///     and the configuration file.
        /// </param>
        public ProfilerEventStreamer(StreamingConfiguration config = null)
        {
            _serverUrl = config?.ServerUrl ?? new Uri("http://localhost:7700");
            _enabled = config?.Enabled ?? true;
            _hostName = config?.HostName ?? Environment.MachineName;
            _appName = config?.AppName ?? Process.GetCurrentProcess().ProcessName;

            if (_enabled)
            {
                _hubProxy = GetHubProxy();
            }
        }

        private async Task<IHubProxy> GetHubProxy()
        {
            var connection = new HubConnection(_serverUrl.ToString(), new Dictionary<string, string>
            {
                {"hostName", _hostName},
                {"appName", _appName}
            });

            var hubProxy = connection.CreateHubProxy("EventIngest");

            hubProxy.On("startEvents", data =>
            {
                Interlocked.Exchange(ref _sending, 1);
            });

            hubProxy.On("stopEvents", data =>
            {
                Interlocked.Exchange(ref _sending, 1);
            });

            await connection.Start();

            return hubProxy;
        }

        public async Task HandleEventAsync(IProfilerEvent stepEvent)
        {
            if (_sending != 1) return;

            var proxy = await _hubProxy.ConfigureAwait(false);

            var message = ToMessage(stepEvent);

            await proxy.Invoke("event", message).ConfigureAwait(false);
        }

        private static dynamic ToMessage(IProfilerEvent stepEvent)
        {
            var transactionStart = stepEvent as ITransactionStartEvent;
            var transactionFinish = stepEvent as ITransactionFinishEvent;
            var stepStart = stepEvent as IStepStartEvent;
            var stepFinish = stepEvent as IStepFinishEvent;

            dynamic message = new ExpandoObject();

            message.type = stepEvent.Type.ToString();

            if (transactionStart != null)
            {
                message.correlationId = transactionStart.CorrelationId;
                message.id = transactionStart.Id.ToString("n");
                message.category = transactionStart.Category;
                message.name = transactionStart.Name;
                message.displayName = transactionStart.DisplayName;
                message.parameters = transactionStart.Parameters;
                message.start = transactionStart.Start;
            }
            else if (stepStart != null)
            {
                message.id = stepStart.Id.ToString("n");
                message.parentId = stepStart.ParentId.ToString("n");
                message.category = stepStart.Category;
                message.name = stepStart.Name;
                message.displayName = stepStart.DisplayName;
                message.parameters = stepStart.Parameters;
                message.start = stepStart.Start;
                message.relativeStart = stepStart.RelativeStart;
            }
            else if (transactionFinish != null)
            {
                message.duration = transactionFinish.Duration;
                message.isSuccess = transactionFinish.IsSuccess;
                message.result = transactionFinish.Result;
            }
            else if (stepFinish != null)
            {
                message.duration = stepFinish.Duration;
                message.isSuccess = stepFinish.IsSuccess;
                message.result = stepFinish.Result;
            }
            return message;
        }
    }
}
