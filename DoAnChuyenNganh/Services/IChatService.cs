using DoAnChuyenNganh.ViewModels.Chat;

namespace DoAnChuyenNganh.Services
{
    public interface IChatService
    {
        #region Message Operations

        /// <summary>
        /// Sends a new chat message
        /// </summary>
        Task<(bool Success, int? MessageId, string Message)> SendMessageAsync(
            int senderId,
            int recipientId,
            string messageContent,
            List<IFormFile>? uploadedImages = null,
            List<IFormFile>? uploadedFiles = null);

        /// <summary>
        /// Gets messages for a conversation between two users
        /// </summary>
        Task<List<ChatMessageViewModel>> GetConversationMessagesAsync(
            int userId,
            int otherUserId,
            int page = 1,
            int pageSize = 50);

        /// <summary>
        /// Gets a single message by ID
        /// </summary>
        Task<ChatMessageViewModel?> GetMessageByIdAsync(int messageId, int userId);

        /// <summary>
        /// Marks messages as read
        /// </summary>
        Task<(bool Success, int UpdatedCount, string Message)> MarkMessagesAsReadAsync(
            int recipientId,
            int senderId);

        /// <summary>
        /// Deletes a message (soft delete)
        /// </summary>
        Task<(bool Success, string Message)> DeleteMessageAsync(int messageId, int userId);

        /// <summary>
        /// Gets the total count of messages in a conversation
        /// </summary>
        Task<int> GetConversationMessageCountAsync(int userId, int otherUserId);

        #endregion

        #region Conversation Operations

        /// <summary>
        /// Gets all conversations for a user
        /// </summary>
        Task<ChatListViewModel> GetUserConversationsAsync(
            int userId,
            int page = 1,
            int pageSize = 20);

        /// <summary>
        /// Gets a conversation detail with messages
        /// </summary>
        Task<ConversationDetailViewModel?> GetConversationDetailAsync(
            int userId,
            int otherUserId,
            int page = 1,
            int pageSize = 50);

        /// <summary>
        /// Searches for users to start a conversation with
        /// </summary>
        Task<List<ConversationViewModel>> SearchUsersForChatAsync(
            int currentUserId,
            string searchTerm,
            int maxResults = 10);

        /// <summary>
        /// Gets the total unread message count for a user
        /// </summary>
        Task<int> GetUnreadMessageCountAsync(int userId);

        /// <summary>
        /// Gets unread message count from a specific user
        /// </summary>
        Task<int> GetUnreadMessageCountFromUserAsync(int userId, int fromUserId);

        #endregion

        #region Attachment Operations

        /// <summary>
        /// Gets attachments for a message
        /// </summary>
        Task<List<ChatAttachmentViewModel>> GetMessageAttachmentsAsync(int messageId);

        /// <summary>
        /// Uploads an attachment for a message
        /// </summary>
        Task<(bool Success, ChatAttachmentViewModel? Attachment, string Message)> UploadAttachmentAsync(
            int messageId,
            IFormFile file,
            int uploadedBy,
            string fileType);

        /// <summary>
        /// Deletes an attachment
        /// </summary>
        Task<(bool Success, string Message)> DeleteAttachmentAsync(int attachmentId, int userId);

        #endregion

        #region Online Users Tracking

        /// <summary>
        /// Marks a user as online
        /// </summary>
        Task UserConnectedAsync(int userId, string connectionId);

        /// <summary>
        /// Marks a user as offline
        /// </summary>
        Task UserDisconnectedAsync(int userId, string connectionId);

        /// <summary>
        /// Gets all currently online users
        /// </summary>
        Task<List<OnlineUserViewModel>> GetOnlineUsersAsync();

        /// <summary>
        /// Gets the count of online users
        /// </summary>
        Task<int> GetOnlineUsersCountAsync();

        /// <summary>
        /// Checks if a specific user is online
        /// </summary>
        Task<bool> IsUserOnlineAsync(int userId);

        /// <summary>
        /// Gets connection IDs for a user
        /// </summary>
        Task<List<string>> GetUserConnectionIdsAsync(int userId);

        #endregion

        #region Helper Methods

        /// <summary>
        /// Maps a PrivateMessage entity to ChatMessageViewModel
        /// </summary>
        ChatMessageViewModel MapToViewModel(DoAnChuyenNganh.Models.PrivateMessage message);

        /// <summary>
        /// Validates if a user can access a conversation
        /// </summary>
        Task<bool> CanAccessConversationAsync(int userId, int otherUserId);

        #endregion
    }
}

