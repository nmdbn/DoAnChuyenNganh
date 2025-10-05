namespace DoAnChuyenNganh.ViewModels.Forum
{
    public class ForumPostDetailViewModel
    {
        public ForumPostViewModel Post { get; set; } = null!;
        public List<ForumReplyViewModel> Replies { get; set; } = new List<ForumReplyViewModel>();
        public ForumReplyViewModel NewReply { get; set; } = new ForumReplyViewModel();
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
        
        // Breadcrumb navigation
        public string CategoryName { get; set; } = null!;
        public short CategoryId { get; set; }
        
        // Related posts
        public List<ForumPostViewModel> RelatedPosts { get; set; } = new List<ForumPostViewModel>();
    }
}

