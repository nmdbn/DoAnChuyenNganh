namespace DoAnChuyenNganh.ViewModels.Forum
{
    public class UserForumActivityViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime? MemberSince { get; set; }
        public DateTime? LastActive { get; set; }

        // Statistics
        public int TotalPosts { get; set; }
        public int TotalReplies { get; set; }
        public int TotalLikesReceived { get; set; }
        public int TotalLikesGiven { get; set; }

        // Recent activity
        public List<ForumPostViewModel> RecentPosts { get; set; } = new List<ForumPostViewModel>();
        public List<ForumReplyViewModel> RecentReplies { get; set; } = new List<ForumReplyViewModel>();
        public List<ForumPostViewModel> BookmarkedPosts { get; set; } = new List<ForumPostViewModel>();

        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
        public string ActiveTab { get; set; } = "posts"; // posts, replies, bookmarks
    }
}

