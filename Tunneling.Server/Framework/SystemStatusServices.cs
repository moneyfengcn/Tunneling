using Microsoft.AspNetCore.Mvc;
using Tunneling.Server.Infrastructure;
using Tunneling.Server.Models.Status;

namespace Tunneling.Server.Framework
{
    public class SystemStatusServices : ISystemStatus
    {
        private readonly INetworkTraffic _networkTraffic;
        private readonly IServicesStatus _servicesStatus;
        private readonly ILogger<SystemStatusServices> _logger;

        public SystemStatusServices(INetworkTraffic networkTraffic, IServicesStatus servicesStatus, ILogger<SystemStatusServices> logger)
        {
            _networkTraffic = networkTraffic;
            _servicesStatus = servicesStatus;
            _logger = logger;
        }

        public DashboardInfo GetServerInfo()
        {
            try
            {
                (var up, var down) = _networkTraffic.GetRealTimeSpeed();

                var status = _servicesStatus.GetSystemStatus();

                ServerInfo info = new ServerInfo()
                {
                    RunTime = DateTime.Now.Subtract(Program.RunTime).Format(),
                    NodeCount = status.Count,
                    Download = down,
                    Upload = up,
                };


                var channels = new List<Models.Status.ChannelStatus>();

                foreach (var group in status)
                {
                    var tmp = new Models.Status.ChannelStatus()
                    {
                        ChannelCount = 0,
                        ChannelName = group.GroupName,
                        SessionInfos = new List<SessionInfo>()
                    };

                    foreach (var item in group.Channels)
                    {
                        tmp.SessionInfos.Add(new SessionInfo()
                        {
                            UpCount = item.TotalReceiveBytes,
                            DownCount = item.TotalSendBytes,
                            Name = item.ChannelName,
                            SessionsCount = item.ActiveConnections,
                            TotalSessions = item.TotalRequestCount
                        });
                    }

                    channels.Add(tmp);
                }


                return new DashboardInfo()
                {
                    ServerInfo = info,
                    Channels = channels
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw;
            }            
        }
    }
}
