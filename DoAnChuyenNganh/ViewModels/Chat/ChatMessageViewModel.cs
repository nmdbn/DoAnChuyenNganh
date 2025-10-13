using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.ViewModels.Chat
{
    /// <summary>
    /// ViewModel for displaying a chat message
    /// </summary>
    public class ChatMessageViewModel
    {
        public int MessageId { get; set; }
        
        public int SenderId { get; set; }
        
        public string SenderUsername { get; set; } = null!;
        
        public string SenderFirstName { get; set; } = null!;
        
        public string SenderLastName { get; set; } = null!;
        
        public string? SenderAvatarUrl { get; set; }
        
        public int RecipientId { get; set; }
        
        public string RecipientUsername { get; set; } = null!;
        
        public string RecipientFirstName { get; set; } = null!;
        
        public string RecipientLastName { get; set; } = null!;
        
        public string? RecipientAvatarUrl { get; set; }
        
        public string MessageContent { get; set; } = null!;
        
        public bool IsRead { get; set; }
        
        public DateTime? ReadAt { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public List<ChatAttachmentViewModel> Attachments { get; set; } = new List<ChatAttachmentViewModel>();
        
        // Helper properties
        public string SenderFullName => $"{SenderFirstName} {SenderLastName}";
        
        public string RecipientFullName => $"{RecipientFirstName} {RecipientLastName}";
        
        public string SenderInitials
        {
            get
            {
                if (!string.IsNullOrEmpty(SenderFirstName) && !string.IsNullOrEmpty(SenderLastName))
                {
                    return $"{SenderFirstName[0]}{SenderLastName[0]}".ToUpper();
                }
                return SenderUsername.Substring(0, Math.Min(2, SenderUsername.Length)).ToUpper();
            }
        }
        
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedAt;
                
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";
                
                return CreatedAt.ToString("MMM dd, yyyy");
            }
        }
    }
    
    /// <summary>
    /// ViewModel for sending a new chat message
    /// </summary>
    public class SendMessageViewModel
    {
        [Required(ErrorMessage = "Recipient is required")]
        public int RecipientId { get; set; }
        
        [Required(ErrorMessage = "Message content is required")]
        [StringLength(5000, ErrorMessage = "Message cannot exceed 5000 characters")]
        public string MessageContent { get; set; } = null!;
        
        public List<IFormFile>? UploadedImages { get; set; }
        
        public List<IFormFile>? UploadedFiles { get; set; }
    }
}

