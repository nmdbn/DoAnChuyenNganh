namespace DoAnChuyenNganh.ViewModels.News
{
    public class NewsArticleListViewModel
    {
        public int ArticleId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Excerpt { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public string CategoryName { get; set; } = null!;
        public int CategoryId { get; set; }
        public string AuthorName { get; set; } = null!;
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; }
        public bool IsFeatured { get; set; }
    }
}

