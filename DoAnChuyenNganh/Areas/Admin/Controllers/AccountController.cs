using DoAnChuyenNganh.Services;
using DoAnChuyenNganh.ViewModels.Auth;
using Microsoft.AspNetCore.Mvc;

namespace DoAnChuyenNganh.Areas.Admin.Controllers
{
    public class AccountController : AdminBaseController
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });

            var user = await _authService.GetUserByIdAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var model = new UserProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model, IFormFile? AvatarFile)
        {
            model.AvatarFile = AvatarFile;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.UpdateProfileAsync(model.UserId, model);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account", new { area = "" });

            var result = await _authService.ChangePasswordAsync(
                userId.Value,
                model.CurrentPassword,
                model.NewPassword,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Admin",
                Request.Headers["User-Agent"]!
            );

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction(nameof(ChangePassword));
        }
    }
}
