using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.News;

namespace DoAnChuyenNganh.Services
{
    public interface INewsService
    {
        /// <summary>
        /// Get all published news articles with pagination
        /// </summary>
        Task<(List<NewsArticleListViewModel> Articles, int TotalCount)> GetPublishedArticlesAsync(
            int page = 1, 
            int pageSize = 12, 
            int? categoryId = null,
            string? searchTerm = null);

        /// <summary>
        /// Get featured news articles
        /// </summary>
        Task<List<NewsArticleListViewModel>> GetFeaturedArticlesAsync(int count = 5);

        /// <summary>
        /// Get hot/trending news articles based on view count
        /// </summary>
        Task<List<NewsArticleListViewModel>> GetHotArticlesAsync(int count = 10, int daysRange = 7);

        /// <summary>
        /// Get related articles based on category and tags
        /// </summary>
        Task<List<NewsArticleListViewModel>> GetRelatedArticlesAsync(int articleId, int count = 5);

        /// <summary>
        /// Get article by ID for viewing
        /// </summary>
        Task<NewsArticleDetailViewModel?> GetArticleByIdAsync(int articleId);

        /// <summary>
        /// Get article entity by ID (for permission checks)
        /// </summary>
        Task<NewsArticle?> GetArticleEntityByIdAsync(int articleId);

        /// <summary>
        /// Get article by slug for viewing
        /// </summary>
        Task<NewsArticleDetailViewModel?> GetArticleBySlugAsync(string slug);

        /// <summary>
        /// Increment view count for an article
        /// </summary>
        Task IncrementViewCountAsync(int articleId);

        /// <summary>
        /// Get all articles for admin management
        /// </summary>
        Task<(List<NewsArticleAdminViewModel> Articles, int TotalCount)> GetAllArticlesForAdminAsync(
            int page = 1,
            int pageSize = 20,
            bool? isPublished = null,
            int? categoryId = null,
            int? authorId = null);

        /// <summary>
        /// Get article for editing
        /// </summary>
        Task<NewsArticleEditViewModel?> GetArticleForEditAsync(int articleId);

        /// <summary>
        /// Create a new news article
        /// </summary>
        Task<(bool Success, int? ArticleId, string Message)> CreateArticleAsync(
            NewsArticleCreateViewModel model, 
            int authorId);

        /// <summary>
        /// Update an existing news article
        /// </summary>
        Task<(bool Success, string Message)> UpdateArticleAsync(
            int articleId, 
            NewsArticleEditViewModel model, 
            int updatedBy);

        /// <summary>
        /// Delete a news article
        /// </summary>
        Task<(bool Success, string Message)> DeleteArticleAsync(int articleId);

        /// <summary>
        /// Publish or unpublish an article
        /// </summary>
        Task<(bool Success, string Message)> TogglePublishStatusAsync(int articleId, int updatedBy);

        /// <summary>
        /// Get all news categories
        /// </summary>
        Task<List<NewsCategory>> GetAllCategoriesAsync();

        /// <summary>
        /// Get active news categories
        /// </summary>
        Task<List<NewsCategory>> GetActiveCategoriesAsync();

        /// <summary>
        /// Generate a URL-friendly slug from title
        /// </summary>
        string GenerateSlug(string title);

        /// <summary>
        /// Check if slug is unique
        /// </summary>
        Task<bool> IsSlugUniqueAsync(string slug, int? excludeArticleId = null);
    }
}

