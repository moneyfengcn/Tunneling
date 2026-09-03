namespace Tunneling.Server.Models.Status
{
    public class ApiResult<T>
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class ServerInfo
    {
        public string RunTime { get; set; }

        public double Upload { get; set; }
        public double Download { get; set; }
        public int NodeCount { get; set; }
    }
    public class SessionInfo
    {
        public string Name { get; set; }
        public bool Status { get; set; }
        public int SessionsCount { get; set; }
        public ulong UpCount { get; set; }
        public ulong DownCount { get; set; }
        public long TotalSessions { get; set; }
    }

    public class ChannelStatus
    {
        public string ChannelName { get; set; }
        public int ChannelCount { get; set; }

        public List<SessionInfo> SessionInfos { get; set; }

    }
    public class DashboardInfo
    {
        public ServerInfo ServerInfo { get; set; }
        public List<ChannelStatus> Channels { get; set; }
    }
}
