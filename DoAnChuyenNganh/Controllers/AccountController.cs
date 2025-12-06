using DoAnChuyenNganh.Services;
using DoAnChuyenNganh.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DoAnChuyenNganh.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;
        private const string SessionCookieName = "AuthSession";

        public AccountController(IAuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        #region Login

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Check if already logged in
            if (IsAuthenticated())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var ipAddress = GetIpAddress();
                var userAgent = GetUserAgent();

                var result = await _authService.AuthenticateAsync(
                    model.UsernameOrEmail,
                    model.Password,
                    ipAddress,
                    userAgent);

                if (!result.Success || result.User == null)
                {
                    // Tạm in message ra log
                    _logger.LogError("Login failed: {Message}", result.Message);
                    ModelState.AddModelError(string.Empty, result.Message ?? "Login failed.");
                    return View(model);
                }

                var session = await _authService.CreateSessionAsync(
                    result.User.UserId,
                    ipAddress,
                    userAgent,
                    model.RememberMe);

                SetSessionCookie(session.SessionToken, model.RememberMe);

                HttpContext.Session.SetInt32("UserId", result.User.UserId);
                HttpContext.Session.SetInt32("RoleId", result.User.RoleId);
                HttpContext.Session.SetString("Username", result.User.Username);


                HttpContext.Session.SetString("Email", result.User.Email);
                HttpContext.Session.SetString("FullName", $"{result.User.FirstName} {result.User.LastName}");
                HttpContext.Session.SetString("RoleName", result.User.Role.RoleName);
                if (!string.IsNullOrEmpty(result.User.AvatarUrl))
                {
                    HttpContext.Session.SetString("AvatarUrl", result.User.AvatarUrl);
                }

                TempData["SuccessMessage"] = "Login successful!";

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while logging in");
                ModelState.AddModelError(string.Empty, $"Internal error: {ex.Message}");
                return View(model);
            }
        }


        #endregion

        #region Register

        [HttpGet]
        public IActionResult Register()
        {
            // Check if already logged in
            if (IsAuthenticated())
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            var result = await _authService.RegisterAsync(model, ipAddress, userAgent);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction(nameof(Login));
        }

        #endregion

        #region Logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var sessionToken = Request.Cookies[SessionCookieName];
            if (!string.IsNullOrEmpty(sessionToken))
            {
                await _authService.InvalidateSessionAsync(sessionToken);
            }

            // Clear session
            HttpContext.Session.Clear();

            // Remove cookie
            Response.Cookies.Delete(SessionCookieName);

            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Change Password

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            var result = await _authService.ChangePasswordAsync(
                userId.Value,
                model.CurrentPassword,
                model.NewPassword,
                ipAddress,
                userAgent);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction(nameof(Profile));
        }

        #endregion

        #region Forgot Password

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (IsAuthenticated())
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.GeneratePasswordResetTokenAsync(model.Email);

            // Always show success message for security
            TempData["SuccessMessage"] = "If the email exists, a password reset link has been sent.";
            return RedirectToAction(nameof(Login));
        }

        #endregion

        #region Reset Password

        [HttpGet]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (IsAuthenticated())
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            var result = await _authService.ResetPasswordAsync(
                model.Email,
                model.Token,
                model.Password,
                ipAddress,
                userAgent);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Password reset successfully! Please login.";
            return RedirectToAction(nameof(Login));
        }

        #endregion

        #region Profile

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction(nameof(Login));
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _authService.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new UserProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                IsEmailVerified = user.IsEmailVerified ?? false,
                RoleName = user.Role.RoleName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model, IFormFile? AvatarFile)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction(nameof(Login));
            }

            // Attach avatar file to model
            model.AvatarFile = AvatarFile;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || userId.Value != model.UserId)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _authService.UpdateProfileAsync(model.UserId, model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            // Update session data
            HttpContext.Session.SetString("Username", model.Username);
            HttpContext.Session.SetString("Email", model.Email);
            HttpContext.Session.SetString("FullName", $"{model.FirstName} {model.LastName}");

            // Update avatar URL in session
            var updatedUser = await _authService.GetUserByIdAsync(model.UserId);
            if (updatedUser != null && !string.IsNullOrEmpty(updatedUser.AvatarUrl))
            {
                HttpContext.Session.SetString("AvatarUrl", updatedUser.AvatarUrl);
            }
            else
            {
                HttpContext.Session.Remove("AvatarUrl");
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        #endregion

        #region Helper Methods

        private bool IsAuthenticated()
        {
            var sessionToken = Request.Cookies[SessionCookieName];
            return !string.IsNullOrEmpty(sessionToken) && HttpContext.Session.GetInt32("UserId").HasValue;
        }

        private void SetSessionCookie(string sessionToken, bool rememberMe)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = rememberMe ? DateTimeOffset.Now.AddDays(30) : DateTimeOffset.Now.AddHours(24)
            };

            Response.Cookies.Append(SessionCookieName, sessionToken, cookieOptions);
        }

        private string GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }

        #endregion

        #region Google Login

        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(GoogleResponse), "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            // Authenticate with Google scheme
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (result?.Succeeded != true || result.Principal == null)
            {
                TempData["ErrorMessage"] = "Google login failed. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            var claims = result.Principal?.Identities.FirstOrDefault()?.Claims;

            if (claims == null)
            {
                TempData["ErrorMessage"] = "Google login failed. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var providerId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerId))
            {
                TempData["ErrorMessage"] = "Google did not provide sufficient information.";
                return RedirectToAction(nameof(Login));
            }

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            // Xử lý đăng nhập hoặc tạo user mới
            var resultAuth = await _authService.AuthenticateGoogleAsync(
                email,
                name ?? "Google User",
                providerId,
                ipAddress,
                userAgent);

            if (!resultAuth.Success || resultAuth.User == null)
            {
                TempData["ErrorMessage"] = resultAuth.Message;
                return RedirectToAction(nameof(Login));
            }

            // Tạo session và cookie
            var session = await _authService.CreateSessionAsync(
                resultAuth.User.UserId,
                ipAddress,
                userAgent,
                rememberMe: true);

            SetSessionCookie(session.SessionToken, rememberMe: true);

            // Lưu session user
            HttpContext.Session.SetInt32("UserId", resultAuth.User.UserId);
            HttpContext.Session.SetInt32("RoleId", resultAuth.User.RoleId);
            HttpContext.Session.SetString("Username", resultAuth.User.Username);
            HttpContext.Session.SetString("Email", resultAuth.User.Email);
            HttpContext.Session.SetString("FullName", $"{resultAuth.User.FirstName} {resultAuth.User.LastName}");
            HttpContext.Session.SetString("RoleName", resultAuth.User.Role?.RoleName ?? "User");
            if (!string.IsNullOrEmpty(resultAuth.User.AvatarUrl))
            {
                HttpContext.Session.SetString("AvatarUrl", resultAuth.User.AvatarUrl);
            }

            await HttpContext.Session.CommitAsync();

            _logger.LogInformation("Google login successful - UserId: {UserId}, RoleId: {RoleId}, Username: {Username}",
                resultAuth.User.UserId, resultAuth.User.RoleId, resultAuth.User.Username);

            TempData["SuccessMessage"] = "Login with Google successful!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region Facebbook Login
        [HttpGet]
        public IActionResult FacebookLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(FacebookResponse), "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }
        [HttpGet]
        public async Task<IActionResult> FacebookResponse(string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);

            if (result?.Succeeded != true || result.Principal == null)
            {
                TempData["ErrorMessage"] = "Facebook login failed. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            if (claims == null)
            {
                TempData["ErrorMessage"] = "Facebook login failed. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var providerId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var picture = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (string.IsNullOrEmpty(providerId))
            {
                TempData["ErrorMessage"] = "Facebook did not provide sufficient information.";
                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                email = $"{providerId}@facebook.local";
            }

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            var resultAuth = await _authService.AuthenticateFacebookAsync(
                email,
                name ?? "Facebook User",
                providerId,
                ipAddress,
                userAgent);

            if (!resultAuth.Success || resultAuth.User == null)
            {
                TempData["ErrorMessage"] = resultAuth.Message;
                return RedirectToAction(nameof(Login));
            }

            var session = await _authService.CreateSessionAsync(
                resultAuth.User.UserId,
                ipAddress,
                userAgent,
                rememberMe: true);

            SetSessionCookie(session.SessionToken, rememberMe: true);

            HttpContext.Session.SetInt32("UserId", resultAuth.User.UserId);
            HttpContext.Session.SetInt32("RoleId", resultAuth.User.RoleId);
            HttpContext.Session.SetString("Username", resultAuth.User.Username);
            HttpContext.Session.SetString("Email", resultAuth.User.Email ?? "");
            HttpContext.Session.SetString("FullName", $"{resultAuth.User.FirstName} {resultAuth.User.LastName}");
            HttpContext.Session.SetString("RoleName", resultAuth.User.Role?.RoleName ?? "User");

            if (!string.IsNullOrEmpty(picture))
            {
                HttpContext.Session.SetString("AvatarUrl", picture);
            }

            await HttpContext.Session.CommitAsync();

            _logger.LogInformation("Facebook login successful - UserId: {UserId}, RoleId: {RoleId}, Username: {Username}",
                resultAuth.User.UserId, resultAuth.User.RoleId, resultAuth.User.Username);

            TempData["SuccessMessage"] = "Login with Facebook successful!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
        #endregion
    }
}
