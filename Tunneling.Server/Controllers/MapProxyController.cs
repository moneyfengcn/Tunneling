using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tunneling.Server.Infrastructure;
using Tunneling.Server.Models.MapProxy;

namespace Tunneling.Server.Controllers
{
    [Authorize]
    public class MapProxyController : Controller
    {
        private readonly ILogger<MapProxyController> _logger;
        private readonly SystemConfig _systemConfig;

        public MapProxyController(ILogger<MapProxyController> logger, IOptions<SystemConfig> options)
        {
            _logger = logger;
            _systemConfig = options.Value;
        }
        public IActionResult Index()
        {
            return View(_systemConfig);
        }
    }
}
