using DoAnChuyenNganh.Services;
using DoAnChuyenNganh.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;

namespace DoAnChuyenNganh.Controllers
{
    public class ForumController : Controller
    {
        private readonly IForumService _forumService;
        private readonly ILogger<ForumController> _logger;

        public ForumController(IForumService forumService, ILogger<ForumController> logger)
        {
            _forumService = forumService;
            _logger = logger;
        }

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdCookie = Request.Cookies["AuthSession"];
            if (string.IsNullOrEmpty(userIdCookie))
            {
                return null;
            }

            // In a real application, you would validate the session and get the user ID
            // For now, we'll store it in TempData or Session
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return HttpContext.Session.GetInt32("UserId");
            }

            return null;
        }

        private bool IsAuthenticated()
        {
            return GetCurrentUserId().HasValue;
        }

        #endregion

        #region Forum Index and Categories

        // GET: /Forum
        public async Task<IActionResult> Index()
        {
            var model = new ForumIndexViewModel
            {
                Categories = await _forumService.GetAllCategoriesAsync(),
                Statistics = await _forumService.GetStatisticsAsync(),
                RecentPosts = await _forumService.GetRecentPostsAsync(10),
                PopularPosts = await _forumService.GetPopularPostsAsync(5)
            };

            return View(model);
        }

        // GET: /Forum/Category/5
        public async Task<IActionResult> Category(short id, int page = 1, string sortBy = "latest", string filterBy = "all")
        {
            var model = await _forumService.GetCategoryDetailAsync(id, page, 20, sortBy, filterBy);
            
            if (model.Category == null)
            {
                return NotFound();
            }

            return View(model);
        }

        #endregion

        #region Post Operations

        // GET: /Forum/Post/123
        public async Task<IActionResult> Post(int id, int page = 1)
        {
            var currentUserId = GetCurrentUserId();
            var model = await _forumService.GetPostDetailAsync(id, page, 20, currentUserId);

            if (model.Post == null)
            {
                return NotFound();
            }

            // Increment view count
            await _forumService.IncrementViewCountAsync(id);

            return View(model);
        }

        // GET: /Forum/CreatePost?categoryId=5
        public async Task<IActionResult> CreatePost(short? categoryId)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("CreatePost", "Forum", new { categoryId }) });
            }

            var categories = await _forumService.GetAllCategoriesAsync();
            ViewBag.Categories = categories;

            var model = new ForumPostViewModel();
            if (categoryId.HasValue)
            {
                model.CategoryId = categoryId.Value;
            }

            return View(model);
        }

        // POST: /Forum/CreatePost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(ForumPostViewModel model, List<IFormFile>? UploadedImages, List<IFormFile>? UploadedFiles)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            // Attach uploaded files to model
            model.UploadedImages = UploadedImages;
            model.UploadedFiles = UploadedFiles;

            if (!ModelState.IsValid)
            {
                var categories = await _forumService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _forumService.CreatePostAsync(model, userId);

            if (result.Success && result.PostId.HasValue)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Post", new { id = result.PostId.Value });
            }

            ModelState.AddModelError("", result.Message);
            var categoriesList = await _forumService.GetAllCategoriesAsync();
            ViewBag.Categories = categoriesList;
            return View(model);
        }

        // GET: /Forum/EditPost/123
        public async Task<IActionResult> EditPost(int id)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("EditPost", "Forum", new { id }) });
            }

            var userId = GetCurrentUserId()!.Value;
            var post = await _forumService.GetPostByIdAsync(id, userId);

            if (post == null)
            {
                return NotFound();
            }

            if (!await _forumService.CanUserEditPostAsync(id, userId))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this post";
                return RedirectToAction("Post", new { id });
            }

            var categories = await _forumService.GetAllCategoriesAsync();
            ViewBag.Categories = categories;

            return View(post);
        }

        // POST: /Forum/EditPost/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id, ForumPostViewModel model, List<IFormFile>? UploadedImages, List<IFormFile>? UploadedFiles)
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            // Attach uploaded files to model
            model.UploadedImages = UploadedImages;
            model.UploadedFiles = UploadedFiles;

            if (!ModelState.IsValid)
            {
                var categories = await _forumService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            var userId = GetCurrentUserId()!.Value;
            model.PostId = id;
            var result = await _forumService.UpdatePostAsync(model, userId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Post", new { id });
            }

            ModelState.AddModelError("", result.Message);
            var categoriesList = await _forumService.GetAllCategoriesAsync();
            ViewBag.Categories = categoriesList;
            return View(model);
        }

        // POST: /Forum/DeletePost/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "You must be logged in" });
            }

            var userId = GetCurrentUserId()!.Value;
            var post = await _forumService.GetPostByIdAsync(id);
            
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found" });
            }

            var categoryId = post.CategoryId;
            var result = await _forumService.DeletePostAsync(id, userId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return Json(new { success = true, message = result.Message, redirectUrl = Url.Action("Category", new { id = categoryId }) });
            }

            return Json(new { success = false, message = result.Message });
        }

        #endregion

        #region Reply Operations

        // POST: /Forum/CreateReply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReply(ForumReplyViewModel model, List<IFormFile>? UploadedImages, List<IFormFile>? UploadedFiles)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "You must be logged in to reply" });
            }

            // Attach uploaded files to model
            model.UploadedImages = UploadedImages;
            model.UploadedFiles = UploadedFiles;

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid reply data" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _forumService.CreateReplyAsync(model, userId);

            if (result.Success)
            {
                return Json(new { success = true, message = result.Message, replyId = result.ReplyId });
            }

            return Json(new { success = false, message = result.Message });
        }

        // POST: /Forum/EditReply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReply(ForumReplyViewModel model, List<IFormFile>? UploadedImages, List<IFormFile>? UploadedFiles)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "You must be logged in" });
            }

            // Attach uploaded files to model
            model.UploadedImages = UploadedImages;
            model.UploadedFiles = UploadedFiles;

            var userId = GetCurrentUserId()!.Value;
            var result = await _forumService.UpdateReplyAsync(model, userId);

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Forum/DeleteReply/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int id)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "You must be logged in" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _forumService.DeleteReplyAsync(id, userId);

            return Json(new { success = result.Success, message = result.Message });
        }

        #endregion

        #region Attachment Operations

        // POST: /Forum/DeleteAttachment/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "You must be logged in" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _forumService.DeleteAttachmentAsync(id, userId);

            return Json(new { success = result.Success, message = result.Message });
        }

        #endregion

        #region Like and Bookmark Operations

        // POST: /Forum/LikePost/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikePost(int id)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "You must be logged in to like posts" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _forumService.ToggleLikePostAsync(id, userId);

            return Json(new {
                success = result.Success,
                message = result.Message,
                isLiked = result.IsLiked,
                newCount = result.NewCount
            });
        }

        // POST: /Forum/LikeReply/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikeReply(int id)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "You must be logged in to like replies" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _forumService.ToggleLikeReplyAsync(id, userId);

            return Json(new {
                success = result.Success,
                message = result.Message,
                isLiked = result.IsLiked,
                newCount = result.NewCount
            });
        }

        // POST: /Forum/BookmarkPost/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookmarkPost(int id, string? title = null)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "You must be logged in to bookmark posts" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _forumService.ToggleBookmarkPostAsync(id, userId, title);

            return Json(new {
                success = result.Success,
                message = result.Message,
                isBookmarked = result.IsBookmarked
            });
        }

        #endregion

        #region Moderation Operations

        // POST: /Forum/ToggleSticky/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSticky(int id)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "You must be logged in" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _forumService.ToggleStickyAsync(id, userId);

            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Forum/ToggleLock/123
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            if (!IsAuthenticated())
            {
                return Json(new { success = false, message = "You must be logged in" });
            }

            var userId = GetCurrentUserId()!.Value;
            var result = await _forumService.ToggleLockAsync(id, userId);

            return Json(new { success = result.Success, message = result.Message });
        }

        #endregion

        #region Search and User Activity

        // GET: /Forum/Search
        public async Task<IActionResult> Search(string query, short? categoryId, string? author,
            string searchIn = "all", string sortBy = "relevance", DateTime? dateFrom = null,
            DateTime? dateTo = null, int page = 1)
        {
            var model = new ForumSearchViewModel
            {
                Query = query,
                CategoryId = categoryId,
                Author = author,
                SearchIn = searchIn,
                SortBy = sortBy,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Pagination = new PaginationViewModel { CurrentPage = page, PageSize = 20 }
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                model = await _forumService.SearchAsync(model);
            }

            var categories = await _forumService.GetAllCategoriesAsync();
            ViewBag.Categories = categories;

            return View(model);
        }

        // GET: /Forum/UserActivity/123
        public async Task<IActionResult> UserActivity(int id, int page = 1, string tab = "posts")
        {
            var model = await _forumService.GetUserActivityAsync(id, page, 20, tab);

            if (string.IsNullOrEmpty(model.Username))
            {
                return NotFound();
            }

            return View(model);
        }

        #endregion
    }
}
