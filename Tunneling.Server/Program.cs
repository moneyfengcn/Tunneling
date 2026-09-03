using MessagePack;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using Serilog.Events;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Tunneling.Core;
using Tunneling.Server.Framework;
using Tunneling.Server.Hubs;
using Tunneling.Server.Infrastructure;

/*
        sc create "TunnelingService" binPath= "C:\Publish\Tunneling.Service.exe" start= auto

        sc start TunnelingService      # 启动服务
        sc query TunnelingService      # 查看状态
        sc stop TunnelingService       # 停止
        sc delete TunnelingService     # 删除服务（卸载时用）

 */


namespace Tunneling.Server
{
    public class Program
    {
        private const string ServiceName = "TunnelingService";
        public static DateTime RunTime;
        public static string? Version = string.Empty;


        public static void Main(string[] args)
        {
            Program.RunTime = DateTime.Now;

            // 程序启动的时候在日志输出一下程序版本号
            Program.Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

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
                Run(args);
            }
        }


        public static void Run(string[] args)
        {
            //日志广播
            var inMemorySink = new SerilogInMemorySink();

            Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Information()
                            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                            .WriteTo.Console()
                            .Enrich.FromLogContext()
                            .WriteTo.Sink(inMemorySink)
                            .CreateLogger();

            Log.Information("Server 程序版本：{version}", Program.Version);

            //检查文件上传下载目录 如果不存在就建立
            var uploadPath = Path.Combine(AppContext.BaseDirectory, "downloads");
            if (!Path.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            // 运行主程序

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddMemoryCache();

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

            // 流式文件上传处理服务
            builder.Services.AddSingleton<IUploadFileManager, UploadFileManagerService>();
            #region 文件上传大小限制
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = long.MaxValue;
            });

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = long.MaxValue;
            });
            #endregion

            // 注入帐号相关
            builder.Services.Configure<SystemConfig>(builder.Configuration.GetSection("SystemConfig"));

            builder.Services.AddSerilog();
            // SignalR
            builder.Services.AddSignalR(options =>
            {
                // 省流量
                options.EnableDetailedErrors = false;
                options.DisableImplicitFromServicesParameters = true;
                options.MaximumReceiveMessageSize = 256 * 1024;

                //// 心跳间隔（默认 15 秒，推荐改成 10~15 秒）
                //options.KeepAliveInterval = TimeSpan.FromSeconds(8);
                //// 客户端多久没回应就认为断线（默认 30 秒，建议设为 KeepAliveInterval 的 2~3 倍）
                //options.ClientTimeoutInterval = TimeSpan.FromSeconds(20);
                //// 可选：提高并发上限（大并发必配）
                //// 阿里云 SLB + 防火墙默认 90～180 秒空闲断连
                //// 所以心跳必须 ≤ 30 秒，8～10 秒最稳             
                //options.HandshakeTimeout = TimeSpan.FromSeconds(15);
            })
            .AddMessagePackProtocol(options =>
            {
                options.SerializerOptions = new MessagePackSerializerOptions(MessagePack.Resolvers.StandardResolver.Instance);
            });

            // 配合 ASP.NET Core 响应压缩（gzip/br）
            builder.Services.AddResponseCompression(opts =>
            {
                //opts.EnableForHttps = true;
                opts.Providers.Add<BrotliCompressionProvider>();
                opts.Providers.Add<GzipCompressionProvider>();
            });

            // 注入隧道上传通道接口
            builder.Services.AddSingleton<ITunnelUploadChannel, TunnelUploadChannelServices>();
            builder.Services.AddSingleton<ITunnelSessionChannelServices, TunnelSessionChannelServices>();


            // 注入服务状态获取接口
            builder.Services.AddSingleton<IServicesStatus, ServicesStatusImpl>();

            builder.Services.AddTransient<ISystemStatus, SystemStatusServices>();

            // 把 sink 注入到 DI，以便其他服务使用
            builder.Services.AddSingleton(inMemorySink);

            // 注册广播服务，会订阅 sink 的事件并广播到 SignalR
            builder.Services.AddSingleton<LogBroadcastService>();


            // 注册 TCP监听服务 为后台服务
            builder.Services.AddHostedService<TcpServerService>();


            // 注册自定义 Token 认证方案（名称 "Token"）
            builder.Services.AddAuthentication("Token")
                .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("Token", options => { });

            // 注入网络流量统计服务
            builder.Services.AddSingleton<INetworkTraffic, NetworkTrafficServiceImpl>();

            // Cookie登录验证
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                                                .AddCookie(options =>
                                                {
                                                    options.LoginPath = "/Home/Login";      // 没登录时跳转
                                                                                            //options.AccessDeniedPath = "/Home/AccessDenied";
                                                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                                                    options.SlidingExpiration = true;
                                                });

            builder.Services.AddAuthorization();


            // 添加 MVC 并启用视图/数据注解本地化
            builder.Services.AddControllersWithViews();



            var app = builder.Build();

            app.UseRequestLocalization(); // 必须放在 UseRouting 之前

            // 确保 LogBroadcastService 被解析并初始化（订阅事件）
            _ = app.Services.GetRequiredService<LogBroadcastService>();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseRouting();

            app.UseResponseCompression();

            // 启用认证/授权中间件
            app.UseAuthentication();
            app.UseAuthorization();

            // 内外网客户端通信的信道
            app.MapHub<ChannelsHub>("/channels", options =>
            {
                // 仅使用 WebSockets 传输方式，禁用其他传输方式以提高性能和安全性
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
            });

            // 日志实时推送
            app.MapHub<LogHub>("/logs");

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
               .WithStaticAssets();

            app.Run();
        }
    }
}
