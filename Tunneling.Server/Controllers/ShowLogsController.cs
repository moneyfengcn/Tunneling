using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tunneling.Server.Controllers
{
    [Authorize]
    public class ShowLogsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
