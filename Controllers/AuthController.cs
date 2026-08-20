using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SupportPanel.Models;
using SupportPanel.Services;
using SupportPanel.ViewModels;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using SupportPanel.Interfaces;

namespace SupportPanel.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly PasswordHasher<User> _hasher = new();

        public AuthController(IUserService userService, IPasswordResetService passwordResetService)
        {
            _userService = userService;
            _passwordResetService = passwordResetService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.ResetSuccess = TempData["ResetSuccess"];
            return View();
        }

        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var loginIdentifier = model.LoginIdentifier.Trim();
            var user = await _userService.GetByEmailAsync(loginIdentifier)
                ?? await _userService.GetByUsernameAsync(loginIdentifier);

            if (user != null && user.Tenant != null && !user.Tenant.IsActive)
            {
                ModelState.AddModelError("", "Firmanız pasif durumda olduğu için giriş yapılamaz.");
                return View(model);
            }

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
                return View(model);
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash ?? "", model.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
                return View(model);
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _userService.HashPassword(user, model.Password);
                await _userService.UpdateAsync(user);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            if (!string.IsNullOrEmpty(user.Role?.Name))
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));

            if (user.TenantId.HasValue)
                claims.Add(new Claim("TenantId", user.TenantId.Value.ToString()));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var resetLinkBaseUrl = Url.Action("ResetPassword", "Auth", null, Request.Scheme)!;
            await _passwordResetService.RequestResetAsync(model.Email.Trim(), resetLinkBaseUrl);

            ViewBag.Submitted = true;
            return View(new ForgotPasswordViewModel());
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ResetPassword(string? token)
        {
            var isValid = !string.IsNullOrEmpty(token) &&
                await _passwordResetService.IsTokenValidAsync(token);

            if (!isValid)
            {
                ViewBag.InvalidToken = true;
                return View(new ResetPasswordViewModel());
            }

            return View(new ResetPasswordViewModel { Token = token! });
        }

        [HttpPost]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _passwordResetService.ResetPasswordAsync(model.Token, model.NewPassword);

            if (result == PasswordResetResult.InvalidOrExpiredToken)
            {
                ViewBag.InvalidToken = true;
                return View(new ResetPasswordViewModel());
            }

            TempData["ResetSuccess"] = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }
    }
}
