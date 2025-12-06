using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.News;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DoAnChuyenNganh.Services
{
    public class NewsService : INewsService
    {
        private readonly DoAnChuyenNganhContext _context;
        private readonly ILogger<NewsService> _logger;

        public NewsService(DoAnChuyenNganhContext context, ILogger<NewsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(List<NewsArticleListViewModel> Articles, int TotalCount)> GetPublishedArticlesAsync(
            int page = 1, 
            int pageSize = 12, 
            int? categoryId = null,
            string? searchTerm = null)
        {
            try
            {
                var query = _context.NewsArticles
                    .Include(a => a.Author)
                    .Include(a => a.Category)
                    .Where(a => a.IsPublished == true);

                // Filter by category
                if (categoryId.HasValue)
                {
                    query = query.Where(a => a.CategoryId == categoryId.Value);
                }

                // Filter by search term
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(a => 
                        a.Title.Contains(searchTerm) || 
                        a.Excerpt!.Contains(searchTerm) ||
                        a.Tags!.Contains(searchTerm));
                }

                var totalCount = await query.CountAsync();

                var articles = await query
                    .OrderByDescending(a => a.PublishedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new NewsArticleListViewModel
                    {
                        ArticleId = a.ArticleId,
                        Title = a.Title,
                        Slug = a.Slug,
                        Excerpt = a.Excerpt,
                        FeaturedImageUrl = a.FeaturedImageUrl,
                        CategoryName = a.Category.CategoryName,
                        CategoryId = a.CategoryId,
                        AuthorName = a.Author.FirstName + " " + a.Author.LastName,
                        PublishedAt = a.PublishedAt,
                        ViewCount = a.ViewCount ?? 0,
                        IsFeatured = a.IsFeatured ?? false
                    })
                    .ToListAsync();

                return (articles, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting published articles");
                return (new List<NewsArticleListViewModel>(), 0);
            }
        }

        public async Task<List<NewsArticleListViewModel>> GetFeaturedArticlesAsync(int count = 5)
        {
            try
            {
                return await _context.NewsArticles
                    .Include(a => a.Author)
                    .Include(a => a.Category)
                    .Where(a => a.IsPublished == true && a.IsFeatured == true)
                    .OrderByDescending(a => a.PublishedAt)
                    .Take(count)
                    .Select(a => new NewsArticleListViewModel
                    {
                        ArticleId = a.ArticleId,
                        Title = a.Title,
                        Slug = a.Slug,
                        Excerpt = a.Excerpt,
                        FeaturedImageUrl = a.FeaturedImageUrl,
                        CategoryName = a.Category.CategoryName,
                        CategoryId = a.CategoryId,
                        AuthorName = a.Author.FirstName + " " + a.Author.LastName,
                        PublishedAt = a.PublishedAt,
                        ViewCount = a.ViewCount ?? 0,
                        IsFeatured = true
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured articles");
                return new List<NewsArticleListViewModel>();
            }
        }

        public async Task<List<NewsArticleListViewModel>> GetHotArticlesAsync(int count = 10, int daysRange = 7)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysRange);

                return await _context.NewsArticles
                    .Include(a => a.Author)
                    .Include(a => a.Category)
                    .Where(a => a.IsPublished == true && a.PublishedAt >= cutoffDate)
                    .OrderByDescending(a => a.ViewCount)
                    .ThenByDescending(a => a.PublishedAt)
                    .Take(count)
                    .Select(a => new NewsArticleListViewModel
                    {
                        ArticleId = a.ArticleId,
                        Title = a.Title,
                        Slug = a.Slug,
                        Excerpt = a.Excerpt,
                        FeaturedImageUrl = a.FeaturedImageUrl,
                        CategoryName = a.Category.CategoryName,
                        CategoryId = a.CategoryId,
                        AuthorName = a.Author.FirstName + " " + a.Author.LastName,
                        PublishedAt = a.PublishedAt,
                        ViewCount = a.ViewCount ?? 0,
                        IsFeatured = a.IsFeatured ?? false
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hot articles");
                return new List<NewsArticleListViewModel>();
            }
        }

        public async Task<List<NewsArticleListViewModel>> GetRelatedArticlesAsync(int articleId, int count = 5)
        {
            try
            {
                var article = await _context.NewsArticles
                    .Where(a => a.ArticleId == articleId)
                    .Select(a => new { a.CategoryId, a.Tags })
                    .FirstOrDefaultAsync();

                if (article == null)
                    return new List<NewsArticleListViewModel>();

                var query = _context.NewsArticles
                    .Include(a => a.Author)
                    .Include(a => a.Category)
                    .Where(a => a.IsPublished == true && a.ArticleId != articleId);

                // Prioritize same category
                query = query.Where(a => a.CategoryId == article.CategoryId);

                return await query
                    .OrderByDescending(a => a.PublishedAt)
                    .Take(count)
                    .Select(a => new NewsArticleListViewModel
                    {
                        ArticleId = a.ArticleId,
                        Title = a.Title,
                        Slug = a.Slug,
                        Excerpt = a.Excerpt,
                        FeaturedImageUrl = a.FeaturedImageUrl,
                        CategoryName = a.Category.CategoryName,
                        CategoryId = a.CategoryId,
                        AuthorName = a.Author.FirstName + " " + a.Author.LastName,
                        PublishedAt = a.PublishedAt,
                        ViewCount = a.ViewCount ?? 0,
                        IsFeatured = a.IsFeatured ?? false
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting related articles");
                return new List<NewsArticleListViewModel>();
            }
        }

        public async Task<NewsArticleDetailViewModel?> GetArticleByIdAsync(int articleId)
        {
            try
            {
                return await _context.NewsArticles
                    .Include(a => a.Author)
                    .Include(a => a.Category)
                    .Include(a => a.UpdatedByNavigation)
                    .Where(a => a.ArticleId == articleId)
                    .Select(a => new NewsArticleDetailViewModel
                    {
                        ArticleId = a.ArticleId,
                        Title = a.Title,
                        Slug = a.Slug,
                        Content = a.Content,
                        Excerpt = a.Excerpt,
                        FeaturedImageUrl = a.FeaturedImageUrl,
                        CategoryName = a.Category.CategoryName,
                        CategoryId = a.CategoryId,
                        AuthorName = a.Author.FirstName + " " + a.Author.LastName,
                        AuthorAvatarUrl = a.Author.AvatarUrl,
                        PublishedAt = a.PublishedAt,
                        UpdatedAt = a.UpdatedAt,
                        ViewCount = a.ViewCount ?? 0,
                        IsFeatured = a.IsFeatured ?? false,
                        IsPublished = a.IsPublished ?? false,
                        Tags = a.Tags
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting article by ID: {articleId}");
                return null;
            }
        }

        public async Task<NewsArticle?> GetArticleEntityByIdAsync(int articleId)
        {
            try
            {
                return await _context.NewsArticles
                    .FirstOrDefaultAsync(a => a.ArticleId == articleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting article entity by ID: {articleId}");
                return null;
            }
        }

        public async Task<NewsArticleDetailViewModel?> GetArticleBySlugAsync(string slug)
        {
            try
            {
                return await _context.NewsArticles
                    .Include(a => a.Author)
                    .Include(a => a.Category)
                    .Include(a => a.UpdatedByNavigation)
                    .Where(a => a.Slug == slug)
                    .Select(a => new NewsArticleDetailViewModel
                    {
                        ArticleId = a.ArticleId,
                        Title = a.Title,
                        Slug = a.Slug,
                        Content = a.Content,
                        Excerpt = a.Excerpt,
                        FeaturedImageUrl = a.FeaturedImageUrl,
                        CategoryName = a.Category.CategoryName,
                        CategoryId = a.CategoryId,
                        AuthorName = a.Author.FirstName + " " + a.Author.LastName,
                        AuthorAvatarUrl = a.Author.AvatarUrl,
                        PublishedAt = a.PublishedAt,
                        UpdatedAt = a.UpdatedAt,
                        ViewCount = a.ViewCount ?? 0,
                        IsFeatured = a.IsFeatured ?? false,
                        IsPublished = a.IsPublished ?? false,
                        Tags = a.Tags
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting article by slug: {slug}");
                return null;
            }
        }

        public async Task IncrementViewCountAsync(int articleId)
        {
            try
            {
                var article = await _context.NewsArticles.FindAsync(articleId);
                if (article != null)
                {
                    article.ViewCount = (article.ViewCount ?? 0) + 1;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error incrementing view count for article: {articleId}");
            }
        }
        public async Task<(List<NewsArticleAdminViewModel> Articles, int TotalCount)> GetAllArticlesForAdminAsync(
            int page = 1,
            int pageSize = 20,
            bool? isPublished = null,
            int? categoryId = null,
            int? authorId = null)
        {
            try
            {
                var query = _context.NewsArticles
                    .Include(a => a.Author)
                    .Include(a => a.Category)
                    .AsQueryable();

                if (isPublished.HasValue)
                {
                    query = query.Where(a => a.IsPublished == isPublished.Value);
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(a => a.CategoryId == categoryId.Value);
                }

                if (authorId.HasValue)
                {
                    query = query.Where(a => a.AuthorId == authorId.Value);
                }

                var totalCount = await query.CountAsync();

                var articles = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new NewsArticleAdminViewModel
                    {
                        ArticleId = a.ArticleId,
                        Title = a.Title,
                        Slug = a.Slug,
                        CategoryName = a.Category.CategoryName,
                        AuthorName = a.Author.FirstName + " " + a.Author.LastName,
                        IsPublished = a.IsPublished ?? false,
                        IsFeatured = a.IsFeatured ?? false,
                        ViewCount = a.ViewCount ?? 0,
                        PublishedAt = a.PublishedAt,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt
                    })
                    .ToListAsync();

                return (articles, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting articles for admin");
                return (new List<NewsArticleAdminViewModel>(), 0);
            }
        }

        public async Task<NewsArticleEditViewModel?> GetArticleForEditAsync(int articleId)
        {
            try
            {
                return await _context.NewsArticles
                    .Where(a => a.ArticleId == articleId)
                    .Select(a => new NewsArticleEditViewModel
                    {
                        ArticleId = a.ArticleId,
                        Title = a.Title,
                        Slug = a.Slug,
                        Content = a.Content,
                        Excerpt = a.Excerpt,
                        FeaturedImageUrl = a.FeaturedImageUrl,
                        CategoryId = a.CategoryId,
                        IsPublished = a.IsPublished ?? false,
                        IsFeatured = a.IsFeatured ?? false,
                        Tags = a.Tags
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting article for edit: {articleId}");
                return null;
            }
        }

        public async Task<(bool Success, int? ArticleId, string Message)> CreateArticleAsync(
            NewsArticleCreateViewModel model,
            int authorId)
        {
            try
            {
                // Generate slug if not provided
                var slug = string.IsNullOrWhiteSpace(model.Slug)
                    ? GenerateSlug(model.Title)
                    : GenerateSlug(model.Slug);

                // Ensure slug is unique
                if (!await IsSlugUniqueAsync(slug))
                {
                    slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 8)}";
                }

                var article = new NewsArticle
                {
                    Title = model.Title,
                    Slug = slug,
                    Content = model.Content,
                    Excerpt = model.Excerpt,
                    FeaturedImageUrl = model.FeaturedImageUrl,
                    CategoryId = model.CategoryId,
                    AuthorId = authorId,
                    IsPublished = model.IsPublished,
                    IsFeatured = model.IsFeatured,
                    Tags = model.Tags,
                    ViewCount = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                if (model.IsPublished)
                {
                    article.PublishedAt = DateTime.Now;
                }

                _context.NewsArticles.Add(article);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"News article created: {article.ArticleId} - {article.Title}");
                return (true, article.ArticleId, "Article created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news article");
                return (false, null, "An error occurred while creating the article");
            }
        }
        public async Task<(bool Success, string Message)> UpdateArticleAsync(
            int articleId,
            NewsArticleEditViewModel model,
            int updatedBy)
        {
            try
            {
                var article = await _context.NewsArticles.FindAsync(articleId);
                if (article == null)
                {
                    return (false, "Article not found");
                }

                // Update slug if title changed
                var slug = string.IsNullOrWhiteSpace(model.Slug)
                    ? GenerateSlug(model.Title)
                    : GenerateSlug(model.Slug);

                // Ensure slug is unique (excluding current article)
                if (slug != article.Slug && !await IsSlugUniqueAsync(slug, articleId))
                {
                    slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 8)}";
                }

                article.Title = model.Title;
                article.Slug = slug;
                article.Content = model.Content;
                article.Excerpt = model.Excerpt;
                article.CategoryId = model.CategoryId;
                article.Tags = model.Tags;
                article.IsFeatured = model.IsFeatured;
                article.UpdatedAt = DateTime.Now;
                article.UpdatedBy = updatedBy;

                // Update featured image if provided
                if (!string.IsNullOrWhiteSpace(model.FeaturedImageUrl))
                {
                    article.FeaturedImageUrl = model.FeaturedImageUrl;
                }

                // Handle publish status change
                if (model.IsPublished && article.IsPublished == false)
                {
                    article.PublishedAt = DateTime.Now;
                }
                article.IsPublished = model.IsPublished;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"News article updated: {article.ArticleId} - {article.Title}");
                return (true, "Article updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating news article: {articleId}");
                return (false, "An error occurred while updating the article");
            }
        }

        public async Task<(bool Success, string Message)> DeleteArticleAsync(int articleId)
        {
            try
            {
                var article = await _context.NewsArticles.FindAsync(articleId);
                if (article == null)
                {
                    return (false, "Article not found");
                }

                _context.NewsArticles.Remove(article);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"News article deleted: {articleId}");
                return (true, "Article deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting news article: {articleId}");
                return (false, "An error occurred while deleting the article");
            }
        }

        public async Task<(bool Success, string Message)> TogglePublishStatusAsync(int articleId, int updatedBy)
        {
            try
            {
                var article = await _context.NewsArticles.FindAsync(articleId);
                if (article == null)
                {
                    return (false, "Article not found");
                }

                article.IsPublished = !article.IsPublished;
                article.UpdatedBy = updatedBy;
                article.UpdatedAt = DateTime.Now;

                if (article.IsPublished == true && article.PublishedAt == null)
                {
                    article.PublishedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                var status = article.IsPublished == true ? "published" : "unpublished";
                _logger.LogInformation($"News article {status}: {articleId}");
                return (true, $"Article {status} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling publish status for article: {articleId}");
                return (false, "An error occurred while updating the article status");
            }
        }

        public async Task<List<NewsCategory>> GetAllCategoriesAsync()
        {
            try
            {
                return await _context.NewsCategories
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all news categories");
                return new List<NewsCategory>();
            }
        }

        public async Task<List<NewsCategory>> GetActiveCategoriesAsync()
        {
            try
            {
                return await _context.NewsCategories
                    .Where(c => c.IsActive == true)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active news categories");
                return new List<NewsCategory>();
            }
        }

        public string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            // Convert to lowercase
            var slug = title.ToLowerInvariant();

            // Remove accents and special characters
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // Replace spaces with hyphens
            slug = Regex.Replace(slug, @"\s+", "-");

            // Remove consecutive hyphens
            slug = Regex.Replace(slug, @"-+", "-");

            // Trim hyphens from start and end
            slug = slug.Trim('-');

            // Limit length
            if (slug.Length > 300)
            {
                slug = slug.Substring(0, 300).TrimEnd('-');
            }

            return slug;
        }

        public async Task<bool> IsSlugUniqueAsync(string slug, int? excludeArticleId = null)
        {
            try
            {
                var query = _context.NewsArticles.Where(a => a.Slug == slug);

                if (excludeArticleId.HasValue)
                {
                    query = query.Where(a => a.ArticleId != excludeArticleId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking slug uniqueness: {slug}");
                return false;
            }
        }
    }
}
