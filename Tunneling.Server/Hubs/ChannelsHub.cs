using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Net;
using Tunneling.Server.Framework;

namespace Tunneling.Server.Hubs
{
    /// <summary>
    /// 信道  用来与内网客户端通信
    /// </summary>
    [Authorize(AuthenticationSchemes = "Token")]
    public class ChannelsHub : Hub<IChannelDownloadStreamEvents>, IChannelUploadStreamEvents
    {
        static public int SessionCount = 0;

        private readonly ILogger<ChannelsHub> _logger;
        private readonly ITunnelUploadChannel _tunnelUploadChannel;




        public ChannelsHub(ILogger<ChannelsHub> logger, ITunnelUploadChannel tunnelUploadChannel)
        {
            _logger = logger;
            _tunnelUploadChannel = tunnelUploadChannel;
        }


        static public List<string> UserList = new List<string>();

        public override async Task OnConnectedAsync()
        {
            Interlocked.Increment(ref SessionCount);
            var userName = this.Context.User.Identity.Name;

            _logger.LogInformation("信道连接己建立 {Name}", userName);
            UserList.Add(userName);

            await this.Groups.AddToGroupAsync(Context.ConnectionId, userName);
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Interlocked.Decrement(ref SessionCount);
            var userName = this.Context.User.Identity.Name;
            _logger.LogInformation("信道己中断 {Name}", userName);

            UserList.Remove(userName);
            _tunnelUploadChannel.ChannelClosed(userName);    
            await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, userName);
            await base.OnDisconnectedAsync(exception);
        }

        #region 内网客户端上报事件

        public void On_SessionConnected(string channelName, uint sessionId)
        {
            try
            {
                _logger.LogInformation("会话己建立 : {channelName} {sessionId}", channelName, sessionId);
                _tunnelUploadChannel.SessionConnected(channelName, sessionId);             
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task On_SessionDataArrivals(string channelName, uint sessionId, byte[] data)
        {
            try
            {
                //_logger.LogInformation("接收到上传数据： {ChannelName} {SessionId}   {Lentgth}", channelName, sessionId, data.Length);
                var ok = await _tunnelUploadChannel.UploadStream(channelName, sessionId, data);
                if (!ok)
                {
                    await this.Clients.Group(this.Context.User.Identity.Name).DisconnectSession(channelName, sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

        }

        public void On_SessionDisconnected(string channelName, uint sessionId)
        {
            try
            {
                _logger.LogInformation("会话己关闭 : {channelName} {sessionId}", channelName, sessionId);
                _tunnelUploadChannel.CloseSession(channelName, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public void On_SyncConversationList(string channelName, List<uint> listSessionId)
        {
            try
            {
                _logger.LogInformation("心跳同步");
                this.Clients.Group(this.Context.User.Identity.Name).Heartbeat(0);
                _tunnelUploadChannel.SyncConversationList(channelName, listSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        #endregion
    }

    /// <summary>
    /// 定义从服务端向客户端推送的事件（服务端 -> 客户端）
    /// </summary>
    public interface IChannelDownloadStreamEvents
    {
        /// <summary>
        /// 下发建立会话连接
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="sessionId"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        Task ConnectSession(string channelName, uint sessionId, string host, int port);

        /// <summary>
        /// 关闭会话连接
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="sessionId"></param>
        Task DisconnectSession(string channelName, uint sessionId);

        /// <summary>
        /// 下发数据
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="sessionId"></param>       
        /// <param name="data"></param>
        Task SendData(string channelName, uint sessionId, byte[] data);

        //下发心跳
        Task Heartbeat(int count);
    }

    /// <summary>
    /// 内网客户端上报的事件（客户端 -> 服务端）
    /// </summary>
    public interface IChannelUploadStreamEvents
    {
        /// <summary>
        /// 客户端上报会话列表同步
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="listSessionId"></param>
        void On_SyncConversationList(string channelName, List<uint> listSessionId);
        /// <summary>
        /// 客户端上报连接已建立
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="sessionId"></param>
        void On_SessionConnected(string channelName, uint sessionId);
        /// <summary>
        /// 客户端上报连接已断开
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="sessionId"></param>
        void On_SessionDisconnected(string channelName, uint sessionId);
        /// <summary>
        /// 客户端上报数据
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="sessionId"></param>
        /// <param name="data"></param>
        Task On_SessionDataArrivals(string channelName, uint sessionId, byte[] data);
    }
}
