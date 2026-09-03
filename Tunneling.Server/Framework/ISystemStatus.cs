using Tunneling.Server.Models.Status;

namespace Tunneling.Server.Framework
{
    public interface ISystemStatus
    {
        DashboardInfo GetServerInfo();
    }
}
