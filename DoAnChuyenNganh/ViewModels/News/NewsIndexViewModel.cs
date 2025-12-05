using DoAnChuyenNganh.Models;

namespace DoAnChuyenNganh.ViewModels.News
{
    public class NewsIndexViewModel
    {
        public List<NewsArticleListViewModel> Articles { get; set; } = new List<NewsArticleListViewModel>();
        public List<NewsArticleListViewModel> FeaturedArticles { get; set; } = new List<NewsArticleListViewModel>();
        public List<NewsArticleListViewModel> HotArticles { get; set; } = new List<NewsArticleListViewModel>();
        public List<NewsCategory> Categories { get; set; } = new List<NewsCategory>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalArticles { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string? SearchTerm { get; set; }
    }
}

