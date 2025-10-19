namespace DoAnChuyenNganh.ViewModels.Notification
{
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? RelatedItemType { get; set; }
        public int? RelatedItemId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // UI-specific properties
        public string Icon { get; set; } = "bi-bell-fill";
        public string IconColor { get; set; } = "text-secondary";
        public string? Url { get; set; }
        
        // Computed properties
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedAt;
                
                if (timeSpan.TotalMinutes < 1)
                    return "just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";
                if (timeSpan.TotalDays < 30)
                    return $"{(int)(timeSpan.TotalDays / 7)}w ago";
                
                return CreatedAt.ToString("MMM dd, yyyy");
            }
        }
        
        public string TypeDisplayName
        {
            get
            {
                return Type switch
                {
                    "forum_reply" => "Forum Reply",
                    "post_like" => "Post Like",
                    "reply_like" => "Reply Like",
                    "mention" => "Mention",
                    "system_alert" => "System Alert",
                    "course_enrollment" => "Course Enrollment",
                    "new_lesson" => "New Lesson",
                    "course_completion" => "Course Completion",
                    "new_material" => "New Material",
                    _ => "Notification"
                };
            }
        }
    }
}

