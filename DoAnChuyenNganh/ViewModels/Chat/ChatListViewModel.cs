namespace DoAnChuyenNganh.ViewModels.Chat
{
    /// <summary>
    /// ViewModel for the chat list page
    /// </summary>
    public class ChatListViewModel
    {
        public List<ConversationViewModel> Conversations { get; set; } = new List<ConversationViewModel>();
        
        public int CurrentPage { get; set; }
        
        public int TotalPages { get; set; }
        
        public int TotalConversations { get; set; }
        
        public int UnreadMessagesCount { get; set; }
        
        public bool HasPreviousPage => CurrentPage > 1;
        
        public bool HasNextPage => CurrentPage < TotalPages;
    }
    
    /// <summary>
    /// ViewModel for a conversation detail page
    /// </summary>
    public class ConversationDetailViewModel
    {
        public int OtherUserId { get; set; }
        
        public string OtherUsername { get; set; } = null!;
        
        public string OtherFirstName { get; set; } = null!;
        
        public string OtherLastName { get; set; } = null!;
        
        public string? OtherAvatarUrl { get; set; }
        
        public bool IsOtherUserOnline { get; set; }
        
        public List<ChatMessageViewModel> Messages { get; set; } = new List<ChatMessageViewModel>();
        
        public int CurrentPage { get; set; }
        
        public int TotalPages { get; set; }
        
        public bool HasMoreMessages => CurrentPage < TotalPages;
        
        // Helper properties
        public string OtherFullName => $"{OtherFirstName} {OtherLastName}";
        
        public string OtherInitials
        {
            get
            {
                if (!string.IsNullOrEmpty(OtherFirstName) && !string.IsNullOrEmpty(OtherLastName))
                {
                    return $"{OtherFirstName[0]}{OtherLastName[0]}".ToUpper();
                }
                return OtherUsername.Substring(0, Math.Min(2, OtherUsername.Length)).ToUpper();
            }
        }
    }
    
    /// <summary>
    /// ViewModel for typing indicator
    /// </summary>
    public class TypingIndicatorViewModel
    {
        public int UserId { get; set; }
        
        public string Username { get; set; } = null!;
        
        public int ConversationWithUserId { get; set; }
        
        public bool IsTyping { get; set; }
    }
}

