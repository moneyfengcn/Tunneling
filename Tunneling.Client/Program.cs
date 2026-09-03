
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Reflection;
using System.Runtime.InteropServices;
using Tunneling.Core;
namespace Tunneling.Client
{
    /*
        docker run -d \
            --name tunneling-client \
            --restart always \
            --network host \
            -e Server__ServerAddress=http://1.2.3.4:1984 \
            -e Server__AccessToken=TOKEN \
            tunneling-client      
    */

    //docker build -t tunneling-client .

    public class AppConfig
    {
        public string ServerAddress { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;
        /// <summary>
        /// 流量控制，默认启用  避免内网过大流量冲垮隧道
        /// </summary>
        public int FlowControl { get; set; } = 10;
    }
    /*
    •	安装为windows服务（管理员权限命令提示符）：
        •	sc create TunnelingClient binPath= "C:\path\to\Tunneling.Client.exe" start= auto    
    •   启动服务  
        •	sc start TunnelingClient
    •	卸载：
        sc delete TunnelingClient
    */
    internal class Program
    {
        private const string ServiceName = "TunnelingClient";
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            // 程序启动的时候在日志输出一下程序版本号
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            Log.Information("TunnelingClient 程序版本：{version}", version);

            if (args.Any(a => string.Equals(a, "--install", StringComparison.OrdinalIgnoreCase)))
            {
                var exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
                Win32ServiceUtils.InstallWinService(ServiceName, exePath);
            }
            else if (args.Any(a => string.Equals(a, "--uninstall", StringComparison.OrdinalIgnoreCase)))
            {
                Win32ServiceUtils.UnInstallWinService(ServiceName);
            }
            else
            {
                //启动
                await Run(args);
            }
        }

        async static Task Run(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // 日志         
            builder.Services.AddSerilog(Log.Logger);

            // 配置
            builder.Services.Configure<AppConfig>(
                builder.Configuration.GetSection("Server"));

            // HostedService
            builder.Services.AddHostedService<SignalRHostedService>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // 注册为 Windows 服务
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = ServiceName;
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                builder.Services.AddSystemd();
            }


            var host = builder.Build();

            await host.RunAsync();
        }
    }
}

