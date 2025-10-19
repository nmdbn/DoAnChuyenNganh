using DoAnChuyenNganh.Hubs;
using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.Notification;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DoAnChuyenNganh.Services
{
    public class NotificationService : INotificationService
    {
        private readonly DoAnChuyenNganhContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            DoAnChuyenNganhContext context,
            ILogger<NotificationService> logger,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        #region Core Notification Operations

        public async Task<(bool Success, int? NotificationId, string Message)> CreateNotificationAsync(
            int userId,
            string type,
            string title,
            string message,
            string? relatedItemType = null,
            int? relatedItemId = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    RelatedItemType = relatedItemType,
                    RelatedItemId = relatedItemId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time notification via SignalR
                var notificationViewModel = MapToViewModel(notification);
                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("ReceiveNotification", notificationViewModel);

                // Also send updated unread count
                var unreadCount = await GetUnreadCountAsync(userId);
                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("UpdateUnreadCount", unreadCount);

                _logger.LogInformation("Notification created and sent via SignalR for user {UserId}: {Title}", userId, title);

                return (true, notification.NotificationId, "Notification created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
                return (false, null, "An error occurred while creating the notification");
            }
        }

        public async Task<NotificationListViewModel> GetUserNotificationsAsync(
            int userId,
            int page = 1,
            int pageSize = 20,
            string? filterType = null,
            bool? unreadOnly = null)
        {
            try
            {
                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                // Apply filters
                if (!string.IsNullOrEmpty(filterType) && filterType != "all")
                {
                    query = query.Where(n => n.Type == filterType);
                }

                if (unreadOnly == true)
                {
                    query = query.Where(n => n.IsRead == false);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new NotificationListViewModel
                {
                    Notifications = notifications.Select(MapToViewModel).ToList(),
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    FilterType = filterType,
                    UnreadOnly = unreadOnly ?? false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                return new NotificationListViewModel();
            }
        }

        public async Task<List<NotificationViewModel>> GetRecentNotificationsAsync(int userId, int count = 10)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return notifications.Select(MapToViewModel).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent notifications for user {UserId}", userId);
                return new List<NotificationViewModel>();
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<(bool Success, string Message)> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                if (notification == null)
                {
                    return (false, "Notification not found");
                }

                if (notification.IsRead == true)
                {
                    return (true, "Notification already marked as read");
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Update unread count via SignalR
                var unreadCount = await GetUnreadCountAsync(userId);
                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("UpdateUnreadCount", unreadCount);

                return (true, "Notification marked as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
                return (false, "An error occurred");
            }
        }

        public async Task<(bool Success, string Message)> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Update unread count via SignalR
                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("UpdateUnreadCount", 0);

                return (true, $"{unreadNotifications.Count} notifications marked as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return (false, "An error occurred");
            }
        }

        public async Task<(bool Success, string Message)> DeleteNotificationAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                if (notification == null)
                {
                    return (false, "Notification not found");
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                // Update unread count if it was unread
                if (notification.IsRead == false)
                {
                    var unreadCount = await GetUnreadCountAsync(userId);
                    await _hubContext.Clients.User(userId.ToString())
                        .SendAsync("UpdateUnreadCount", unreadCount);
                }

                return (true, "Notification deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
                return (false, "An error occurred");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAllReadAsync(int userId)
        {
            try
            {
                var readNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == true)
                    .ToListAsync();

                _context.Notifications.RemoveRange(readNotifications);
                await _context.SaveChangesAsync();

                return (true, $"{readNotifications.Count} notifications deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting read notifications for user {UserId}", userId);
                return (false, "An error occurred");
            }
        }

        public async Task<NotificationViewModel?> GetNotificationByIdAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                return notification != null ? MapToViewModel(notification) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification {NotificationId}", notificationId);
                return null;
            }
        }

        #endregion

        #region Specific Notification Creators

        public async Task CreateForumReplyNotificationAsync(int postId, int replyId, int replierUserId, int? parentReplyId = null)
        {
            try
            {
                var post = await _context.ForumPosts
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PostId == postId);

                if (post == null) return;

                var replier = await _context.Users.FindAsync(replierUserId);
                if (replier == null) return;

                // Notify post author (if not the replier)
                if (post.UserId != replierUserId)
                {
                    await CreateNotificationAsync(
                        post.UserId,
                        "forum_reply",
                        "New Reply to Your Post",
                        $"{replier.Username} replied to your post: {TruncateText(post.Title, 50)}",
                        "post",
                        postId);
                }

                // If it's a reply to another reply, notify the parent reply author
                if (parentReplyId.HasValue)
                {
                    var parentReply = await _context.ForumReplies
                        .Include(r => r.User)
                        .FirstOrDefaultAsync(r => r.ReplyId == parentReplyId.Value);

                    if (parentReply != null && parentReply.UserId != replierUserId && parentReply.UserId != post.UserId)
                    {
                        await CreateNotificationAsync(
                            parentReply.UserId,
                            "forum_reply",
                            "New Reply to Your Comment",
                            $"{replier.Username} replied to your comment on: {TruncateText(post.Title, 50)}",
                            "reply",
                            replyId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating forum reply notification");
            }
        }

        public async Task CreatePostLikeNotificationAsync(int postId, int likerUserId)
        {
            try
            {
                _logger.LogInformation("Creating post like notification for postId={PostId}, likerUserId={LikerUserId}", postId, likerUserId);

                var post = await _context.ForumPosts
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PostId == postId);

                if (post == null)
                {
                    _logger.LogWarning("Post {PostId} not found for like notification", postId);
                    return;
                }

                if (post.UserId == likerUserId)
                {
                    _logger.LogDebug("User {UserId} liked their own post, skipping notification", likerUserId);
                    return;
                }

                var liker = await _context.Users.FindAsync(likerUserId);
                if (liker == null)
                {
                    _logger.LogWarning("Liker user {UserId} not found", likerUserId);
                    return;
                }

                _logger.LogInformation("Sending post like notification to user {UserId} from {LikerUsername}", post.UserId, liker.Username);

                await CreateNotificationAsync(
                    post.UserId,
                    "post_like",
                    "Someone Liked Your Post",
                    $"{liker.Username} liked your post: {TruncateText(post.Title, 50)}",
                    "post",
                    postId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post like notification for postId={PostId}", postId);
            }
        }

        public async Task CreateReplyLikeNotificationAsync(int replyId, int likerUserId)
        {
            try
            {
                _logger.LogInformation("Creating reply like notification for replyId={ReplyId}, likerUserId={LikerUserId}", replyId, likerUserId);

                var reply = await _context.ForumReplies
                    .Include(r => r.User)
                    .Include(r => r.Post)
                    .FirstOrDefaultAsync(r => r.ReplyId == replyId);

                if (reply == null)
                {
                    _logger.LogWarning("Reply {ReplyId} not found for like notification", replyId);
                    return;
                }

                if (reply.UserId == likerUserId)
                {
                    _logger.LogDebug("User {UserId} liked their own reply, skipping notification", likerUserId);
                    return;
                }

                var liker = await _context.Users.FindAsync(likerUserId);
                if (liker == null)
                {
                    _logger.LogWarning("Liker user {UserId} not found", likerUserId);
                    return;
                }

                _logger.LogInformation("Sending reply like notification to user {UserId} from {LikerUsername}", reply.UserId, liker.Username);

                await CreateNotificationAsync(
                    reply.UserId,
                    "reply_like",
                    "Someone Liked Your Comment",
                    $"{liker.Username} liked your comment on: {TruncateText(reply.Post.Title, 50)}",
                    "reply",
                    replyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reply like notification for replyId={ReplyId}", replyId);
            }
        }

        public async Task CreateMentionNotificationsAsync(string content, int mentionerUserId, string itemType, int itemId)
        {
            try
            {
                // Extract @mentions from content
                var mentionPattern = @"@(\w+)";
                var matches = Regex.Matches(content, mentionPattern);

                var mentioner = await _context.Users.FindAsync(mentionerUserId);
                if (mentioner == null) return;

                var uniqueUsernames = matches.Select(m => m.Groups[1].Value).Distinct();

                foreach (var username in uniqueUsernames)
                {
                    var mentionedUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == username);

                    if (mentionedUser != null && mentionedUser.UserId != mentionerUserId)
                    {
                        await CreateNotificationAsync(
                            mentionedUser.UserId,
                            "mention",
                            "You Were Mentioned",
                            $"{mentioner.Username} mentioned you in a {itemType}",
                            itemType,
                            itemId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mention notifications");
            }
        }

        public async Task CreateSystemNotificationAsync(string title, string message, byte? roleId = null)
        {
            try
            {
                var query = _context.Users.Where(u => u.IsActive == true);

                if (roleId.HasValue)
                {
                    query = query.Where(u => u.RoleId == roleId.Value);
                }

                var users = await query.ToListAsync();

                foreach (var user in users)
                {
                    await CreateNotificationAsync(
                        user.UserId,
                        "system_alert",
                        title,
                        message);
                }

                _logger.LogInformation("System notification sent to {Count} users", users.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system notification");
            }
        }

        #endregion

        #region Helper Methods

        private NotificationViewModel MapToViewModel(Notification notification)
        {
            return new NotificationViewModel
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                RelatedItemType = notification.RelatedItemType,
                RelatedItemId = notification.RelatedItemId,
                IsRead = notification.IsRead ?? false,
                ReadAt = notification.ReadAt,
                CreatedAt = notification.CreatedAt ?? DateTime.Now,
                Icon = GetNotificationIcon(notification.Type),
                IconColor = GetNotificationIconColor(notification.Type),
                Url = GetNotificationUrl(notification.RelatedItemType, notification.RelatedItemId)
            };
        }

        private string GetNotificationIcon(string type)
        {
            return type switch
            {
                "forum_reply" => "bi-chat-dots-fill",
                "post_like" => "bi-heart-fill",
                "reply_like" => "bi-heart-fill",
                "mention" => "bi-at",
                "system_alert" => "bi-exclamation-circle-fill",
                "course_enrollment" => "bi-book-fill",
                "new_lesson" => "bi-play-circle-fill",
                "course_completion" => "bi-trophy-fill",
                "new_material" => "bi-file-earmark-text-fill",
                _ => "bi-bell-fill"
            };
        }

        private string GetNotificationIconColor(string type)
        {
            return type switch
            {
                "forum_reply" => "text-primary",
                "post_like" => "text-danger",
                "reply_like" => "text-danger",
                "mention" => "text-info",
                "system_alert" => "text-warning",
                "course_enrollment" => "text-success",
                "new_lesson" => "text-primary",
                "course_completion" => "text-warning",
                "new_material" => "text-info",
                _ => "text-secondary"
            };
        }

        private string? GetNotificationUrl(string? itemType, int? itemId)
        {
            if (string.IsNullOrEmpty(itemType) || !itemId.HasValue)
                return null;

            return itemType switch
            {
                "post" => $"/Forum/Post/{itemId}",
                // For replies, we need to get the post ID and link to the post page
                // We cannot use anchor links because we don't know the comment's position
                "reply" => GetReplyPostUrl(itemId.Value),
                "course" => $"/Course/Details/{itemId}",
                "lesson" => $"/Lesson/View/{itemId}",
                _ => null
            };
        }

        private string? GetReplyPostUrl(int replyId)
        {
            try
            {
                // Get the post ID for this reply
                var reply = _context.ForumReplies
                    .Where(r => r.ReplyId == replyId)
                    .Select(r => new { r.PostId })
                    .FirstOrDefault();

                return reply != null ? $"/Forum/Post/{reply.PostId}" : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post URL for reply {ReplyId}", replyId);
                return null;
            }
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }

        #endregion
    }
}

