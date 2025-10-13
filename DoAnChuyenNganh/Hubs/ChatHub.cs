using DoAnChuyenNganh.Services;
using DoAnChuyenNganh.ViewModels.Chat;
using Microsoft.AspNetCore.SignalR;

namespace DoAnChuyenNganh.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time chat functionality
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IChatService _chatService;
        private readonly INotificationService _notificationService;

        public ChatHub(
            ILogger<ChatHub> logger,
            IChatService chatService,
            INotificationService notificationService)
        {
            _logger = logger;
            _chatService = chatService;
            _notificationService = notificationService;
        }

        #region Connection Management

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _logger.LogInformation("User {UserId} connected to ChatHub with connection {ConnectionId}", userId, Context.ConnectionId);

                // Track user connection
                await _chatService.UserConnectedAsync(userIdInt, Context.ConnectionId);

                // Add user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                // Notify all clients about online users count update
                var onlineCount = await _chatService.GetOnlineUsersCountAsync();
                await Clients.All.SendAsync("UpdateOnlineUsersCount", onlineCount);

                // Notify user's contacts that they are online
                await Clients.Others.SendAsync("UserOnline", userIdInt);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _logger.LogInformation("User {UserId} disconnected from ChatHub with connection {ConnectionId}", userId, Context.ConnectionId);

                // Remove user connection tracking
                await _chatService.UserDisconnectedAsync(userIdInt, Context.ConnectionId);

                // Check if user is still online (might have multiple connections)
                var isStillOnline = await _chatService.IsUserOnlineAsync(userIdInt);

                if (!isStillOnline)
                {
                    // Notify user's contacts that they are offline
                    await Clients.Others.SendAsync("UserOffline", userIdInt);
                }

                // Update online users count
                var onlineCount = await _chatService.GetOnlineUsersCountAsync();
                await Clients.All.SendAsync("UpdateOnlineUsersCount", onlineCount);
            }

            await base.OnDisconnectedAsync(exception);
        }

        #endregion

        #region Message Operations

        /// <summary>
        /// Send a chat message to another user
        /// </summary>
        public async Task SendMessage(int recipientId, string messageContent)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int senderIdInt))
            {
                await Clients.Caller.SendAsync("Error", "Invalid user session");
                return;
            }

            try
            {
                // Send message through service
                var result = await _chatService.SendMessageAsync(senderIdInt, recipientId, messageContent);

                if (result.Success && result.MessageId.HasValue)
                {
                    // Get the full message details
                    var message = await _chatService.GetMessageByIdAsync(result.MessageId.Value, senderIdInt);

                    if (message != null)
                    {
                        // Send to sender (confirmation)
                        await Clients.Caller.SendAsync("ReceiveMessage", message);

                        // Send to recipient (if online)
                        await Clients.User(recipientId.ToString()).SendAsync("ReceiveMessage", message);

                        // Update unread count for recipient
                        var unreadCount = await _chatService.GetUnreadMessageCountAsync(recipientId);
                        await Clients.User(recipientId.ToString()).SendAsync("UpdateUnreadCount", unreadCount);

                        _logger.LogInformation("Message {MessageId} sent from user {SenderId} to user {RecipientId}", 
                            result.MessageId, senderIdInt, recipientId);
                    }
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from user {SenderId} to user {RecipientId}", senderIdInt, recipientId);
                await Clients.Caller.SendAsync("Error", "An error occurred while sending the message");
            }
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        public async Task MarkMessagesAsRead(int senderId)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int recipientIdInt))
            {
                return;
            }

            try
            {
                var result = await _chatService.MarkMessagesAsReadAsync(recipientIdInt, senderId);

                if (result.Success && result.UpdatedCount > 0)
                {
                    // Notify sender that their messages were read
                    await Clients.User(senderId.ToString()).SendAsync("MessagesRead", recipientIdInt);

                    // Update unread count for recipient
                    var unreadCount = await _chatService.GetUnreadMessageCountAsync(recipientIdInt);
                    await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);

                    _logger.LogInformation("User {RecipientId} marked {Count} messages as read from user {SenderId}", 
                        recipientIdInt, result.UpdatedCount, senderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read for user {RecipientId} from user {SenderId}", 
                    recipientIdInt, senderId);
            }
        }

        #endregion

        #region Typing Indicators

        /// <summary>
        /// Notify another user that current user is typing
        /// </summary>
        public async Task UserTyping(int recipientId)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int senderIdInt))
            {
                await Clients.User(recipientId.ToString()).SendAsync("UserTyping", senderIdInt);
                _logger.LogDebug("User {SenderId} is typing to user {RecipientId}", senderIdInt, recipientId);
            }
        }

        /// <summary>
        /// Notify another user that current user stopped typing
        /// </summary>
        public async Task UserStoppedTyping(int recipientId)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int senderIdInt))
            {
                await Clients.User(recipientId.ToString()).SendAsync("UserStoppedTyping", senderIdInt);
                _logger.LogDebug("User {SenderId} stopped typing to user {RecipientId}", senderIdInt, recipientId);
            }
        }

        #endregion

        #region Online Status

        /// <summary>
        /// Get current online users count
        /// </summary>
        public async Task RequestOnlineUsersCount()
        {
            var count = await _chatService.GetOnlineUsersCountAsync();
            await Clients.Caller.SendAsync("UpdateOnlineUsersCount", count);
        }

        /// <summary>
        /// Check if a specific user is online
        /// </summary>
        public async Task CheckUserOnlineStatus(int userId)
        {
            var isOnline = await _chatService.IsUserOnlineAsync(userId);
            await Clients.Caller.SendAsync("UserOnlineStatus", userId, isOnline);
        }

        /// <summary>
        /// Get list of online users
        /// </summary>
        public async Task RequestOnlineUsers()
        {
            var onlineUsers = await _chatService.GetOnlineUsersAsync();
            await Clients.Caller.SendAsync("OnlineUsersList", onlineUsers);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Client can call this to confirm they received a message
        /// </summary>
        public async Task ConfirmMessageReceived(int messageId)
        {
            var userId = Context.UserIdentifier;
            _logger.LogDebug("User {UserId} confirmed receipt of message {MessageId}", userId, messageId);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Request unread message count
        /// </summary>
        public async Task RequestUnreadCount()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                var unreadCount = await _chatService.GetUnreadMessageCountAsync(userIdInt);
                await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);
            }
        }

        #endregion
    }
}

