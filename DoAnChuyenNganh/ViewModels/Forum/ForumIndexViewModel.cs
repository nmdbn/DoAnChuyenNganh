namespace DoAnChuyenNganh.ViewModels.Forum
{
    public class ForumIndexViewModel
    {
        public List<ForumCategoryViewModel> Categories { get; set; } = new List<ForumCategoryViewModel>();
        public ForumStatisticsViewModel Statistics { get; set; } = new ForumStatisticsViewModel();
        public List<ForumPostViewModel> RecentPosts { get; set; } = new List<ForumPostViewModel>();
        public List<ForumPostViewModel> PopularPosts { get; set; } = new List<ForumPostViewModel>();
    }

    public class ForumStatisticsViewModel
    {
        public int TotalPosts { get; set; }
        public int TotalReplies { get; set; }
        public int TotalUsers { get; set; }
        public int TotalCategories { get; set; }
        public string? NewestMember { get; set; }
        public DateTime? NewestMemberJoinDate { get; set; }
    }
}

