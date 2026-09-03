
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Tunneling.Core;
using Tunneling.Server.Framework;
using Tunneling.Server.Hubs;
using Tunneling.Server.Infrastructure;
using Tunneling.Server.Models.MapProxy;

namespace Tunneling.Server
{
    public class TcpServerService : IHostedService
    {
        private readonly ILogger<TcpServerService> _logger;
        private readonly IMemoryCache _memoryCache;

        private readonly List<TcpServer> _tcpServers;
        /// <summary>
        /// tcpserver -> hub 隧道下发通道通知事件服务
        /// </summary>
        private readonly ITunnelSessionChannelServices _tunnelSessionChannelServices;
        /// <summary>
        /// hub -> tcpserver 隧道上传通道通知事件服务
        /// </summary>
        private readonly ITunnelUploadChannel _tunnelUploadChannel;
        private readonly IServicesStatus _servicesStatus;

        //private readonly SystemConfig systemConfig;

        private SocketAsyncEventArgsPool SocketAsyncEventArgsPool = new SocketAsyncEventArgsPool(10000);
        public TcpServerService(ILogger<TcpServerService> logger,
                            IOptions<SystemConfig> options,
                            ITunnelSessionChannelServices tunnelSessionChannel,
                            ITunnelUploadChannel tunnelUploadChannel,
                            IServicesStatus servicesStatus,
                            IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;

            _tcpServers = new List<TcpServer>();

            _tunnelSessionChannelServices = tunnelSessionChannel;

            var systemConfig = options.Value;
            foreach (var group in systemConfig.MapGroups)
            {
                foreach (var item in group.MapProxy)
                {
                    var server = new TcpServer(item.PublicPort, SocketAsyncEventArgsPool)
                    {
                        Policy = item.Policy,
                        GroupName = group.GroupName,
                        ChannelName = item.Name,
                        PrivateHost = item.LocalHost,
                        PrivatePort = item.LocalPort,
                        CheckChannelFilter = On_CheckChannelFilter
                    };

                    server.OnServerStarted += Server_OnServerStarted;
                    server.OnServerStopped += Server_OnServerStopped;
                    server.OnSessionConnected += Server_OnSessionConnected;
                    server.OnSessionDisconnected += Server_OnSessionDisconnected;
                    server.OnDataReceived += Server_OnDataReceived;

                    _tcpServers.Add(server);
                }
            }
            _tunnelUploadChannel = tunnelUploadChannel;
            _tunnelUploadChannel.OnSessionConnectedEvent += tunnelUploadChannel_OnSessionConnected;
            _tunnelUploadChannel.OnCloseSessionEvent += tunnelUploadChannel_OnCloseSession;
            _tunnelUploadChannel.OnUploadStreamEvent += tunnelUploadChannel_OnUploadStream;
            _tunnelUploadChannel.OnSessionCheckedEvent += tunnelUploadChannel_OnSessionCheckedEvent;
            _tunnelUploadChannel.OnSyncConversationListEvent += tunnelUploadChannel_OnSyncConversationListEvent;
            _tunnelUploadChannel.OnChannelClosedEvent += _tunnelUploadChannel_OnChannelClosedEvent;

            _servicesStatus = servicesStatus;
            _servicesStatus.OnGetStatusEvent += On_GetSystemStatus;

        }

        /// <summary>
        /// 收集服务状态
        /// </summary>
        /// <returns></returns>
        private List<SystemStatus> On_GetSystemStatus()
        {
            var xx = from server in _tcpServers
                     group server by server.GroupName into g
                     select new SystemStatus
                     {
                         GroupName = g.Key,
                         Channels = _tcpServers.Where(a => a.GroupName == g.Key)
                         .Select(b => new ChannelStatus
                         {
                             ChannelName = b.ChannelName,
                             ActiveConnections = b.SessionCount,
                             TotalReceiveBytes = b.TotalReceiveBytes,
                             TotalSendBytes = b.TotalSendBytes,
                             TotalRequestCount = b.TotalRequestCount,
                         })
                         .ToList()
                     };
            return xx.ToList();
        }

        #region 隧道上报事件
        /// <summary>
        /// 隧道关闭通知
        /// </summary>
        /// <param name="group"></param>
        private void _tunnelUploadChannel_OnChannelClosedEvent(string group)
        {
            try
            {
                _logger.LogInformation("隧道通道关闭通知: {GroupName}  清除所有会话", group);
                var servers = _tcpServers.Where(a => a.GroupName == group);
                foreach (var server in servers)
                {
                    var sessions = server.GetAllSessions();
                    foreach (var session in sessions)
                    {
                        session.Close();
                        _logger.LogInformation("清除会话: {channelName} {sessionId}", server.SessionName, session.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        // 客户端要求同步会话列表
        private void tunnelUploadChannel_OnSyncConversationListEvent(string channelName, List<uint> sessionId)
        {
            try
            {
                var server = _tcpServers.FirstOrDefault(a => a.SessionName == channelName);
                if (server != null)
                {
                    var sessions = server.GetAllSessions();

                    foreach (var id in sessionId)
                    {
                        //如果会话己经不存在，就通知客户
                        if (!sessions.Any(a => a.Id == id))
                        {
                            _logger.LogInformation("同步关闭会话 {sessionId}", id);
                            _tunnelSessionChannelServices.DisconnectSession(server.GroupName, server.SessionName, id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        // 上报己不存在的会话
        private void tunnelUploadChannel_OnSessionCheckedEvent(string channelName, uint sessionId)
        {
            try
            {
                var server = _tcpServers.FirstOrDefault(a => a.SessionName == channelName);
                if (server != null)
                {
                    var session = server.GetSessionById(sessionId);
                    if (session != null)
                    {
                        session.Close();
                        _logger.LogInformation("清除己死掉的会话: {channelName} {sessionId}", channelName, sessionId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
        private async Task<bool> tunnelUploadChannel_OnUploadStream(string channelName, uint sessionId, byte[] data)
        {
            bool ok = false;
            try
            {
                var server = _tcpServers.FirstOrDefault(a => a.SessionName == channelName);
                if (server != null)
                {
                    ok = await server.SendData(sessionId, data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return ok;
        }

        private void tunnelUploadChannel_OnCloseSession(string channelName, uint sessionId)
        {
            var server = _tcpServers.FirstOrDefault(a => a.SessionName == channelName);
            if (server != null)
            {
                var session = server.GetSessionById(sessionId);
                if (session != null)
                {
                    session.Close();
                    _logger.LogInformation("会话己清除: {channelName} {sessionId}", channelName, sessionId);
                }
            }
        }
        private void tunnelUploadChannel_OnSessionConnected(string channelName, uint sessionId)
        {
            _logger.LogInformation("成功建立隧道会话：{ChannelName} {sessionId} ", channelName, sessionId);
        }
        #endregion

        #region 后台服务的重载控制方法


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TCP监听服务正在启动...");
            foreach (var item in _tcpServers)
            {
                item.Start();
            }

            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TCP监听服务己停止.");

            foreach (var item in _tcpServers)
            {
                item.Close();
            }

            _tcpServers.Clear();

            return Task.CompletedTask;
        }
        #endregion

        #region TCP服务器事件
        /// <summary>
        /// 在这里检查通道是否有效,信道未建立则拒绝socket入站连接
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        private bool On_CheckChannelFilter(TcpServer server, Socket socket)
        {
            //检查是否被ban IP

            if (server.Policy != null)
            {
                var ip = socket?.RemoteEndPoint?.ToString()?.Split(':')[0];

                var key = $"BanIP_{server.GroupName}_{server.ChannelName}_{ip}";

                int count = 0;
                if (_memoryCache.TryGetValue(key, out count))
                {
                    count++;
                    if (count >= server.Policy.Threshold)
                    {
                        _logger.LogWarning("拒绝被禁止的IP连接: {RemoteHost} {ChannelName} count->{count}", ip, server.ChannelName, count);
                        _memoryCache.Set(key, count, server.Policy.Time);
                        return false;
                    }
                }

                _memoryCache.Set(key, count, server.Policy.Time);
            }

            return _tunnelSessionChannelServices.CheckChannelStatus(server.GroupName);
        }
        private void Server_OnDataReceived(TcpServer server, TcpSession session, byte[] data)
        {
            try
            {
                _tunnelSessionChannelServices.SendData(server.GroupName, server.SessionName, session.Id, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private void Server_OnSessionDisconnected(TcpServer server, TcpSession session)
        {
            _logger.LogInformation("关闭会话通知 {ChannelName} {SessionId} {Port} -> 内网 {PrivateHost}:{PrivatePort}"
                  , server.SessionName
                  , session.Id
                       , server.Port
                       , server.PrivateHost
                       , server.PrivatePort);
            try
            {
                _tunnelSessionChannelServices.DisconnectSession(server.GroupName, server.SessionName, session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private async void Server_OnSessionConnected(TcpServer server, TcpSession session)
        {
            _logger.LogInformation("{RemoteHost} 请求建立会话 {ChannelName} {Port} -> 映射内网 {PrivateHost}:{PrivatePort}"
                        , session.RemoteHost.ToString()
                        , server.SessionName
                        , server.Port
                        , server.PrivateHost
                        , server.PrivatePort);
            try
            {
                //var task = _tunnelSessionChannelServices.ConnectSession(server.GroupName, server.SessionName, session.Id, server.PrivateHost, server.PrivatePort);
                //task.Wait(TimeSpan.FromSeconds(1));

                await _tunnelSessionChannelServices.ConnectSession(server.GroupName, server.SessionName, session.Id, server.PrivateHost, server.PrivatePort);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private void Server_OnServerStopped(TcpServer server)
        {
            _logger.LogInformation("关闭TCP监听服务：{ChannelName} {Port} -> 映射内网 {PrivateHost}:{PrivatePort}"
                        , server.SessionName
                        , server.Port
                        , server.PrivateHost
                        , server.PrivatePort);
        }

        private void Server_OnServerStarted(TcpServer server)
        {
            _logger.LogInformation("成功启动TCP监听服务：{ChannelName} {Port} -> 映射内网 {PrivateHost}:{PrivatePort}"
                        , server.SessionName
                        , server.Port
                        , server.PrivateHost
                        , server.PrivatePort);
        }
        #endregion
    }
}