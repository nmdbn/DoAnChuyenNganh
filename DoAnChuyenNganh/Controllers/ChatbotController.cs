using DoAnChuyenNganh.Services;
using DoAnChuyenNganh.ViewModels.Chatbot;
using Microsoft.AspNetCore.Mvc;

namespace DoAnChuyenNganh.Controllers
{
    /// <summary>
    /// Controller for AI Chatbot feature
    /// Frontend-only implementation - integrates with external AI Chatbot API
    /// </summary>
    public class ChatbotController : Controller
    {
        private readonly ILogger<ChatbotController> _logger;
        private readonly IAuthService _authService;

        public ChatbotController(ILogger<ChatbotController> logger, IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        #region Helper Methods

        /// <summary>
        /// Get the current logged-in user's ID from session
        /// </summary>
        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        private bool IsUserLoggedIn()
        {
            return GetCurrentUserId().HasValue;
        }

        #endregion

        #region Views

        /// <summary>
        /// AI Chatbot main page
        /// Displays the chatbot interface for authenticated users
        /// Frontend integrates with external API at http://localhost:8000
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Check authentication
            if (!IsUserLoggedIn())
            {
                _logger.LogWarning("Unauthenticated user attempted to access chatbot");
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId()!.Value;
            _logger.LogInformation("User {UserId} accessed AI Chatbot", userId);

            // Get user avatar from session or database
            var userAvatarUrl = HttpContext.Session.GetString("AvatarUrl");

            // If not in session, get from database
            if (string.IsNullOrEmpty(userAvatarUrl))
            {
                var user = await _authService.GetUserByIdAsync(userId);
                userAvatarUrl = user?.AvatarUrl;
            }

            // Create view model with user ID and avatar for JavaScript initialization
            var model = new AIChatbotViewModel
            {
                CurrentUserId = userId,
                UserAvatarUrl = userAvatarUrl
            };

            return View(model);
        }

        #endregion
    }
}

