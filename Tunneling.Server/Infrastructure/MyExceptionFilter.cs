using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Tunneling.Server.Infrastructure
{
    // Exception filter registered as a service (supports DI)
    public class MyExceptionFilter :  ExceptionFilterAttribute
    {
        private readonly ILogger<MyExceptionFilter> _logger;

        public MyExceptionFilter(ILogger<MyExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var ex = context.Exception;
            var http = context.HttpContext;
            var req = http.Request;
            var path = req.Path + req.QueryString;
            var user = http.User?.Identity?.Name ?? "anonymous";

            // Log full exception with request context
            _logger.LogError(ex, "Unhandled exception processing request {Method} {Path} by {User} {Message}", req.Method, path, user, ex.Message);

            // If the request expects JSON (AJAX or Accept header), return a simple JSON error response
            var accept = req.Headers["Accept"].ToString();
            var isAjax = req.Headers["X-Requested-With"] == "XMLHttpRequest";
            var wantsJson = !string.IsNullOrEmpty(accept) && accept.Contains("application/json");

            if (isAjax || wantsJson)
            {
                var json = new { error = "An unexpected error occurred." };
                context.Result = new JsonResult(json)
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                context.ExceptionHandled = true;
            }
        }
    }
}
