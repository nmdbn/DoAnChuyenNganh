namespace DoAnChuyenNganh.ViewModels.Notification
{
    public class NotificationListViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
        public int PageSize { get; set; } = 20;
        public string? FilterType { get; set; }
        public bool UnreadOnly { get; set; } = false;
        
        // Available notification types for filtering
        public List<NotificationTypeFilter> AvailableTypes { get; set; } = new()
        {
            new NotificationTypeFilter { Value = "all", Label = "All Notifications", Icon = "bi-bell" },
            new NotificationTypeFilter { Value = "forum_reply", Label = "Forum Replies", Icon = "bi-chat-dots" },
            new NotificationTypeFilter { Value = "post_like", Label = "Post Likes", Icon = "bi-heart" },
            new NotificationTypeFilter { Value = "reply_like", Label = "Reply Likes", Icon = "bi-heart" },
            new NotificationTypeFilter { Value = "mention", Label = "Mentions", Icon = "bi-at" },
            new NotificationTypeFilter { Value = "system_alert", Label = "System Alerts", Icon = "bi-exclamation-circle" }
        };
        
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
    
    public class NotificationTypeFilter
    {
        public string Value { get; set; } = null!;
        public string Label { get; set; } = null!;
        public string Icon { get; set; } = null!;
    }
}

