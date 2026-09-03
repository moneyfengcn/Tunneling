using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tunneling.Server.Framework;
using Tunneling.Server.Infrastructure;
using Tunneling.Server.Models.Status;

namespace Tunneling.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatusController : ControllerBase
    {

        [HttpGet]
        public ApiResult<DashboardInfo> GetServerInfo([FromServices] ISystemStatus systemStatus)
        {
            var status = systemStatus.GetServerInfo();

            return new ApiResult<DashboardInfo>()
            {
                Data = status,
                Message = "Success",
                Status = true
            };
        }
    }
}