using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.ViewModels.Chatbot
{
    /// <summary>
    /// Main ViewModel for the AI Chatbot page
    /// </summary>
    public class AIChatbotViewModel
    {
        public int CurrentUserId { get; set; }
        public string? UserAvatarUrl { get; set; }
        public List<ConversationSummaryViewModel> Conversations { get; set; } = new List<ConversationSummaryViewModel>();
        public string? ActiveConversationId { get; set; }
        public ConversationDetailViewModel? ActiveConversation { get; set; }
    }

    /// <summary>
    /// ViewModel for conversation summary in sidebar
    /// </summary>
    public class ConversationSummaryViewModel
    {
        public string ConversationId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - UpdatedAt;

                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";

                return UpdatedAt.ToString("MMM dd");
            }
        }

        public string TitlePreview
        {
            get
            {
                if (string.IsNullOrEmpty(Title))
                    return "New Conversation";

                if (Title.Length > 30)
                    return Title.Substring(0, 30) + "...";

                return Title;
            }
        }
    }

    /// <summary>
    /// ViewModel for conversation detail with messages
    /// </summary>
    public class ConversationDetailViewModel
    {
        public string ConversationId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }
        public List<ChatMessageViewModel> Messages { get; set; } = new List<ChatMessageViewModel>();
    }

    /// <summary>
    /// ViewModel for a single chat message
    /// </summary>
    public class ChatMessageViewModel
    {
        public string MessageId { get; set; } = null!;
        public string ConversationId { get; set; } = null!;
        public string Role { get; set; } = null!; // "user" or "assistant"
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public SqlMetadataViewModel? SqlMetadata { get; set; }

        public bool IsUser => Role.Equals("user", StringComparison.OrdinalIgnoreCase);
        public bool IsAssistant => Role.Equals("assistant", StringComparison.OrdinalIgnoreCase);

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

                return CreatedAt.ToString("MMM dd, yyyy HH:mm");
            }
        }

        public string FormattedTime => CreatedAt.ToString("HH:mm");
    }

    /// <summary>
    /// ViewModel for SQL metadata in assistant messages
    /// </summary>
    public class SqlMetadataViewModel
    {
        public string? Question { get; set; }
        public string? SqlQuery { get; set; }
        public List<string>? RelevantTables { get; set; }
        public bool IsValid { get; set; }
        public double? ConfidenceScore { get; set; }
    }

    /// <summary>
    /// ViewModel for sending a new message
    /// </summary>
    public class SendMessageViewModel
    {
        public string? ConversationId { get; set; }

        [Required(ErrorMessage = "Message is required")]
        [StringLength(5000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 5000 characters")]
        public string Message { get; set; } = null!;

        public bool GenerateSql { get; set; } = false;
    }

    /// <summary>
    /// ViewModel for creating a new conversation
    /// </summary>
    public class CreateConversationViewModel
    {
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }
    }
}

