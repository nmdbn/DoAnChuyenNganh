namespace DoAnChuyenNganh.ViewModels.News
{
    public class NewsArticleAdminViewModel
    {
        public int ArticleId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

