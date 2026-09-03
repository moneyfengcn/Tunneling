using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Tunneling.Core;


namespace Tunneling.Client
{

    internal class SignalRHostedService(ILogger<SignalRHostedService> logger, IOptions<AppConfig> config) : BackgroundService
    {
        private HubConnection _connection;
        private ProxyDispatcher _proxyDispatcher;

        #region 定时器心跳
        private System.Threading.Timer? _timer = null;

        private int _heartbeat = 0;
        private void OnHeartbeat(int count)
        {
            _heartbeat = 0;
            logger.LogInformation("心跳回应");
        }

        //定时上报会话列表与服务器同步
        private void On_TimerCallback(object? state)
        {
            try
            {
                if (_heartbeat++ >= 3)
                {
                    _heartbeat = 0;
                    _connection.StopAsync();
                }
                _proxyDispatcher.SyncConversationList(On_SyncConversationList);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
        #endregion

        #region 代理到服务器

        private void On_SyncConversationList(string channelName, List<uint> listSessionId)
        {
            try
            {
                //if (listSessionId.Count > 0)
                {
                    logger.LogInformation("同步会话 心跳 {channelName} {Count}", channelName, listSessionId.Count);
                    _connection.InvokeAsync("On_SyncConversationList", channelName, listSessionId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
        private void On_ProxyConnected(string channelName, uint sessionId, bool isConnected)
        {
            try
            {
                string status = isConnected ? "连接打开" : "连接关闭";
                logger.LogInformation("本地代理 {ChannelName} {sessionId} ->  {status}"
                    , channelName
                    , sessionId
                    , status);

                if (isConnected)
                {
                    _connection.SendAsync("On_SessionConnected", channelName, sessionId);
                }
                else
                {
                    _connection.SendAsync("On_SessionDisconnected", channelName, sessionId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }


        private async Task On_DataReceivedEventHandler(ProxySocket session, int sendCount, byte[] data)
        {
            try
            {
                if (!session.IsConnected)
                {
                    logger.LogInformation("会话己清除，不需要再传输数据  {ChannelName} {SessionId}", session.ChannelName, session.SessionId);
                    return;
                }
                if (_connection.State != HubConnectionState.Connected)
                {
                    logger.LogInformation("信道未连接，无法传输数据  {ChannelName} {SessionId}", session.ChannelName, session.SessionId);
                    session.Disconnect();
                    return;
                }

                if (sendCount >= config.Value.FlowControl)
                {
                    await _connection.InvokeAsync("On_SessionDataArrivals", session.ChannelName, session.SessionId, data);
                }
                else
                {
                    await _connection.SendAsync("On_SessionDataArrivals", session.ChannelName, session.SessionId, data);
                }
            }
            catch
            {
                logger.LogDebug("发送会话数据失败 {Channel} {Session}", session.ChannelName, session.SessionId);
            }
        }
        #endregion

        #region 服务控制

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _proxyDispatcher = new ProxyDispatcher()
            {
                OnDataReceived = On_DataReceivedEventHandler,
                FlowControl = config.Value.FlowControl
            };

            _proxyDispatcher.OnProxyConnected += On_ProxyConnected;
            var hubUrl = new Uri(new Uri(config.Value.ServerAddress), "/channels");
            _connection = new HubConnectionBuilder()
                                .WithUrl(hubUrl, options =>
                                {
                                    options.SkipNegotiation = true;
                                    // 指定传输方式
                                    options.Transports = HttpTransportType.WebSockets;
                                    options.SkipNegotiation = true;

                                    options.DefaultTransferFormat = TransferFormat.Binary;

                                    // AccessTokenProvider
                                    options.AccessTokenProvider = () =>
                                    {
                                        string? token = string.IsNullOrWhiteSpace(config.Value.AccessToken) ? null : config.Value.AccessToken;
                                        return Task.FromResult(token);
                                    };
                                })
                                .AddMessagePackProtocol(options =>
                                {
                                    options.SerializerOptions = new MessagePack.MessagePackSerializerOptions(MessagePack.Resolvers.StandardResolver.Instance);
                                })
                                //.WithAutomaticReconnect(new[]
                                //{
                                //    TimeSpan.FromSeconds(0),   // 立即重试
                                //    TimeSpan.FromSeconds(3),
                                //    TimeSpan.FromSeconds(5),
                                //    TimeSpan.FromSeconds(10),
                                //    TimeSpan.FromSeconds(30)   // 之后每 30 秒一次
                                //})
                                .Build();

            _connection.Reconnecting += async error =>
            {
                logger.LogWarning("channel reconnecting");
                await Task.CompletedTask;
            };

            _connection.Reconnected += async connectionId =>
            {
                logger.LogInformation("channel reconnected. connectionId={0}", connectionId);
                await Task.CompletedTask;
            };

            _connection.Closed += async ex =>
            {
                logger.LogInformation("channel 连接关闭事件");
                await Task.CompletedTask;
            };

            _connection.On<string, uint, string, int>("ConnectSession", On_ConnectSession);
            _connection.On<string, uint>("DisconnectSession", On_DisconnectSession);
            _connection.On<string, uint, byte[]>("SendData", On_SendData);
            _connection.On<int>("Heartbeat", OnHeartbeat);

            const int Timeinterval = 20;
            _timer = new Timer(On_TimerCallback, this, TimeSpan.FromSeconds(Timeinterval), TimeSpan.FromSeconds(Timeinterval));
            await base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 重试延迟 
            TimeSpan retryDelay = TimeSpan.FromSeconds(3);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(retryDelay, stoppingToken);
                    // 如果已连接，短暂等待后继续监测
                    if (_connection.State == HubConnectionState.Connected)
                    {
                        await Task.Delay(retryDelay, stoppingToken);
                        continue;
                    }
                    if (_connection.State != HubConnectionState.Disconnected)
                    {
                        await _connection.StopAsync(stoppingToken);
                    }
                    logger.LogInformation("正在尝试与公网服务器建立信道 {ServerAddress} ...", config.Value.ServerAddress);

                    await _connection.StartAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); // 确保连接稳定

                    if (_connection?.State == HubConnectionState.Connected)
                    {
                        logger.LogInformation("成功与公网服务器建立信道 {ServerAddress} ...", config.Value.ServerAddress);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 取消请求，退出循环以便优雅停止             
                    continue;
                }
                catch (WebSocketException) { continue; }
                catch (HttpRequestException)
                {
                    logger.LogInformation("信道中断");
                    continue;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
                await Task.Delay(retryDelay, stoppingToken);
            }

            logger.LogInformation("信道服务正在停止 ...");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
                _proxyDispatcher.Dispose();

                if (_connection != null && _connection.State == HubConnectionState.Connected)
                {
                    await _connection.StopAsync();
                    await _connection.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            logger.LogInformation("隧道服务己关闭.");
            await base.StopAsync(cancellationToken);
        }
        #endregion


        #region  服务器下发的事件

        void On_ConnectSession(string channelName, uint sessionId, string host, int port)
        {
            try
            {
                logger.LogInformation("请求建立会话 {ChannelName} {SessionId} -> {Host}:{Port}"
                                         , channelName, sessionId, host, port);

                var ok = _proxyDispatcher.Connect(channelName, sessionId, host, port);
                logger.LogInformation("调度器建立会话 {ChannelName} {SessionId} -> {Host}:{Port}   {Result}",
                    channelName, sessionId, host, port, (ok ? "成功" : "失败"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }

        }

        /// <summary>
        /// 关闭会话连接
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="sessionId"></param>
        void On_DisconnectSession(string channelName, uint sessionId)
        {
            try
            {
                var result = _proxyDispatcher.Disconnect(channelName, sessionId);
                logger.LogInformation("请求关闭会话 {0} - {1}  {2}"
                                    , channelName, sessionId,
                                    (result ? "关闭本地会话成功" : "己不存在"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

        /// <summary>
        /// 下发数据
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="sessionId"></param>       
        /// <param name="data"></param>
        async void On_SendData(string channelName, uint sessionId, byte[] data)
        {
            try
            {
                var result = await _proxyDispatcher.SendData(channelName, sessionId, data);
                if (!result)
                {
                    logger.LogInformation("透传发送数据失败 client -> target {Channel} {SessionId}", channelName, sessionId);
                    _connection.SendAsync("On_SessionDisconnected", channelName, sessionId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
        #endregion
    }
}