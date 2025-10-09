using Microsoft.AspNetCore.SignalR;

namespace DoAnChuyenNganh.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time notification delivery
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} connected to NotificationHub", userId);
                
                // Add user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
                
                // Remove user from their personal group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            if (exception != null)
            {
                _logger.LogError(exception, "User disconnected with error");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client can call this to confirm they received a notification
        /// </summary>
        public async Task ConfirmNotificationReceived(int notificationId)
        {
            var userId = Context.UserIdentifier;
            _logger.LogDebug("User {UserId} confirmed receipt of notification {NotificationId}", userId, notificationId);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Client can call this to request their current unread count
        /// </summary>
        public async Task RequestUnreadCount()
        {
            var userId = Context.UserIdentifier;
            _logger.LogDebug("User {UserId} requested unread count", userId);
            // The actual count will be sent by the NotificationService
            await Task.CompletedTask;
        }
    }
}

