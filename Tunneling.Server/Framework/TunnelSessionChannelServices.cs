using Microsoft.AspNetCore.SignalR;
using Tunneling.Server.Hubs;

namespace Tunneling.Server.Framework
{
    /// <summary>
    /// 隧道下发事件广播接口
    /// </summary>
    public interface ITunnelSessionChannelServices 
    {
        /// <summary>
        /// 检查通道状态
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns>是否己建立</returns>
        bool CheckChannelStatus(string channelName);

        Task ConnectSession(string group, string channelName, uint sessionId, string host, int port);
        Task DisconnectSession(string group, string channelName, uint sessionId);
        Task SendData(string group, string channelName, uint sessionId, byte[] data);      
    }

    public class TunnelSessionChannelServices : ITunnelSessionChannelServices
    {

        private readonly IHubContext<ChannelsHub, IChannelDownloadStreamEvents> _hubChannels;
        public TunnelSessionChannelServices(IHubContext<ChannelsHub, IChannelDownloadStreamEvents> hubChannels)
        {
            _hubChannels = hubChannels;
        }

        public bool CheckChannelStatus(string channelGroupName)
        {
            return ChannelsHub.UserList.Contains(channelGroupName);
        }

        public Task ConnectSession(string group, string channelName, uint sessionId, string host, int port)
        {
            return _hubChannels.Clients.Group(group).ConnectSession(channelName, sessionId, host, port);
        }

        public Task DisconnectSession(string group, string channelName, uint sessionId)
        {
            return _hubChannels.Clients.Group(group).DisconnectSession(channelName, sessionId);
        }

        public Task SendData(string group, string channelName, uint sessionId, byte[] data)
        {
            return _hubChannels.Clients.Group(group).SendData(channelName, sessionId, data);
        }
    }
}
