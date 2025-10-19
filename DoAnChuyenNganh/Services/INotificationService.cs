using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.Notification;

namespace DoAnChuyenNganh.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Creates a new notification for a user
        /// </summary>
        Task<(bool Success, int? NotificationId, string Message)> CreateNotificationAsync(
            int userId,
            string type,
            string title,
            string message,
            string? relatedItemType = null,
            int? relatedItemId = null);

        /// <summary>
        /// Gets all notifications for a user with pagination
        /// </summary>
        Task<NotificationListViewModel> GetUserNotificationsAsync(
            int userId,
            int page = 1,
            int pageSize = 20,
            string? filterType = null,
            bool? unreadOnly = null);

        /// <summary>
        /// Gets recent notifications for dropdown display
        /// </summary>
        Task<List<NotificationViewModel>> GetRecentNotificationsAsync(int userId, int count = 10);

        /// <summary>
        /// Gets the count of unread notifications for a user
        /// </summary>
        Task<int> GetUnreadCountAsync(int userId);

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        Task<(bool Success, string Message)> MarkAsReadAsync(int notificationId, int userId);

        /// <summary>
        /// Marks all notifications as read for a user
        /// </summary>
        Task<(bool Success, string Message)> MarkAllAsReadAsync(int userId);

        /// <summary>
        /// Deletes a notification
        /// </summary>
        Task<(bool Success, string Message)> DeleteNotificationAsync(int notificationId, int userId);

        /// <summary>
        /// Deletes all read notifications for a user
        /// </summary>
        Task<(bool Success, string Message)> DeleteAllReadAsync(int userId);

        /// <summary>
        /// Gets a single notification by ID
        /// </summary>
        Task<NotificationViewModel?> GetNotificationByIdAsync(int notificationId, int userId);

        #region Specific Notification Creators

        /// <summary>
        /// Creates a notification for a new forum reply
        /// </summary>
        Task CreateForumReplyNotificationAsync(int postId, int replyId, int replierUserId, int? parentReplyId = null);

        /// <summary>
        /// Creates a notification for a post like
        /// </summary>
        Task CreatePostLikeNotificationAsync(int postId, int likerUserId);

        /// <summary>
        /// Creates a notification for a reply like
        /// </summary>
        Task CreateReplyLikeNotificationAsync(int replyId, int likerUserId);

        /// <summary>
        /// Creates notifications for mentioned users in content
        /// </summary>
        Task CreateMentionNotificationsAsync(string content, int mentionerUserId, string itemType, int itemId);

        /// <summary>
        /// Creates a system notification for all users or specific role
        /// </summary>
        Task CreateSystemNotificationAsync(string title, string message, byte? roleId = null);

        #endregion
    }
}

