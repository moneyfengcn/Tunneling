using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Tunneling.Server.Hubs
{
    [Authorize]
    public class LogHub : Hub<ILogEvents>
    {
        // 服务器端可以通过 IHubContext<LogHub> 调用 SendLog 向所有客户端广播
    }

    public interface ILogEvents
    {
        Task ReceiveLog(string message);
    }
}
