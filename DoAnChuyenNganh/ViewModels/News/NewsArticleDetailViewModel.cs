namespace DoAnChuyenNganh.ViewModels.News
{
    public class NewsArticleDetailViewModel
    {
        public int ArticleId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Excerpt { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public string CategoryName { get; set; } = null!;
        public int CategoryId { get; set; }
        public string AuthorName { get; set; } = null!;
        public string? AuthorAvatarUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ViewCount { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPublished { get; set; }
        public string? Tags { get; set; }
        public List<NewsArticleListViewModel>? RelatedArticles { get; set; }
    }
}

