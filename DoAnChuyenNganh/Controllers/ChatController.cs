using DoAnChuyenNganh.Services;
using DoAnChuyenNganh.ViewModels.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DoAnChuyenNganh.Hubs;

namespace DoAnChuyenNganh.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly IAuthService _authService;
        private readonly ILogger<ChatController> _logger;
        private readonly IHubContext<ChatHub> _chatHubContext;

        public ChatController(
            IChatService chatService,
            IAuthService authService,
            ILogger<ChatController> logger,
            IHubContext<ChatHub> chatHubContext)
        {
            _chatService = chatService;
            _authService = authService;
            _logger = logger;
            _chatHubContext = chatHubContext;
        }

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        private bool IsUserLoggedIn()
        {
            return GetCurrentUserId().HasValue;
        }

        #endregion

        #region Views

        /// <summary>
        /// Chat list page - shows all conversations
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId()!.Value;

            try
            {
                var model = await _chatService.GetUserConversationsAsync(userId, page, 20);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat list for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while loading your conversations.";
                return View(new ChatListViewModel());
            }
        }

        /// <summary>
        /// Conversation detail page - shows messages with a specific user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Conversation(int userId, int page = 1)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId()!.Value;

            try
            {
                var model = await _chatService.GetConversationDetailAsync(currentUserId, userId, page, 50);

                if (model == null)
                {
                    TempData["ErrorMessage"] = "User not found or conversation not accessible.";
                    return RedirectToAction("Index");
                }

                // Mark messages as read
                await _chatService.MarkMessagesAsReadAsync(currentUserId, userId);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation for user {CurrentUserId} with user {OtherUserId}", 
                    currentUserId, userId);
                TempData["ErrorMessage"] = "An error occurred while loading the conversation.";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region API Endpoints

        /// <summary>
        /// API: Send a message
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageViewModel model)
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            var userId = GetCurrentUserId()!.Value;

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid message data" });
            }

            try
            {
                var result = await _chatService.SendMessageAsync(
                    userId,
                    model.RecipientId,
                    model.MessageContent,
                    model.UploadedImages,
                    model.UploadedFiles);

                if (result.Success)
                {
                    var message = await _chatService.GetMessageByIdAsync(result.MessageId!.Value, userId);

                    // Broadcast message via SignalR to both sender and recipient
                    if (message != null)
                    {
                        // Send to sender (confirmation)
                        await _chatHubContext.Clients.User(userId.ToString()).SendAsync("ReceiveMessage", message);

                        // Send to recipient (if online)
                        await _chatHubContext.Clients.User(model.RecipientId.ToString()).SendAsync("ReceiveMessage", message);

                        // Update unread count for recipient
                        var unreadCount = await _chatService.GetUnreadMessageCountAsync(model.RecipientId);
                        await _chatHubContext.Clients.User(model.RecipientId.ToString()).SendAsync("UpdateUnreadCount", unreadCount);

                        _logger.LogInformation("Message {MessageId} broadcast via SignalR to sender {SenderId} and recipient {RecipientId}",
                            message.MessageId, userId, model.RecipientId);
                    }

                    return Json(new { success = true, message = result.Message, data = message });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from user {UserId} to user {RecipientId}",
                    userId, model.RecipientId);
                return Json(new { success = false, message = "An error occurred while sending the message" });
            }
        }

        /// <summary>
        /// API: Get conversation messages
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMessages(int otherUserId, int page = 1, int pageSize = 50)
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            var userId = GetCurrentUserId()!.Value;

            try
            {
                var messages = await _chatService.GetConversationMessagesAsync(userId, otherUserId, page, pageSize);
                var totalCount = await _chatService.GetConversationMessageCountAsync(userId, otherUserId);

                return Json(new
                {
                    success = true,
                    data = messages,
                    currentPage = page,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    totalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for user {UserId} with user {OtherUserId}", 
                    userId, otherUserId);
                return Json(new { success = false, message = "An error occurred while loading messages" });
            }
        }

        /// <summary>
        /// API: Get conversations list
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetConversations(int page = 1, int pageSize = 20)
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            var userId = GetCurrentUserId()!.Value;

            try
            {
                var model = await _chatService.GetUserConversationsAsync(userId, page, pageSize);
                return Json(new { success = true, data = model });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while loading conversations" });
            }
        }

        /// <summary>
        /// API: Search users for chat
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string searchTerm, int maxResults = 10)
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            var userId = GetCurrentUserId()!.Value;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new { success = false, message = "Search term is required" });
            }

            try
            {
                var users = await _chatService.SearchUsersForChatAsync(userId, searchTerm, maxResults);

                // Log the first user for debugging
                if (users.Any())
                {
                    var firstUser = users.First();
                    _logger.LogInformation("Search returned {Count} users. First user: OtherUserId={OtherUserId}, Username={Username}, FirstName={FirstName}, LastName={LastName}, FullName={FullName}",
                        users.Count, firstUser.OtherUserId, firstUser.Username, firstUser.FirstName, firstUser.LastName, firstUser.FullName);
                }

                return Json(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users for user {UserId} with term {SearchTerm}",
                    userId, searchTerm);
                return Json(new { success = false, message = "An error occurred while searching users" });
            }
        }

        /// <summary>
        /// API: Mark messages as read
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int senderId)
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            var userId = GetCurrentUserId()!.Value;

            try
            {
                var result = await _chatService.MarkMessagesAsReadAsync(userId, senderId);
                return Json(new { success = result.Success, message = result.Message, updatedCount = result.UpdatedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read for user {UserId} from user {SenderId}", 
                    userId, senderId);
                return Json(new { success = false, message = "An error occurred while marking messages as read" });
            }
        }

        /// <summary>
        /// API: Get unread message count
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            var userId = GetCurrentUserId()!.Value;

            try
            {
                var count = await _chatService.GetUnreadMessageCountAsync(userId);
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while getting unread count" });
            }
        }

        /// <summary>
        /// API: Delete a message
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            var userId = GetCurrentUserId()!.Value;

            try
            {
                var result = await _chatService.DeleteMessageAsync(messageId, userId);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId} for user {UserId}", messageId, userId);
                return Json(new { success = false, message = "An error occurred while deleting the message" });
            }
        }

        #endregion
    }
}

