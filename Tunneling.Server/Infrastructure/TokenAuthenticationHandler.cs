
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks.Dataflow;

namespace Tunneling.Server.Infrastructure
{
    /// <summary>
    /// 自定义 Token 认证处理器， 为 channelHub 提供专门的 Token 认证
    /// </summary>
    public class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ILogger<TokenAuthenticationHandler> _logger;
        private readonly SystemConfig systemConfig;
        public TokenAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<SystemConfig> config) : base(options, logger, encoder)
        {
            systemConfig = config.Value;
            _logger = logger.CreateLogger<TokenAuthenticationHandler>();
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // 尝试从 Authorization header 读取 Bearer token
            string? token = null;
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var header = authHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(header) && header.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
                {
                    token = header.Substring("Bearer ".Length).Trim();
                }
            }

            // 如果没有，再从 query string 中读取 access_token（SignalR WebSocket 场景）
            if (string.IsNullOrEmpty(token) && Request.Query.TryGetValue("access_token", out var q))
            {
                token = q.FirstOrDefault();
            }

            // 如果没有读到Token，直接返回空
            if (string.IsNullOrEmpty(token))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (systemConfig.MapGroups.Count < 1)
            {
                return Task.FromResult(AuthenticateResult.Fail("读不到AccessToken，请在服务端的 appsettings.json 中配置"));
            }

            // 从配置读取 AccessToken
            var group = systemConfig.MapGroups.FirstOrDefault(a => string.Equals(token, a.AccessToken, StringComparison.Ordinal));

            if (group == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("非法 token"));
            }

            _logger.LogInformation("Token认证通过");

            var claims = new[] { new Claim(ClaimTypes.Name, group.GroupName) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
