
using Tunneling.Server.Hubs;

namespace Tunneling.Server.Framework
{
    public class ChannelStatus
    {
        public string ChannelName { get; set; } = string.Empty;
        public int ActiveConnections { get; set; }          // 当前活动连接数
        public ulong TotalReceiveBytes { get; set; }        // 下行 bytes
        public ulong TotalSendBytes { get; set; }           // 上行 bytes      
        public long TotalRequestCount { get; set; }        // 总请求数

    }
    public class SystemStatus
    {
        public string GroupName { get; set; } = string.Empty;
        public List<ChannelStatus> Channels { get; set; } 
    }

    //用来获取每个服务的状态数据
    public interface IServicesStatus
    {
        List<SystemStatus> GetSystemStatus();

        int GetChannelCount();
        event Func<List<SystemStatus>> OnGetStatusEvent;
    }

    public class ServicesStatusImpl() : IServicesStatus
    {
        public event Func<List<SystemStatus>> OnGetStatusEvent;

        public int GetChannelCount()
        {
            return ChannelsHub.SessionCount;
        }

        public List<SystemStatus> GetSystemStatus() => this.OnGetStatusEvent();
    }

}
