namespace DoAnChuyenNganh.ViewModels.Forum
{
    public class ForumCategoryDetailViewModel
    {
        public ForumCategoryViewModel Category { get; set; } = null!;
        public List<ForumPostViewModel> Posts { get; set; } = new List<ForumPostViewModel>();
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
        public string SortBy { get; set; } = "latest"; // latest, popular, trending, mostReplied, mostLiked
        public string FilterBy { get; set; } = "all"; // all, sticky, locked, unanswered
    }
}

