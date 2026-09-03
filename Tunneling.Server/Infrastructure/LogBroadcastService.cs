using Microsoft.AspNetCore.SignalR;
using Tunneling.Server.Hubs;

namespace Tunneling.Server.Infrastructure
{
    public class LogBroadcastService
    {
        private readonly SerilogInMemorySink _sink;
        private readonly IHubContext<LogHub, ILogEvents> _hub;
        private readonly ILogger<LogBroadcastService> _logger;

        public LogBroadcastService(SerilogInMemorySink sink, IHubContext<LogHub, ILogEvents> hub, ILogger<LogBroadcastService> logger)
        {
            _sink = sink;
            _hub = hub;
            _logger = logger;

            _sink.LogEmitted += OnLogEmitted;
        }

        private void OnLogEmitted(string line)
        {
            try
            {     
                _ = _hub.Clients.All.ReceiveLog(line);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast log");
            }
        }

        public void Dispose()
        {
            _sink.LogEmitted -= OnLogEmitted;
        }
    }
}
