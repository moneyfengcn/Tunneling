
using Tunneling.Server.Infrastructure;

namespace Tunneling.Server.Framework
{
    public interface INetworkTraffic
    {
        (double downloadMbps, double uploadMbps) GetRealTimeSpeed();
    }

    public class NetworkTrafficServiceImpl : INetworkTraffic, IDisposable
    {
        private double _downloadMbps = 0;
        private double _uploadMbps = 0;
        private readonly System.Threading.Timer _timer;

        public NetworkTrafficServiceImpl()
        {
            _timer = new Timer(OnTimeCallback, this, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void OnTimeCallback(object? state)
        {
            (_downloadMbps, _uploadMbps) = NetworkTraffic.GetRealTimeSpeed();
        }

        public (double downloadMbps, double uploadMbps) GetRealTimeSpeed()
        {
            return (_downloadMbps, _uploadMbps);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
