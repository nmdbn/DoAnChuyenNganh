namespace DoAnChuyenNganh.ViewModels.Chat
{
    /// <summary>
    /// ViewModel for displaying a conversation in the chat list
    /// </summary>
    public class ConversationViewModel
    {
        public int OtherUserId { get; set; }
        
        public string Username { get; set; } = null!;
        
        public string FirstName { get; set; } = null!;
        
        public string LastName { get; set; } = null!;
        
        public string? AvatarUrl { get; set; }
        
        public string LastMessage { get; set; } = null!;
        
        public int LastMessageSenderId { get; set; }
        
        public bool LastMessageIsRead { get; set; }
        
        public DateTime LastMessageAt { get; set; }
        
        public int UnreadCount { get; set; }
        
        public bool IsOnline { get; set; }
        
        // Helper properties
        public string FullName => $"{FirstName} {LastName}";
        
        public string Initials
        {
            get
            {
                if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                {
                    return $"{FirstName[0]}{LastName[0]}".ToUpper();
                }
                return Username.Substring(0, Math.Min(2, Username.Length)).ToUpper();
            }
        }
        
        public string LastMessagePreview
        {
            get
            {
                if (string.IsNullOrEmpty(LastMessage))
                    return "";
                
                // Remove HTML tags for preview
                var preview = System.Text.RegularExpressions.Regex.Replace(LastMessage, "<.*?>", string.Empty);
                
                if (preview.Length > 50)
                    return preview.Substring(0, 50) + "...";
                
                return preview;
            }
        }
        
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - LastMessageAt;
                
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";
                
                return LastMessageAt.ToString("MMM dd");
            }
        }
    }
}

