using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Principal;
using Tunneling.Server.Infrastructure;
using Tunneling.Server.Models;
using Tunneling.Server.Models.Home;
using Tunneling.Server;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Tunneling.Server.Controllers
{
    [Authorize]
    [TypeFilter(typeof(MyExceptionFilter))]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
  


        public HomeController(ILogger<HomeController> logger )
        {
            _logger = logger; 
        }

        public IActionResult Index()
        {
            return View();
        }

        // 获取运行状态信息的部分局部视图
        public IActionResult PartialStatus()
        {
            return PartialView();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginModel()
            {
                UserName = string.Empty,
                Password = string.Empty,
                RememberMe = true
            });
        }

        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login(LoginModel model, string? returnUrl
            , [FromServices] IOptions<SystemConfig> options)
        {
            SystemConfig config = options.Value;

            ModelState.Clear();
            if (string.IsNullOrWhiteSpace(model.UserName))
            {
                ModelState.AddModelError("username", "用户名不能为空");
                return View(model);
            }
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("password","请输入密码");
                return View(model);
            }

            // 明文比对
            bool isValid = string.Equals(model.UserName, config.UserName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(model.Password, config.Password);

            if (isValid)
            {
                _logger.LogInformation("帐号密码正确,登录成功 {0}", config.UserName);

                // 登录成功：创建身份认证票据（重要！）
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,  config.UserName),
                    new Claim(ClaimTypes.Role, "Administrator")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,                    // 记住我 
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                };

                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);   // 安全跳转
                }
                else
                {
                    return RedirectToAction("Index", "Home");  // 默认首页
                }
            }
            else
            {
                _logger.LogInformation("登录失败！ {0} {1}", model.UserName, model.Password);
                ModelState.AddModelError("Login failed", "登录失败");
                return View(model);
            }
        }
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        [HttpGet, AllowAnonymous]
        public IActionResult SetLanguage(string culture, string? returnUrl)
        {
            string[] language = new[]
            {
                "zh-CN",
                "zh-TW",
                "ja-JP",
                "en-US",
            };
            if (language.IndexOf(culture) < 0)
                culture = "en-US";

            Response.Cookies.Append(
                    ".AspNetCore.Culture",  // 必须是这个名字！默认就是这个
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.Now.AddYears(1) });

            return RedirectToAction("Login", "Home");
        }
    }
}
