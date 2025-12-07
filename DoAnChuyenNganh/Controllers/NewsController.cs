using DoAnChuyenNganh.Services;
using DoAnChuyenNganh.ViewModels.News;
using Microsoft.AspNetCore.Mvc;

namespace DoAnChuyenNganh.Controllers
{
    public class NewsController : Controller
    {
        private readonly INewsService _newsService;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<NewsController> _logger;

        public NewsController(
            INewsService newsService, 
            IFileUploadService fileUploadService,
            ILogger<NewsController> logger)
        {
            _newsService = newsService;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdCookie = Request.Cookies["AuthSession"];
            _logger.LogInformation("GetCurrentUserId - AuthSession Cookie: {Cookie}",
                string.IsNullOrEmpty(userIdCookie) ? "NULL/EMPTY" : "EXISTS");

            if (string.IsNullOrEmpty(userIdCookie))
            {
                _logger.LogWarning("GetCurrentUserId - No AuthSession cookie found");
                return null;
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation("GetCurrentUserId - Session UserId: {UserId}",
                userId.HasValue ? userId.Value.ToString() : "NULL");

            if (userId.HasValue)
            {
                return userId;
            }

            _logger.LogWarning("GetCurrentUserId - AuthSession cookie exists but UserId not in session");
            return null;
        }

        private bool IsAuthenticated()
        {
            var isAuth = GetCurrentUserId().HasValue;
            _logger.LogInformation("IsAuthenticated - Result: {IsAuthenticated}", isAuth);
            return isAuth;
        }

        private bool IsAdmin()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            _logger.LogInformation("IsAdmin - Session RoleId: {RoleId}",
                roleId.HasValue ? roleId.Value.ToString() : "NULL");

            var result = roleId.HasValue && roleId.Value == 4;
            _logger.LogInformation("IsAdmin - Result: {IsAdmin} (Expected RoleId=4, Got: {RoleId})",
                result, roleId.HasValue ? roleId.Value.ToString() : "NULL");

            return result;
        }

        private bool IsWriter()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            _logger.LogInformation("IsWriter - Session RoleId: {RoleId}",
                roleId.HasValue ? roleId.Value.ToString() : "NULL");

            var result = roleId.HasValue && roleId.Value == 5;
            _logger.LogInformation("IsWriter - Result: {IsWriter} (Expected RoleId=5, Got: {RoleId})",
                result, roleId.HasValue ? roleId.Value.ToString() : "NULL");

            return result;
        }

        private bool CanManageNews()
        {
            var isAdmin = IsAdmin();
            var isWriter = IsWriter();
            var result = isAdmin || isWriter;

            _logger.LogInformation("CanManageNews - IsAdmin: {IsAdmin}, IsWriter: {IsWriter}, Result: {CanManage}",
                isAdmin, isWriter, result);

            return result;
        }

        private bool CanEditArticle(int authorId)
        {
            if (IsAdmin())
                return true;

            if (IsWriter())
            {
                var currentUserId = GetCurrentUserId();
                return currentUserId.HasValue && currentUserId.Value == authorId;
            }

            return false;
        }

        #endregion

        #region Public News Pages

        // GET: /News
        public async Task<IActionResult> Index(int page = 1, int? categoryId = null, string? search = null)
        {
            const int pageSize = 12;

            var (articles, totalCount) = await _newsService.GetPublishedArticlesAsync(
                page, 
                pageSize, 
                categoryId, 
                search);

            var featuredArticles = await _newsService.GetFeaturedArticlesAsync(5);
            var hotArticles = await _newsService.GetHotArticlesAsync(10, 7);
            var categories = await _newsService.GetActiveCategoriesAsync();

            var model = new NewsIndexViewModel
            {
                Articles = articles,
                FeaturedArticles = featuredArticles,
                HotArticles = hotArticles,
                Categories = categories,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalArticles = totalCount,
                SelectedCategoryId = categoryId,
                SearchTerm = search
            };

            return View(model);
        }

        // GET: /News/Details/{slug}
        [HttpGet("News/Details/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var article = await _newsService.GetArticleBySlugAsync(slug);

            if (article == null)
            {
                return NotFound();
            }

            // Only show published articles to non-admin users
            if (!article.IsPublished && !IsAdmin())
            {
                return NotFound();
            }

            // Increment view count
            await _newsService.IncrementViewCountAsync(article.ArticleId);

            // Get related articles
            article.RelatedArticles = await _newsService.GetRelatedArticlesAsync(article.ArticleId, 5);

            return View(article);
        }

        #endregion

        #region Admin Management

        // GET: /News/Admin
        public async Task<IActionResult> Admin(int page = 1, bool? isPublished = null, int? categoryId = null)
        {
            _logger.LogInformation("========== NEWS ADMIN ACCESS ATTEMPT ==========");
            _logger.LogInformation("Admin Action - Page: {Page}, IsPublished: {IsPublished}, CategoryId: {CategoryId}",
                page, isPublished?.ToString() ?? "NULL", categoryId?.ToString() ?? "NULL");

            // Log all session values
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var sessionRoleId = HttpContext.Session.GetInt32("RoleId");
            var authCookie = Request.Cookies["AuthSession"];

            _logger.LogInformation("Admin Action - Session Values:");
            _logger.LogInformation("  - UserId from Session: {UserId}", sessionUserId?.ToString() ?? "NULL");
            _logger.LogInformation("  - RoleId from Session: {RoleId}", sessionRoleId?.ToString() ?? "NULL");
            _logger.LogInformation("  - AuthSession Cookie: {Cookie}", string.IsNullOrEmpty(authCookie) ? "NULL/EMPTY" : "EXISTS");

            // Log all available session keys
            var sessionKeys = HttpContext.Session.Keys.ToList();
            _logger.LogInformation("Admin Action - All Session Keys: {Keys}",
                sessionKeys.Count > 0 ? string.Join(", ", sessionKeys) : "NONE");

            // Check authentication
            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("Admin Action - GetCurrentUserId() returned: {UserId}",
                currentUserId?.ToString() ?? "NULL");

            // Check permissions
            var canManage = CanManageNews();
            _logger.LogInformation("Admin Action - CanManageNews() returned: {CanManage}", canManage);

            if (!canManage)
            {
                _logger.LogWarning("Admin Action - ACCESS DENIED - Redirecting to Login");
                _logger.LogWarning("Admin Action - Redirect Reason: CanManageNews() = false");
                _logger.LogInformation("=================================================");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("Admin Action - ACCESS GRANTED - Loading admin panel");

            const int pageSize = 20;

            // Writers can only see their own articles
            int? authorIdFilter = IsWriter() && currentUserId.HasValue ? currentUserId.Value : null;
            _logger.LogInformation("Admin Action - Author Filter: {AuthorFilter}",
                authorIdFilter?.ToString() ?? "NULL (Admin - sees all)");

            var (articles, totalCount) = await _newsService.GetAllArticlesForAdminAsync(
                page,
                pageSize,
                isPublished,
                categoryId,
                authorIdFilter);

            var categories = await _newsService.GetAllCategoriesAsync();

            _logger.LogInformation("Admin Action - Loaded {ArticleCount} articles (Total: {TotalCount})",
                articles.Count, totalCount);

            ViewBag.Articles = articles;
            ViewBag.Categories = categories;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalArticles = totalCount;
            ViewBag.SelectedPublishStatus = isPublished;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.IsAdmin = IsAdmin();
            ViewBag.IsWriter = IsWriter();

            _logger.LogInformation("Admin Action - Rendering view successfully");
            _logger.LogInformation("=================================================");

            return View();
        }

        // GET: /News/Create
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Create (GET) - Access attempt");

            if (!CanManageNews())
            {
                _logger.LogWarning("Create (GET) - ACCESS DENIED - Redirecting to Login");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("Create (GET) - ACCESS GRANTED");
            ViewBag.Categories = await _newsService.GetActiveCategoriesAsync();
            return View();
        }

        // POST: /News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsArticleCreateViewModel model, IFormFile? featuredImage)
        {
            if (!CanManageNews())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _newsService.GetActiveCategoriesAsync();
                return View(model);
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Handle featured image upload
            if (featuredImage != null && featuredImage.Length > 0)
            {
                var uploadResult = await _fileUploadService.UploadImageAsync(
                    featuredImage,
                    "uploads/news/images",
                    maxWidth: 1200,
                    maxHeight: 800,
                    maxSizeBytes: 5242880); // 5MB

                if (uploadResult.Success)
                {
                    model.FeaturedImageUrl = uploadResult.FilePath;
                }
                else
                {
                    ModelState.AddModelError("FeaturedImage", uploadResult.Message);
                    ViewBag.Categories = await _newsService.GetActiveCategoriesAsync();
                    return View(model);
                }
            }

            var result = await _newsService.CreateArticleAsync(model, userId.Value);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Admin));
            }

            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.Categories = await _newsService.GetActiveCategoriesAsync();
            return View(model);
        }

        // GET: /News/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            if (!CanManageNews())
            {
                return RedirectToAction("Login", "Account");
            }

            var article = await _newsService.GetArticleForEditAsync(id);
            if (article == null)
            {
                return NotFound();
            }

            // Check if user can edit this article
            if (!CanEditArticle(article.AuthorId))
            {
                TempData["Error"] = "You don't have permission to edit this article.";
                return RedirectToAction("Admin");
            }

            ViewBag.Categories = await _newsService.GetActiveCategoriesAsync();
            return View(article);
        }

        // POST: /News/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NewsArticleEditViewModel model, IFormFile? featuredImage)
        {
            if (!CanManageNews())
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user can edit this article
            var existingArticle = await _newsService.GetArticleEntityByIdAsync(id);
            if (existingArticle == null)
            {
                return NotFound();
            }

            if (!CanEditArticle(existingArticle.AuthorId))
            {
                return Json(new { success = false, message = "You don't have permission to edit this article." });
            }

            if (id != model.ArticleId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _newsService.GetActiveCategoriesAsync();
                return View(model);
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Handle featured image upload
            if (featuredImage != null && featuredImage.Length > 0)
            {
                var uploadResult = await _fileUploadService.UploadImageAsync(
                    featuredImage,
                    "uploads/news/images",
                    maxWidth: 1200,
                    maxHeight: 800,
                    maxSizeBytes: 5242880); // 5MB

                if (uploadResult.Success)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(model.FeaturedImageUrl))
                    {
                        await _fileUploadService.DeleteFileAsync(model.FeaturedImageUrl);
                    }
                    model.FeaturedImageUrl = uploadResult.FilePath;
                }
                else
                {
                    ModelState.AddModelError("FeaturedImage", uploadResult.Message);
                    ViewBag.Categories = await _newsService.GetActiveCategoriesAsync();
                    return View(model);
                }
            }

            var result = await _newsService.UpdateArticleAsync(id, model, userId.Value);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Admin));
            }

            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.Categories = await _newsService.GetActiveCategoriesAsync();
            return View(model);
        }

        // POST: /News/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!CanManageNews())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            // Check if user can delete this article
            var article = await _newsService.GetArticleEntityByIdAsync(id);
            if (article == null)
            {
                return Json(new { success = false, message = "Article not found" });
            }

            if (!CanEditArticle(article.AuthorId))
            {
                return Json(new { success = false, message = "You don't have permission to delete this article." });
            }

            var result = await _newsService.DeleteArticleAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /News/TogglePublish/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            // Only admins can toggle publish status
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Only administrators can publish/unpublish articles" });
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var result = await _newsService.TogglePublishStatusAsync(id, userId.Value);

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /News/UploadImage (for TinyMCE editor)
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (!CanManageNews())
            {
                return Json(new { error = "Unauthorized" });
            }

            if (file == null || file.Length == 0)
            {
                return Json(new { error = "No file uploaded" });
            }

            var uploadResult = await _fileUploadService.UploadImageAsync(
                file,
                "uploads/news/images",
                maxSizeBytes: 5242880); // 5MB

            if (uploadResult.Success)
            {
                return Json(new { location = uploadResult.FilePath });
            }

            return Json(new { error = uploadResult.Message });
        }

        #endregion
    }
}


