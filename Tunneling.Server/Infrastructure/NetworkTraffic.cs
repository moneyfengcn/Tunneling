using System.Net.NetworkInformation;

namespace Tunneling.Server.Infrastructure
{
    static internal class NetworkTraffic
    {
        private static long _lastBytesReceived = 0;
        private static long _lastBytesSent = 0;
        private static DateTime _lastTime = DateTime.UtcNow;

        // 获取网卡上下行流量
        public static (double downloadMbps, double uploadMbps) GetRealTimeSpeed()
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .OrderByDescending(n => n.GetIPv4Statistics().BytesReceived)
                .FirstOrDefault();

            if (nic == null) return (0, 0);

            long received = nic.GetIPv4Statistics().BytesReceived;
            long sent = nic.GetIPv4Statistics().BytesSent;
            var now = DateTime.UtcNow;
            var timeDiff = (now - _lastTime).TotalSeconds;

            if (timeDiff < 0.1) return (0, 0); // 太快了，防抖

            double downSpeed = (received - _lastBytesReceived) * 8 / (timeDiff * 1_000_000); // Mbps
            double upSpeed = (sent - _lastBytesSent) * 8 / (timeDiff * 1_000_000);

            _lastBytesReceived = received;
            _lastBytesSent = sent;
            _lastTime = now;

            return (downSpeed, upSpeed);
        }
    }
}
