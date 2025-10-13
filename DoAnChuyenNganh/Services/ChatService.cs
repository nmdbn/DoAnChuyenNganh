using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.Chat;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace DoAnChuyenNganh.Services
{
    public class ChatService : IChatService
    {
        private readonly DoAnChuyenNganhContext _context;
        private readonly ILogger<ChatService> _logger;
        private readonly IFileUploadService _fileUploadService;
        private readonly INotificationService _notificationService;

        // In-memory storage for online users (connectionId -> userId mapping)
        private static readonly ConcurrentDictionary<string, int> _connections = new();
        // userId -> List of connectionIds (for multiple tabs/devices)
        private static readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();

        public ChatService(
            DoAnChuyenNganhContext context,
            ILogger<ChatService> logger,
            IFileUploadService fileUploadService,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _fileUploadService = fileUploadService;
            _notificationService = notificationService;
        }

        #region Message Operations

        public async Task<(bool Success, int? MessageId, string Message)> SendMessageAsync(
            int senderId,
            int recipientId,
            string messageContent,
            List<IFormFile>? uploadedImages = null,
            List<IFormFile>? uploadedFiles = null)
        {
            try
            {
                // Validate users exist
                var sender = await _context.Users.FindAsync(senderId);
                var recipient = await _context.Users.FindAsync(recipientId);

                if (sender == null || recipient == null)
                {
                    return (false, null, "Invalid sender or recipient");
                }

                // Create message
                var message = new PrivateMessage
                {
                    SenderId = senderId,
                    RecipientId = recipientId,
                    MessageContent = messageContent,
                    IsRead = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };

                _context.PrivateMessages.Add(message);
                await _context.SaveChangesAsync();

                // Handle image uploads
                if (uploadedImages != null && uploadedImages.Any())
                {
                    foreach (var image in uploadedImages)
                    {
                        var uploadResult = await _fileUploadService.UploadImageAsync(
                            image, 
                            "uploads/chat/images");

                        if (uploadResult.Success && uploadResult.FilePath != null)
                        {
                            var attachment = new ChatAttachment
                            {
                                MessageId = message.MessageId,
                                FileName = Path.GetFileName(uploadResult.FilePath),
                                OriginalFileName = image.FileName,
                                FilePath = uploadResult.FilePath,
                                FileType = "image",
                                MimeType = image.ContentType,
                                FileSize = image.Length,
                                UploadedBy = senderId,
                                CreatedAt = DateTime.Now
                            };
                            _context.ChatAttachments.Add(attachment);
                        }
                    }
                }

                // Handle file uploads
                if (uploadedFiles != null && uploadedFiles.Any())
                {
                    foreach (var file in uploadedFiles)
                    {
                        var uploadResult = await _fileUploadService.UploadFileAsync(
                            file, 
                            "uploads/chat/files");

                        if (uploadResult.Success && uploadResult.FilePath != null)
                        {
                            var attachment = new ChatAttachment
                            {
                                MessageId = message.MessageId,
                                FileName = Path.GetFileName(uploadResult.FilePath),
                                OriginalFileName = file.FileName,
                                FilePath = uploadResult.FilePath,
                                FileType = "file",
                                MimeType = file.ContentType,
                                FileSize = file.Length,
                                UploadedBy = senderId,
                                CreatedAt = DateTime.Now
                            };
                            _context.ChatAttachments.Add(attachment);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Create notification for recipient
                await _notificationService.CreateNotificationAsync(
                    recipientId,
                    "chat_message",
                    $"New message from {sender.FirstName} {sender.LastName}",
                    messageContent.Length > 100 ? messageContent.Substring(0, 100) + "..." : messageContent,
                    "PrivateMessage",
                    message.MessageId);

                _logger.LogInformation($"Message sent from user {senderId} to user {recipientId}");
                return (true, message.MessageId, "Message sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return (false, null, "An error occurred while sending the message");
            }
        }

        public async Task<List<ChatMessageViewModel>> GetConversationMessagesAsync(
            int userId,
            int otherUserId,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                var messages = await _context.PrivateMessages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.ChatAttachments)
                    .Where(m => ((m.SenderId == userId && m.RecipientId == otherUserId) ||
                                (m.SenderId == otherUserId && m.RecipientId == userId)) &&
                               m.IsDeleted == false)
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return messages.Select(m => MapToViewModel(m)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation messages");
                return new List<ChatMessageViewModel>();
            }
        }

        public async Task<ChatMessageViewModel?> GetMessageByIdAsync(int messageId, int userId)
        {
            try
            {
                var message = await _context.PrivateMessages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.ChatAttachments)
                    .FirstOrDefaultAsync(m => m.MessageId == messageId &&
                                            (m.SenderId == userId || m.RecipientId == userId) &&
                                            m.IsDeleted == false);

                return message != null ? MapToViewModel(message) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message by ID");
                return null;
            }
        }

        public async Task<(bool Success, int UpdatedCount, string Message)> MarkMessagesAsReadAsync(
            int recipientId,
            int senderId)
        {
            try
            {
                var unreadMessages = await _context.PrivateMessages
                    .Where(m => m.RecipientId == recipientId &&
                               m.SenderId == senderId &&
                               m.IsRead == false &&
                               m.IsDeleted == false)
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                    message.ReadAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Marked {unreadMessages.Count} messages as read for user {recipientId} from user {senderId}");
                return (true, unreadMessages.Count, "Messages marked as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read");
                return (false, 0, "An error occurred while marking messages as read");
            }
        }

        public async Task<(bool Success, string Message)> DeleteMessageAsync(int messageId, int userId)
        {
            try
            {
                var message = await _context.PrivateMessages
                    .FirstOrDefaultAsync(m => m.MessageId == messageId &&
                                            (m.SenderId == userId || m.RecipientId == userId));

                if (message == null)
                {
                    return (false, "Message not found");
                }

                message.IsDeleted = true;
                message.DeletedBy = userId;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Message {messageId} deleted by user {userId}");
                return (true, "Message deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                return (false, "An error occurred while deleting the message");
            }
        }

        public async Task<int> GetConversationMessageCountAsync(int userId, int otherUserId)
        {
            try
            {
                return await _context.PrivateMessages
                    .CountAsync(m => ((m.SenderId == userId && m.RecipientId == otherUserId) ||
                                     (m.SenderId == otherUserId && m.RecipientId == userId)) &&
                                    m.IsDeleted == false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation message count");
                return 0;
            }
        }

        #endregion

        #region Conversation Operations

        public async Task<ChatListViewModel> GetUserConversationsAsync(
            int userId,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                // Get all messages involving the user
                var userMessages = await _context.PrivateMessages
                    .Where(m => (m.SenderId == userId || m.RecipientId == userId) && m.IsDeleted == false)
                    .ToListAsync();

                // Group by conversation partner
                var conversations = userMessages
                    .GroupBy(m => m.SenderId == userId ? m.RecipientId : m.SenderId)
                    .Select(g => new
                    {
                        OtherUserId = g.Key,
                        LastMessage = g.OrderByDescending(m => m.CreatedAt).First()
                    })
                    .OrderByDescending(c => c.LastMessage.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var conversationViewModels = new List<ConversationViewModel>();

                foreach (var conv in conversations)
                {
                    var otherUser = await _context.Users.FindAsync(conv.OtherUserId);
                    if (otherUser == null) continue;

                    var unreadCount = await GetUnreadMessageCountFromUserAsync(userId, conv.OtherUserId);

                    conversationViewModels.Add(new ConversationViewModel
                    {
                        OtherUserId = conv.OtherUserId,
                        Username = otherUser.Username,
                        FirstName = otherUser.FirstName,
                        LastName = otherUser.LastName,
                        AvatarUrl = otherUser.AvatarUrl,
                        LastMessage = conv.LastMessage.MessageContent,
                        LastMessageSenderId = conv.LastMessage.SenderId,
                        LastMessageIsRead = conv.LastMessage.IsRead ?? false,
                        LastMessageAt = conv.LastMessage.CreatedAt ?? DateTime.Now,
                        UnreadCount = unreadCount,
                        IsOnline = await IsUserOnlineAsync(conv.OtherUserId)
                    });
                }

                var totalConversations = userMessages
                    .GroupBy(m => m.SenderId == userId ? m.RecipientId : m.SenderId)
                    .Count();

                return new ChatListViewModel
                {
                    Conversations = conversationViewModels,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(totalConversations / (double)pageSize),
                    TotalConversations = totalConversations,
                    UnreadMessagesCount = await GetUnreadMessageCountAsync(userId)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user conversations");
                return new ChatListViewModel();
            }
        }

        public async Task<ConversationDetailViewModel?> GetConversationDetailAsync(
            int userId,
            int otherUserId,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                _logger.LogInformation("Getting conversation detail for user {UserId} with user {OtherUserId}", userId, otherUserId);

                var otherUser = await _context.Users.FindAsync(otherUserId);
                if (otherUser == null)
                {
                    _logger.LogWarning("User {OtherUserId} not found in database", otherUserId);
                    return null;
                }

                if (!(otherUser.IsActive ?? false))
                {
                    _logger.LogWarning("User {OtherUserId} is not active", otherUserId);
                    return null;
                }

                var messages = await GetConversationMessagesAsync(userId, otherUserId, page, pageSize);
                var totalMessages = await GetConversationMessageCountAsync(userId, otherUserId);

                _logger.LogInformation("Found {MessageCount} messages in conversation", messages.Count);

                return new ConversationDetailViewModel
                {
                    OtherUserId = otherUserId,
                    OtherUsername = otherUser.Username,
                    OtherFirstName = otherUser.FirstName,
                    OtherLastName = otherUser.LastName,
                    OtherAvatarUrl = otherUser.AvatarUrl,
                    IsOtherUserOnline = await IsUserOnlineAsync(otherUserId),
                    Messages = messages,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(totalMessages / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation detail for user {UserId} with user {OtherUserId}", userId, otherUserId);
                return null;
            }
        }

        public async Task<List<ConversationViewModel>> SearchUsersForChatAsync(
            int currentUserId,
            string searchTerm,
            int maxResults = 10)
        {
            try
            {
                _logger.LogInformation("Searching users for currentUserId={CurrentUserId}, searchTerm={SearchTerm}", currentUserId, searchTerm);

                var users = await _context.Users
                    .Where(u => u.UserId != currentUserId &&
                               u.IsActive == true &&
                               (u.Username.Contains(searchTerm) ||
                                u.FirstName.Contains(searchTerm) ||
                                u.LastName.Contains(searchTerm)))
                    .Take(maxResults)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} users matching search term", users.Count);

                var result = new List<ConversationViewModel>();

                foreach (var user in users)
                {
                    _logger.LogInformation("Processing user: UserId={UserId}, Username={Username}, FirstName={FirstName}, LastName={LastName}",
                        user.UserId, user.Username, user.FirstName, user.LastName);

                    // Get last message if exists
                    var lastMessage = await _context.PrivateMessages
                        .Where(m => ((m.SenderId == currentUserId && m.RecipientId == user.UserId) ||
                                    (m.SenderId == user.UserId && m.RecipientId == currentUserId)) &&
                                   m.IsDeleted == false)
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefaultAsync();

                    var viewModel = new ConversationViewModel
                    {
                        OtherUserId = user.UserId,
                        Username = user.Username,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        AvatarUrl = user.AvatarUrl,
                        LastMessage = lastMessage?.MessageContent ?? "",
                        LastMessageSenderId = lastMessage?.SenderId ?? 0,
                        LastMessageIsRead = lastMessage?.IsRead ?? true,
                        LastMessageAt = lastMessage?.CreatedAt ?? DateTime.Now,
                        UnreadCount = await GetUnreadMessageCountFromUserAsync(currentUserId, user.UserId),
                        IsOnline = await IsUserOnlineAsync(user.UserId)
                    };

                    _logger.LogInformation("Created ViewModel: OtherUserId={OtherUserId}, Username={Username}, FullName={FullName}",
                        viewModel.OtherUserId, viewModel.Username, viewModel.FullName);

                    result.Add(viewModel);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users for chat");
                return new List<ConversationViewModel>();
            }
        }

        public async Task<int> GetUnreadMessageCountAsync(int userId)
        {
            try
            {
                return await _context.PrivateMessages
                    .CountAsync(m => m.RecipientId == userId &&
                                    m.IsRead == false &&
                                    m.IsDeleted == false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread message count");
                return 0;
            }
        }

        public async Task<int> GetUnreadMessageCountFromUserAsync(int userId, int fromUserId)
        {
            try
            {
                return await _context.PrivateMessages
                    .CountAsync(m => m.RecipientId == userId &&
                                    m.SenderId == fromUserId &&
                                    m.IsRead == false &&
                                    m.IsDeleted == false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread message count from user");
                return 0;
            }
        }

        #endregion

        #region Attachment Operations

        public async Task<List<ChatAttachmentViewModel>> GetMessageAttachmentsAsync(int messageId)
        {
            try
            {
                var attachments = await _context.ChatAttachments
                    .Where(a => a.MessageId == messageId)
                    .ToListAsync();

                return attachments.Select(a => new ChatAttachmentViewModel
                {
                    AttachmentId = a.AttachmentId,
                    MessageId = a.MessageId,
                    FileName = a.FileName,
                    OriginalFileName = a.OriginalFileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    MimeType = a.MimeType,
                    FileSize = a.FileSize,
                    UploadedBy = a.UploadedBy,
                    CreatedAt = a.CreatedAt ?? DateTime.Now
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message attachments");
                return new List<ChatAttachmentViewModel>();
            }
        }

        public async Task<(bool Success, ChatAttachmentViewModel? Attachment, string Message)> UploadAttachmentAsync(
            int messageId,
            IFormFile file,
            int uploadedBy,
            string fileType)
        {
            try
            {
                var message = await _context.PrivateMessages.FindAsync(messageId);
                if (message == null)
                {
                    return (false, null, "Message not found");
                }

                var uploadResult = fileType == "image"
                    ? await _fileUploadService.UploadImageAsync(file, "uploads/chat/images")
                    : await _fileUploadService.UploadFileAsync(file, "uploads/chat/files");

                if (!uploadResult.Success || uploadResult.FilePath == null)
                {
                    return (false, null, uploadResult.Message);
                }

                var attachment = new ChatAttachment
                {
                    MessageId = messageId,
                    FileName = Path.GetFileName(uploadResult.FilePath),
                    OriginalFileName = file.FileName,
                    FilePath = uploadResult.FilePath,
                    FileType = fileType,
                    MimeType = file.ContentType,
                    FileSize = file.Length,
                    UploadedBy = uploadedBy,
                    CreatedAt = DateTime.Now
                };

                _context.ChatAttachments.Add(attachment);
                await _context.SaveChangesAsync();

                var viewModel = new ChatAttachmentViewModel
                {
                    AttachmentId = attachment.AttachmentId,
                    MessageId = attachment.MessageId,
                    FileName = attachment.FileName,
                    OriginalFileName = attachment.OriginalFileName,
                    FilePath = attachment.FilePath,
                    FileType = attachment.FileType,
                    MimeType = attachment.MimeType,
                    FileSize = attachment.FileSize,
                    UploadedBy = attachment.UploadedBy,
                    CreatedAt = attachment.CreatedAt ?? DateTime.Now
                };

                return (true, viewModel, "Attachment uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment");
                return (false, null, "An error occurred while uploading the attachment");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAttachmentAsync(int attachmentId, int userId)
        {
            try
            {
                var attachment = await _context.ChatAttachments
                    .Include(a => a.Message)
                    .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

                if (attachment == null)
                {
                    return (false, "Attachment not found");
                }

                // Check if user has permission to delete
                if (attachment.Message.SenderId != userId && attachment.Message.RecipientId != userId)
                {
                    return (false, "You don't have permission to delete this attachment");
                }

                // Delete file from file system
                await _fileUploadService.DeleteFileAsync(attachment.FilePath);

                // Delete from database
                _context.ChatAttachments.Remove(attachment);
                await _context.SaveChangesAsync();

                return (true, "Attachment deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment");
                return (false, "An error occurred while deleting the attachment");
            }
        }

        #endregion

        #region Online Users Tracking

        public Task UserConnectedAsync(int userId, string connectionId)
        {
            _connections[connectionId] = userId;

            if (!_userConnections.ContainsKey(userId))
            {
                _userConnections[userId] = new HashSet<string>();
            }

            _userConnections[userId].Add(connectionId);

            _logger.LogInformation($"User {userId} connected with connection {connectionId}");
            return Task.CompletedTask;
        }

        public Task UserDisconnectedAsync(int userId, string connectionId)
        {
            _connections.TryRemove(connectionId, out _);

            if (_userConnections.ContainsKey(userId))
            {
                _userConnections[userId].Remove(connectionId);

                if (_userConnections[userId].Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                }
            }

            _logger.LogInformation($"User {userId} disconnected from connection {connectionId}");
            return Task.CompletedTask;
        }

        public async Task<List<OnlineUserViewModel>> GetOnlineUsersAsync()
        {
            try
            {
                var onlineUserIds = _userConnections.Keys.ToList();

                var users = await _context.Users
                    .Where(u => onlineUserIds.Contains(u.UserId))
                    .ToListAsync();

                return users.Select(u => new OnlineUserViewModel
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AvatarUrl = u.AvatarUrl,
                    ConnectedAt = DateTime.Now,
                    LastActivityAt = DateTime.Now
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online users");
                return new List<OnlineUserViewModel>();
            }
        }

        public Task<int> GetOnlineUsersCountAsync()
        {
            return Task.FromResult(_userConnections.Count);
        }

        public Task<bool> IsUserOnlineAsync(int userId)
        {
            return Task.FromResult(_userConnections.ContainsKey(userId));
        }

        public Task<List<string>> GetUserConnectionIdsAsync(int userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                return Task.FromResult(connections.ToList());
            }

            return Task.FromResult(new List<string>());
        }

        #endregion

        #region Helper Methods

        public ChatMessageViewModel MapToViewModel(PrivateMessage message)
        {
            var viewModel = new ChatMessageViewModel
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                SenderUsername = message.Sender.Username,
                SenderFirstName = message.Sender.FirstName,
                SenderLastName = message.Sender.LastName,
                SenderAvatarUrl = message.Sender.AvatarUrl,
                RecipientId = message.RecipientId,
                RecipientUsername = message.Recipient.Username,
                RecipientFirstName = message.Recipient.FirstName,
                RecipientLastName = message.Recipient.LastName,
                RecipientAvatarUrl = message.Recipient.AvatarUrl,
                MessageContent = message.MessageContent,
                IsRead = message.IsRead ?? false,
                ReadAt = message.ReadAt,
                CreatedAt = message.CreatedAt ?? DateTime.Now
            };

            // Map attachments if loaded
            if (message.ChatAttachments != null && message.ChatAttachments.Any())
            {
                viewModel.Attachments = message.ChatAttachments.Select(a => new ChatAttachmentViewModel
                {
                    AttachmentId = a.AttachmentId,
                    MessageId = a.MessageId,
                    FileName = a.FileName,
                    OriginalFileName = a.OriginalFileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    MimeType = a.MimeType,
                    FileSize = a.FileSize,
                    UploadedBy = a.UploadedBy,
                    CreatedAt = a.CreatedAt ?? DateTime.Now
                }).ToList();
            }

            return viewModel;
        }

        public async Task<bool> CanAccessConversationAsync(int userId, int otherUserId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                var otherUser = await _context.Users.FindAsync(otherUserId);

                return user != null && otherUser != null && user.IsActive == true && otherUser.IsActive == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking conversation access");
                return false;
            }
        }

        #endregion
    }
}

