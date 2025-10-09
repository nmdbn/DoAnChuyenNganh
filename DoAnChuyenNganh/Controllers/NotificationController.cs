using DoAnChuyenNganh.Services;
using Microsoft.AspNetCore.Mvc;

namespace DoAnChuyenNganh.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        private bool IsAuthenticated()
        {
            return GetCurrentUserId().HasValue;
        }

        #endregion

        #region Views

        // GET: /Notification
        public async Task<IActionResult> Index(int page = 1, string? filterType = null, bool unreadOnly = false)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId()!.Value;
            var model = await _notificationService.GetUserNotificationsAsync(userId, page, 20, filterType, unreadOnly);

            return View(model);
        }

        #endregion

        #region API Endpoints

        // GET: /Notification/GetRecent
        [HttpGet]
        public async Task<IActionResult> GetRecent(int count = 10)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var userId = GetCurrentUserId()!.Value;
            var notifications = await _notificationService.GetRecentNotificationsAsync(userId, count);

            return Json(new { success = true, notifications });
        }

        // GET: /Notification/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, count = 0 });
            }

            var userId = GetCurrentUserId()!.Value;
            var count = await _notificationService.GetUnreadCountAsync(userId);

            return Json(new { success = true, count });
        }

        // POST: /Notification/MarkAsRead/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _notificationService.MarkAsReadAsync(id, userId);

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Notification/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _notificationService.MarkAllAsReadAsync(userId);

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Notification/Delete/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _notificationService.DeleteNotificationAsync(id, userId);

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Notification/DeleteAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllRead()
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _notificationService.DeleteAllReadAsync(userId);

            return Json(new { success = result.Success, message = result.Message });
        }

        // GET: /Notification/Details/123
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var userId = GetCurrentUserId()!.Value;
            var notification = await _notificationService.GetNotificationByIdAsync(id, userId);

            if (notification == null)
            {
                return Json(new { success = false, message = "Notification not found" });
            }

            return Json(new { success = true, notification });
        }

        #endregion
    }
}

