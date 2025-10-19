using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.Forum;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyenNganh.Services
{
    public class ForumService : IForumService
    {
        private readonly DoAnChuyenNganhContext _context;
        private readonly ILogger<ForumService> _logger;
        private readonly IFileUploadService _fileUploadService;
        private readonly INotificationService _notificationService;

        public ForumService(
            DoAnChuyenNganhContext context,
            ILogger<ForumService> logger,
            IFileUploadService fileUploadService,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _fileUploadService = fileUploadService;
            _notificationService = notificationService;
        }

        #region Category Operations

        public async Task<List<ForumCategoryViewModel>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _context.ForumCategories
                    .Include(c => c.LastPostByNavigation)
                    .Where(c => c.IsActive == true)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.CategoryName)
                    .Select(c => new ForumCategoryViewModel
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        Description = c.Description,
                        SortOrder = c.SortOrder ?? 0,
                        PostCount = c.PostCount ?? 0,
                        LastPostAt = c.LastPostAt,
                        LastPostByUsername = c.LastPostByNavigation != null ? c.LastPostByNavigation.Username : null,
                        IsActive = c.IsActive ?? true,
                        CreatedAt = c.CreatedAt
                    })
                    .ToListAsync();

                // Get last post title for each category
                foreach (var category in categories)
                {
                    var lastPost = await _context.ForumPosts
                        .Where(p => p.CategoryId == category.CategoryId)
                        .OrderByDescending(p => p.CreatedAt)
                        .Select(p => new { p.PostId, p.Title })
                        .FirstOrDefaultAsync();

                    if (lastPost != null)
                    {
                        category.LastPostTitle = lastPost.Title;
                        category.LastPostId = lastPost.PostId;
                    }
                }

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                return new List<ForumCategoryViewModel>();
            }
        }

        public async Task<ForumCategoryViewModel?> GetCategoryByIdAsync(short categoryId)
        {
            try
            {
                var category = await _context.ForumCategories
                    .Include(c => c.LastPostByNavigation)
                    .Include(c => c.CreatedByNavigation)
                    .Where(c => c.CategoryId == categoryId)
                    .Select(c => new ForumCategoryViewModel
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        Description = c.Description,
                        SortOrder = c.SortOrder ?? 0,
                        PostCount = c.PostCount ?? 0,
                        LastPostAt = c.LastPostAt,
                        LastPostByUsername = c.LastPostByNavigation != null ? c.LastPostByNavigation.Username : null,
                        IsActive = c.IsActive ?? true,
                        CreatedAt = c.CreatedAt,
                        CreatedByUsername = c.CreatedByNavigation.Username
                    })
                    .FirstOrDefaultAsync();

                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by ID: {CategoryId}", categoryId);
                return null;
            }
        }

        public async Task<ForumCategoryDetailViewModel> GetCategoryDetailAsync(
            short categoryId, int page, int pageSize, string sortBy, string filterBy)
        {
            try
            {
                var category = await GetCategoryByIdAsync(categoryId);
                if (category == null)
                {
                    return new ForumCategoryDetailViewModel();
                }

                var query = _context.ForumPosts
                    .Include(p => p.User)
                    .Where(p => p.CategoryId == categoryId && p.IsApproved == true);

                // Apply filters
                query = filterBy switch
                {
                    "sticky" => query.Where(p => p.IsSticky == true),
                    "locked" => query.Where(p => p.IsLocked == true),
                    "unanswered" => query.Where(p => p.ReplyCount == 0),
                    _ => query
                };

                // Apply sorting
                query = sortBy switch
                {
                    "popular" => query.OrderByDescending(p => p.ViewCount),
                    "trending" => query.OrderByDescending(p => p.LikeCount).ThenByDescending(p => p.ViewCount),
                    "mostReplied" => query.OrderByDescending(p => p.ReplyCount),
                    "mostLiked" => query.OrderByDescending(p => p.LikeCount),
                    _ => query.OrderByDescending(p => p.IsSticky).ThenByDescending(p => p.CreatedAt)
                };

                var totalItems = await query.CountAsync();
                var postEntities = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var posts = postEntities.Select(p => MapToPostViewModel(p, null)).ToList();

                return new ForumCategoryDetailViewModel
                {
                    Category = category,
                    Posts = posts,
                    Pagination = new PaginationViewModel
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalItems
                    },
                    SortBy = sortBy,
                    FilterBy = filterBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category detail: {CategoryId}", categoryId);
                return new ForumCategoryDetailViewModel();
            }
        }

        public async Task<(bool Success, string Message)> CreateCategoryAsync(ForumCategoryViewModel model, int userId)
        {
            try
            {
                var category = new ForumCategory
                {
                    CategoryName = model.CategoryName,
                    Description = model.Description,
                    SortOrder = model.SortOrder ?? 0,
                    PostCount = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = userId
                };

                _context.ForumCategories.Add(category);
                await _context.SaveChangesAsync();

                return (true, "Category created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return (false, "An error occurred while creating the category");
            }
        }

        public async Task<(bool Success, string Message)> UpdateCategoryAsync(ForumCategoryViewModel model, int userId)
        {
            try
            {
                var category = await _context.ForumCategories.FindAsync(model.CategoryId);
                if (category == null)
                {
                    return (false, "Category not found");
                }

                category.CategoryName = model.CategoryName;
                category.Description = model.Description;
                category.SortOrder = model.SortOrder ?? 0;
                category.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                return (true, "Category updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                return (false, "An error occurred while updating the category");
            }
        }

        public async Task<(bool Success, string Message)> DeleteCategoryAsync(short categoryId)
        {
            try
            {
                var category = await _context.ForumCategories.FindAsync(categoryId);
                if (category == null)
                {
                    return (false, "Category not found");
                }

                // Check if category has posts
                var hasPost = await _context.ForumPosts.AnyAsync(p => p.CategoryId == categoryId);
                if (hasPost)
                {
                    return (false, "Cannot delete category with existing posts");
                }

                _context.ForumCategories.Remove(category);
                await _context.SaveChangesAsync();

                return (true, "Category deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return (false, "An error occurred while deleting the category");
            }
        }

        #endregion

        #region Post Operations

        public async Task<ForumPostViewModel?> GetPostByIdAsync(int postId, int? currentUserId = null)
        {
            try
            {
                var post = await _context.ForumPosts
                    .Include(p => p.User)
                    .Include(p => p.Category)
                    .Include(p => p.UpdatedByNavigation)
                    .Include(p => p.ForumAttachments)
                    .FirstOrDefaultAsync(p => p.PostId == postId);

                if (post == null) return null;

                var viewModel = MapToPostViewModel(post, currentUserId);

                // Get last reply info
                var lastReply = await _context.ForumReplies
                    .Include(r => r.User)
                    .Where(r => r.PostId == postId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastReply != null)
                {
                    viewModel.LastReplyUsername = lastReply.User.Username;
                    viewModel.LastReplyAt = lastReply.CreatedAt;
                }

                // Check if liked/bookmarked by current user
                if (currentUserId.HasValue)
                {
                    viewModel.IsLikedByCurrentUser = await IsPostLikedByUserAsync(postId, currentUserId.Value);
                    viewModel.IsBookmarkedByCurrentUser = await IsPostBookmarkedByUserAsync(postId, currentUserId.Value);
                    viewModel.CanEdit = await CanUserEditPostAsync(postId, currentUserId.Value);
                    viewModel.CanDelete = await CanUserDeletePostAsync(postId, currentUserId.Value);
                    viewModel.CanModerate = await CanUserModerateAsync(currentUserId.Value);
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post by ID: {PostId}", postId);
                return null;
            }
        }

        public async Task<ForumPostDetailViewModel> GetPostDetailAsync(int postId, int page, int pageSize, int? currentUserId = null)
        {
            try
            {
                var post = await GetPostByIdAsync(postId, currentUserId);
                if (post == null)
                {
                    return new ForumPostDetailViewModel();
                }

                var replies = await GetRepliesByPostIdAsync(postId, page, pageSize, currentUserId);
                var totalReplies = await _context.ForumReplies.CountAsync(r => r.PostId == postId);

                // Get related posts from same category
                var relatedPostEntities = await _context.ForumPosts
                    .Include(p => p.User)
                    .Where(p => p.CategoryId == post.CategoryId && p.PostId != postId && p.IsApproved == true)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var relatedPosts = relatedPostEntities.Select(p => MapToPostViewModel(p, currentUserId)).ToList();

                return new ForumPostDetailViewModel
                {
                    Post = post,
                    Replies = replies,
                    Pagination = new PaginationViewModel
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalReplies
                    },
                    CategoryName = post.CategoryName ?? "",
                    CategoryId = post.CategoryId,
                    RelatedPosts = relatedPosts,
                    NewReply = new ForumReplyViewModel { PostId = postId }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post detail: {PostId}", postId);
                return new ForumPostDetailViewModel();
            }
        }

        public async Task<List<ForumPostViewModel>> GetPostsByCategoryAsync(
            short categoryId, int page, int pageSize, string sortBy, string filterBy)
        {
            try
            {
                var query = _context.ForumPosts
                    .Include(p => p.User)
                    .Where(p => p.CategoryId == categoryId && p.IsApproved == true);

                // Apply filters
                query = filterBy switch
                {
                    "sticky" => query.Where(p => p.IsSticky == true),
                    "locked" => query.Where(p => p.IsLocked == true),
                    "unanswered" => query.Where(p => p.ReplyCount == 0),
                    _ => query
                };

                // Apply sorting
                query = sortBy switch
                {
                    "popular" => query.OrderByDescending(p => p.ViewCount),
                    "trending" => query.OrderByDescending(p => p.LikeCount).ThenByDescending(p => p.ViewCount),
                    "mostReplied" => query.OrderByDescending(p => p.ReplyCount),
                    "mostLiked" => query.OrderByDescending(p => p.LikeCount),
                    _ => query.OrderByDescending(p => p.IsSticky).ThenByDescending(p => p.CreatedAt)
                };

                var postEntities = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return postEntities.Select(p => MapToPostViewModel(p, null)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting posts by category");
                return new List<ForumPostViewModel>();
            }
        }

        public async Task<List<ForumPostViewModel>> GetRecentPostsAsync(int count = 10)
        {
            try
            {
                var postEntities = await _context.ForumPosts
                    .Include(p => p.User)
                    .Include(p => p.Category)
                    .Where(p => p.IsApproved == true)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return postEntities.Select(p => MapToPostViewModel(p, null)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent posts");
                return new List<ForumPostViewModel>();
            }
        }

        public async Task<List<ForumPostViewModel>> GetPopularPostsAsync(int count = 10)
        {
            try
            {
                var postEntities = await _context.ForumPosts
                    .Include(p => p.User)
                    .Include(p => p.Category)
                    .Where(p => p.IsApproved == true)
                    .OrderByDescending(p => p.ViewCount)
                    .ThenByDescending(p => p.LikeCount)
                    .Take(count)
                    .ToListAsync();

                return postEntities.Select(p => MapToPostViewModel(p, null)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular posts");
                return new List<ForumPostViewModel>();
            }
        }

        public async Task<List<ForumPostViewModel>> GetTrendingPostsAsync(int count = 10)
        {
            try
            {
                var sevenDaysAgo = DateTime.Now.AddDays(-7);
                var postEntities = await _context.ForumPosts
                    .Include(p => p.User)
                    .Include(p => p.Category)
                    .Where(p => p.IsApproved == true && p.CreatedAt >= sevenDaysAgo)
                    .OrderByDescending(p => p.LikeCount + p.ReplyCount + (p.ViewCount / 10))
                    .Take(count)
                    .ToListAsync();

                return postEntities.Select(p => MapToPostViewModel(p, null)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending posts");
                return new List<ForumPostViewModel>();
            }
        }

        public async Task<(bool Success, int? PostId, string Message)> CreatePostAsync(ForumPostViewModel model, int userId)
        {
            try
            {
                var post = new ForumPost
                {
                    CategoryId = model.CategoryId,
                    UserId = userId,
                    Title = model.Title,
                    Content = model.Content,
                    IsSticky = false,
                    IsLocked = false,
                    ViewCount = 0,
                    ReplyCount = 0,
                    LikeCount = 0,
                    IsApproved = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ForumPosts.Add(post);
                await _context.SaveChangesAsync();

                // Handle image uploads
                if (model.UploadedImages != null && model.UploadedImages.Any())
                {
                    foreach (var image in model.UploadedImages)
                    {
                        var uploadResult = await _fileUploadService.UploadImageAsync(image, "uploads/forum/images");
                        if (uploadResult.Success && uploadResult.FilePath != null)
                        {
                            var attachment = new ForumAttachment
                            {
                                PostId = post.PostId,
                                FileName = Path.GetFileName(uploadResult.FilePath),
                                OriginalFileName = image.FileName,
                                FilePath = uploadResult.FilePath,
                                FileType = "image",
                                MimeType = image.ContentType,
                                FileSize = image.Length,
                                UploadedBy = userId,
                                CreatedAt = DateTime.Now
                            };
                            _context.ForumAttachments.Add(attachment);
                        }
                    }
                }

                // Handle file uploads
                if (model.UploadedFiles != null && model.UploadedFiles.Any())
                {
                    foreach (var file in model.UploadedFiles)
                    {
                        var uploadResult = await _fileUploadService.UploadFileAsync(file, "uploads/forum/files");
                        if (uploadResult.Success && uploadResult.FilePath != null)
                        {
                            var attachment = new ForumAttachment
                            {
                                PostId = post.PostId,
                                FileName = Path.GetFileName(uploadResult.FilePath),
                                OriginalFileName = file.FileName,
                                FilePath = uploadResult.FilePath,
                                FileType = "file",
                                MimeType = file.ContentType,
                                FileSize = file.Length,
                                UploadedBy = userId,
                                CreatedAt = DateTime.Now
                            };
                            _context.ForumAttachments.Add(attachment);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Update category post count
                await UpdateCategoryPostCountAsync(model.CategoryId);

                return (true, post.PostId, "Post created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                return (false, null, "An error occurred while creating the post");
            }
        }

        public async Task<(bool Success, string Message)> UpdatePostAsync(ForumPostViewModel model, int userId)
        {
            try
            {
                var post = await _context.ForumPosts.FindAsync(model.PostId);
                if (post == null)
                {
                    return (false, "Post not found");
                }

                // Check permissions
                if (!await CanUserEditPostAsync(model.PostId, userId))
                {
                    return (false, "You don't have permission to edit this post");
                }

                post.Title = model.Title;
                post.Content = model.Content;
                post.UpdatedAt = DateTime.Now;
                post.UpdatedBy = userId;

                // Handle new image uploads
                if (model.UploadedImages != null && model.UploadedImages.Any())
                {
                    foreach (var image in model.UploadedImages)
                    {
                        var uploadResult = await _fileUploadService.UploadImageAsync(image, "forum/images");
                        if (uploadResult.Success)
                        {
                            var attachment = new ForumAttachment
                            {
                                PostId = post.PostId,
                                FileName = Path.GetFileName(uploadResult.FilePath!),
                                OriginalFileName = image.FileName,
                                FilePath = uploadResult.FilePath!,
                                FileType = "image",
                                MimeType = image.ContentType,
                                FileSize = image.Length,
                                UploadedBy = userId,
                                CreatedAt = DateTime.Now
                            };
                            _context.ForumAttachments.Add(attachment);
                        }
                    }
                }

                // Handle new file uploads
                if (model.UploadedFiles != null && model.UploadedFiles.Any())
                {
                    foreach (var file in model.UploadedFiles)
                    {
                        var uploadResult = await _fileUploadService.UploadFileAsync(file, "forum/files");
                        if (uploadResult.Success)
                        {
                            var attachment = new ForumAttachment
                            {
                                PostId = post.PostId,
                                FileName = Path.GetFileName(uploadResult.FilePath!),
                                OriginalFileName = file.FileName,
                                FilePath = uploadResult.FilePath!,
                                FileType = "file",
                                MimeType = file.ContentType,
                                FileSize = file.Length,
                                UploadedBy = userId,
                                CreatedAt = DateTime.Now
                            };
                            _context.ForumAttachments.Add(attachment);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return (true, "Post updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post");
                return (false, "An error occurred while updating the post");
            }
        }

        public async Task<(bool Success, string Message)> DeletePostAsync(int postId, int userId)
        {
            try
            {
                var post = await _context.ForumPosts.FindAsync(postId);
                if (post == null)
                {
                    return (false, "Post not found");
                }

                // Check permissions
                if (!await CanUserDeletePostAsync(postId, userId))
                {
                    return (false, "You don't have permission to delete this post");
                }

                // Delete all replies first
                var replies = await _context.ForumReplies.Where(r => r.PostId == postId).ToListAsync();
                _context.ForumReplies.RemoveRange(replies);

                // Delete likes and bookmarks
                var likes = await _context.UserLikes.Where(l => l.ItemType == "post" && l.ItemId == postId).ToListAsync();
                _context.UserLikes.RemoveRange(likes);

                var bookmarks = await _context.UserBookmarks.Where(b => b.ItemType == "post" && b.ItemId == postId).ToListAsync();
                _context.UserBookmarks.RemoveRange(bookmarks);

                _context.ForumPosts.Remove(post);
                await _context.SaveChangesAsync();

                // Update category post count
                await UpdateCategoryPostCountAsync(post.CategoryId);

                return (true, "Post deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post");
                return (false, "An error occurred while deleting the post");
            }
        }

        public async Task IncrementViewCountAsync(int postId)
        {
            try
            {
                var post = await _context.ForumPosts.FindAsync(postId);
                if (post != null)
                {
                    post.ViewCount = (post.ViewCount ?? 0) + 1;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing view count");
            }
        }

        #endregion

        #region Reply Operations

        public async Task<ForumReplyViewModel?> GetReplyByIdAsync(int replyId, int? currentUserId = null)
        {
            try
            {
                var reply = await _context.ForumReplies
                    .Include(r => r.User)
                    .Include(r => r.ParentReply)
                        .ThenInclude(pr => pr.User)
                    .FirstOrDefaultAsync(r => r.ReplyId == replyId);

                if (reply == null) return null;

                var viewModel = MapToReplyViewModel(reply, currentUserId);

                if (currentUserId.HasValue)
                {
                    viewModel.IsLikedByCurrentUser = await IsReplyLikedByUserAsync(replyId, currentUserId.Value);
                    viewModel.CanEdit = reply.UserId == currentUserId.Value || await CanUserModerateAsync(currentUserId.Value);
                    viewModel.CanDelete = reply.UserId == currentUserId.Value || await CanUserModerateAsync(currentUserId.Value);
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reply by ID: {ReplyId}", replyId);
                return null;
            }
        }

        public async Task<List<ForumReplyViewModel>> GetRepliesByPostIdAsync(int postId, int page, int pageSize, int? currentUserId = null)
        {
            try
            {
                var allReplies = await _context.ForumReplies
                    .Include(r => r.User)
                    .Include(r => r.ParentReply)
                        .ThenInclude(pr => pr.User)
                    .Include(r => r.ForumAttachments)
                    .Where(r => r.PostId == postId && r.IsApproved == true)
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync();

                // Build hierarchical structure
                var topLevelReplies = allReplies.Where(r => r.ParentReplyId == null).ToList();
                var result = new List<ForumReplyViewModel>();

                foreach (var reply in topLevelReplies)
                {
                    var viewModel = MapToReplyViewModel(reply, currentUserId);
                    BuildReplyHierarchy(viewModel, allReplies, currentUserId);

                    // Set permissions for the entire tree
                    if (currentUserId.HasValue)
                    {
                        await SetReplyPermissionsAsync(viewModel, currentUserId.Value);
                    }

                    result.Add(viewModel);
                }

                // Apply pagination to top-level replies only
                return result
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting replies by post ID");
                return new List<ForumReplyViewModel>();
            }
        }

        public async Task<(bool Success, int? ReplyId, string Message)> CreateReplyAsync(ForumReplyViewModel model, int userId)
        {
            try
            {
                var reply = new ForumReply
                {
                    PostId = model.PostId,
                    UserId = userId,
                    ParentReplyId = model.ParentReplyId,
                    Content = model.Content,
                    LikeCount = 0,
                    IsApproved = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ForumReplies.Add(reply);
                await _context.SaveChangesAsync();

                // Handle image uploads
                if (model.UploadedImages != null && model.UploadedImages.Any())
                {
                    foreach (var image in model.UploadedImages)
                    {
                        var uploadResult = await _fileUploadService.UploadImageAsync(image, "uploads/forum/images");
                        if (uploadResult.Success && uploadResult.FilePath != null)
                        {
                            var attachment = new ForumAttachment
                            {
                                ReplyId = reply.ReplyId,
                                FileName = Path.GetFileName(uploadResult.FilePath),
                                OriginalFileName = image.FileName,
                                FilePath = uploadResult.FilePath,
                                FileType = "image",
                                MimeType = image.ContentType,
                                FileSize = image.Length,
                                UploadedBy = userId,
                                CreatedAt = DateTime.Now
                            };
                            _context.ForumAttachments.Add(attachment);
                        }
                    }
                }

                // Handle file uploads
                if (model.UploadedFiles != null && model.UploadedFiles.Any())
                {
                    foreach (var file in model.UploadedFiles)
                    {
                        var uploadResult = await _fileUploadService.UploadFileAsync(file, "uploads/forum/files");
                        if (uploadResult.Success && uploadResult.FilePath != null)
                        {
                            var attachment = new ForumAttachment
                            {
                                ReplyId = reply.ReplyId,
                                FileName = Path.GetFileName(uploadResult.FilePath),
                                OriginalFileName = file.FileName,
                                FilePath = uploadResult.FilePath,
                                FileType = "file",
                                MimeType = file.ContentType,
                                FileSize = file.Length,
                                UploadedBy = userId,
                                CreatedAt = DateTime.Now
                            };
                            _context.ForumAttachments.Add(attachment);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Update post reply count
                var post = await _context.ForumPosts.FindAsync(model.PostId);
                if (post != null)
                {
                    post.ReplyCount = (post.ReplyCount ?? 0) + 1;
                    await _context.SaveChangesAsync();
                }

                // Create notifications for reply
                await _notificationService.CreateForumReplyNotificationAsync(
                    model.PostId,
                    reply.ReplyId,
                    userId,
                    model.ParentReplyId);

                // Create notifications for mentions
                await _notificationService.CreateMentionNotificationsAsync(
                    model.Content,
                    userId,
                    "reply",
                    reply.ReplyId);

                return (true, reply.ReplyId, "Reply posted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reply");
                return (false, null, "An error occurred while posting the reply");
            }
        }

        public async Task<(bool Success, string Message)> UpdateReplyAsync(ForumReplyViewModel model, int userId)
        {
            try
            {
                var reply = await _context.ForumReplies.FindAsync(model.ReplyId);
                if (reply == null)
                {
                    return (false, "Reply not found");
                }

                // Check permissions
                if (reply.UserId != userId && !await CanUserModerateAsync(userId))
                {
                    return (false, "You don't have permission to edit this reply");
                }

                reply.Content = model.Content;
                reply.UpdatedAt = DateTime.Now;
                reply.UpdatedBy = userId;

                // Handle new image uploads
                if (model.UploadedImages != null && model.UploadedImages.Any())
                {
                    foreach (var image in model.UploadedImages)
                    {
                        var uploadResult = await _fileUploadService.UploadImageAsync(image, "forum/images");
                        if (uploadResult.Success)
                        {
                            var attachment = new ForumAttachment
                            {
                                ReplyId = reply.ReplyId,
                                FileName = Path.GetFileName(uploadResult.FilePath!),
                                OriginalFileName = image.FileName,
                                FilePath = uploadResult.FilePath!,
                                FileType = "image",
                                MimeType = image.ContentType,
                                FileSize = image.Length,
                                UploadedBy = userId,
                                CreatedAt = DateTime.Now
                            };
                            _context.ForumAttachments.Add(attachment);
                        }
                    }
                }

                // Handle new file uploads
                if (model.UploadedFiles != null && model.UploadedFiles.Any())
                {
                    foreach (var file in model.UploadedFiles)
                    {
                        var uploadResult = await _fileUploadService.UploadFileAsync(file, "forum/files");
                        if (uploadResult.Success)
                        {
                            var attachment = new ForumAttachment
                            {
                                ReplyId = reply.ReplyId,
                                FileName = Path.GetFileName(uploadResult.FilePath!),
                                OriginalFileName = file.FileName,
                                FilePath = uploadResult.FilePath!,
                                FileType = "file",
                                MimeType = file.ContentType,
                                FileSize = file.Length,
                                UploadedBy = userId,
                                CreatedAt = DateTime.Now
                            };
                            _context.ForumAttachments.Add(attachment);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return (true, "Reply updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reply");
                return (false, "An error occurred while updating the reply");
            }
        }

        public async Task<(bool Success, string Message)> DeleteReplyAsync(int replyId, int userId)
        {
            try
            {
                var reply = await _context.ForumReplies.FindAsync(replyId);
                if (reply == null)
                {
                    return (false, "Reply not found");
                }

                // Check permissions
                if (reply.UserId != userId && !await CanUserModerateAsync(userId))
                {
                    return (false, "You don't have permission to delete this reply");
                }

                // Delete child replies recursively
                var childReplies = await _context.ForumReplies.Where(r => r.ParentReplyId == replyId).ToListAsync();
                foreach (var child in childReplies)
                {
                    await DeleteReplyAsync(child.ReplyId, userId);
                }

                // Delete likes
                var likes = await _context.UserLikes.Where(l => l.ItemType == "reply" && l.ItemId == replyId).ToListAsync();
                _context.UserLikes.RemoveRange(likes);

                _context.ForumReplies.Remove(reply);
                await _context.SaveChangesAsync();

                // Update post reply count
                var post = await _context.ForumPosts.FindAsync(reply.PostId);
                if (post != null)
                {
                    post.ReplyCount = Math.Max(0, (post.ReplyCount ?? 0) - 1);
                    await _context.SaveChangesAsync();
                }

                return (true, "Reply deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reply");
                return (false, "An error occurred while deleting the reply");
            }
        }

        #endregion

        #region Like Operations

        public async Task<(bool Success, bool IsLiked, int NewCount, string Message)> ToggleLikePostAsync(int postId, int userId)
        {
            try
            {
                var existingLike = await _context.UserLikes
                    .FirstOrDefaultAsync(l => l.UserId == userId && l.ItemType == "post" && l.ItemId == postId);

                var post = await _context.ForumPosts.FindAsync(postId);
                if (post == null)
                {
                    return (false, false, 0, "Post not found");
                }

                if (existingLike != null)
                {
                    // Unlike
                    _context.UserLikes.Remove(existingLike);
                    post.LikeCount = Math.Max(0, (post.LikeCount ?? 0) - 1);
                    await _context.SaveChangesAsync();
                    return (true, false, post.LikeCount ?? 0, "Post unliked");
                }
                else
                {
                    // Like
                    var like = new UserLike
                    {
                        UserId = userId,
                        ItemType = "post",
                        ItemId = postId,
                        CreatedAt = DateTime.Now
                    };
                    _context.UserLikes.Add(like);
                    post.LikeCount = (post.LikeCount ?? 0) + 1;
                    await _context.SaveChangesAsync();

                    // Create notification for post like
                    await _notificationService.CreatePostLikeNotificationAsync(postId, userId);

                    return (true, true, post.LikeCount ?? 0, "Post liked");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling post like");
                return (false, false, 0, "An error occurred");
            }
        }

        public async Task<(bool Success, bool IsLiked, int NewCount, string Message)> ToggleLikeReplyAsync(int replyId, int userId)
        {
            try
            {
                var existingLike = await _context.UserLikes
                    .FirstOrDefaultAsync(l => l.UserId == userId && l.ItemType == "reply" && l.ItemId == replyId);

                var reply = await _context.ForumReplies.FindAsync(replyId);
                if (reply == null)
                {
                    return (false, false, 0, "Reply not found");
                }

                if (existingLike != null)
                {
                    // Unlike
                    _context.UserLikes.Remove(existingLike);
                    reply.LikeCount = Math.Max(0, (reply.LikeCount ?? 0) - 1);
                    await _context.SaveChangesAsync();
                    return (true, false, reply.LikeCount ?? 0, "Reply unliked");
                }
                else
                {
                    // Like
                    var like = new UserLike
                    {
                        UserId = userId,
                        ItemType = "reply",
                        ItemId = replyId,
                        CreatedAt = DateTime.Now
                    };
                    _context.UserLikes.Add(like);
                    reply.LikeCount = (reply.LikeCount ?? 0) + 1;
                    await _context.SaveChangesAsync();

                    // Create notification for reply like
                    await _notificationService.CreateReplyLikeNotificationAsync(replyId, userId);

                    return (true, true, reply.LikeCount ?? 0, "Reply liked");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling reply like");
                return (false, false, 0, "An error occurred");
            }
        }

        public async Task<bool> IsPostLikedByUserAsync(int postId, int userId)
        {
            return await _context.UserLikes
                .AnyAsync(l => l.UserId == userId && l.ItemType == "post" && l.ItemId == postId);
        }

        public async Task<bool> IsReplyLikedByUserAsync(int replyId, int userId)
        {
            return await _context.UserLikes
                .AnyAsync(l => l.UserId == userId && l.ItemType == "reply" && l.ItemId == replyId);
        }

        #endregion

        #region Bookmark Operations

        public async Task<(bool Success, bool IsBookmarked, string Message)> ToggleBookmarkPostAsync(
            int postId, int userId, string? title = null)
        {
            try
            {
                var existingBookmark = await _context.UserBookmarks
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.ItemType == "post" && b.ItemId == postId);

                if (existingBookmark != null)
                {
                    // Remove bookmark
                    _context.UserBookmarks.Remove(existingBookmark);
                    await _context.SaveChangesAsync();
                    return (true, false, "Bookmark removed");
                }
                else
                {
                    // Add bookmark
                    var bookmark = new UserBookmark
                    {
                        UserId = userId,
                        ItemType = "post",
                        ItemId = postId,
                        BookmarkTitle = title,
                        CreatedAt = DateTime.Now
                    };
                    _context.UserBookmarks.Add(bookmark);
                    await _context.SaveChangesAsync();
                    return (true, true, "Post bookmarked");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling bookmark");
                return (false, false, "An error occurred");
            }
        }

        public async Task<bool> IsPostBookmarkedByUserAsync(int postId, int userId)
        {
            return await _context.UserBookmarks
                .AnyAsync(b => b.UserId == userId && b.ItemType == "post" && b.ItemId == postId);
        }

        public async Task<List<ForumPostViewModel>> GetUserBookmarksAsync(int userId, int page, int pageSize)
        {
            try
            {
                var bookmarkIds = await _context.UserBookmarks
                    .Where(b => b.UserId == userId && b.ItemType == "post")
                    .OrderByDescending(b => b.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => b.ItemId)
                    .ToListAsync();

                var postEntities = await _context.ForumPosts
                    .Include(p => p.User)
                    .Include(p => p.Category)
                    .Where(p => bookmarkIds.Contains(p.PostId))
                    .ToListAsync();

                var posts = postEntities.Select(p => MapToPostViewModel(p, userId)).ToList();
                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user bookmarks");
                return new List<ForumPostViewModel>();
            }
        }

        #endregion

        #region Moderation Operations

        public async Task<(bool Success, string Message)> ToggleStickyAsync(int postId, int userId)
        {
            try
            {
                if (!await CanUserModerateAsync(userId))
                {
                    return (false, "You don't have permission to perform this action");
                }

                var post = await _context.ForumPosts.FindAsync(postId);
                if (post == null)
                {
                    return (false, "Post not found");
                }

                post.IsSticky = !(post.IsSticky ?? false);
                await _context.SaveChangesAsync();

                return (true, post.IsSticky == true ? "Post pinned" : "Post unpinned");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling sticky");
                return (false, "An error occurred");
            }
        }

        public async Task<(bool Success, string Message)> ToggleLockAsync(int postId, int userId)
        {
            try
            {
                if (!await CanUserModerateAsync(userId))
                {
                    return (false, "You don't have permission to perform this action");
                }

                var post = await _context.ForumPosts.FindAsync(postId);
                if (post == null)
                {
                    return (false, "Post not found");
                }

                post.IsLocked = !(post.IsLocked ?? false);
                await _context.SaveChangesAsync();

                return (true, post.IsLocked == true ? "Post locked" : "Post unlocked");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling lock");
                return (false, "An error occurred");
            }
        }

        public async Task<(bool Success, string Message)> ApprovePostAsync(int postId, int userId)
        {
            try
            {
                if (!await CanUserModerateAsync(userId))
                {
                    return (false, "You don't have permission to perform this action");
                }

                var post = await _context.ForumPosts.FindAsync(postId);
                if (post == null)
                {
                    return (false, "Post not found");
                }

                post.IsApproved = true;
                await _context.SaveChangesAsync();

                return (true, "Post approved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving post");
                return (false, "An error occurred");
            }
        }

        public async Task<(bool Success, string Message)> ApproveReplyAsync(int replyId, int userId)
        {
            try
            {
                if (!await CanUserModerateAsync(userId))
                {
                    return (false, "You don't have permission to perform this action");
                }

                var reply = await _context.ForumReplies.FindAsync(replyId);
                if (reply == null)
                {
                    return (false, "Reply not found");
                }

                reply.IsApproved = true;
                await _context.SaveChangesAsync();

                return (true, "Reply approved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving reply");
                return (false, "An error occurred");
            }
        }

        #endregion

        #region Search Operations

        public async Task<ForumSearchViewModel> SearchAsync(ForumSearchViewModel searchModel)
        {
            try
            {
                var query = _context.ForumPosts
                    .Include(p => p.User)
                    .Include(p => p.Category)
                    .Where(p => p.IsApproved == true);

                // Apply search query
                if (!string.IsNullOrWhiteSpace(searchModel.Query))
                {
                    query = searchModel.SearchIn switch
                    {
                        "titles" => query.Where(p => p.Title.Contains(searchModel.Query)),
                        "content" => query.Where(p => p.Content.Contains(searchModel.Query)),
                        _ => query.Where(p => p.Title.Contains(searchModel.Query) || p.Content.Contains(searchModel.Query))
                    };
                }

                // Apply category filter
                if (searchModel.CategoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == searchModel.CategoryId.Value);
                }

                // Apply author filter
                if (!string.IsNullOrWhiteSpace(searchModel.Author))
                {
                    query = query.Where(p => p.User.Username.Contains(searchModel.Author));
                }

                // Apply date filters
                if (searchModel.DateFrom.HasValue)
                {
                    query = query.Where(p => p.CreatedAt >= searchModel.DateFrom.Value);
                }
                if (searchModel.DateTo.HasValue)
                {
                    query = query.Where(p => p.CreatedAt <= searchModel.DateTo.Value);
                }

                // Apply sorting
                query = searchModel.SortBy switch
                {
                    "date" => query.OrderByDescending(p => p.CreatedAt),
                    "replies" => query.OrderByDescending(p => p.ReplyCount),
                    "likes" => query.OrderByDescending(p => p.LikeCount),
                    _ => query.OrderByDescending(p => p.CreatedAt) // relevance default to date
                };

                var totalResults = await query.CountAsync();
                var resultEntities = await query
                    .Skip((searchModel.Pagination.CurrentPage - 1) * searchModel.Pagination.PageSize)
                    .Take(searchModel.Pagination.PageSize)
                    .ToListAsync();

                var results = resultEntities.Select(p => MapToPostViewModel(p, null)).ToList();
                searchModel.Results = results;
                searchModel.TotalResults = totalResults;
                searchModel.Pagination.TotalItems = totalResults;

                return searchModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching forum");
                return searchModel;
            }
        }

        #endregion

        #region User Activity

        public async Task<UserForumActivityViewModel> GetUserActivityAsync(int userId, int page, int pageSize, string activeTab)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return new UserForumActivityViewModel();
                }

                var viewModel = new UserForumActivityViewModel
                {
                    UserId = userId,
                    Username = user.Username,
                    AvatarUrl = user.AvatarUrl,
                    MemberSince = user.CreatedAt,
                    LastActive = user.LastLoginAt,
                    ActiveTab = activeTab
                };

                // Get statistics
                viewModel.TotalPosts = await _context.ForumPosts.CountAsync(p => p.UserId == userId);
                viewModel.TotalReplies = await _context.ForumReplies.CountAsync(r => r.UserId == userId);
                viewModel.TotalLikesReceived = await _context.UserLikes
                    .Where(l => (l.ItemType == "post" && _context.ForumPosts.Any(p => p.PostId == l.ItemId && p.UserId == userId)) ||
                                (l.ItemType == "reply" && _context.ForumReplies.Any(r => r.ReplyId == l.ItemId && r.UserId == userId)))
                    .CountAsync();
                viewModel.TotalLikesGiven = await _context.UserLikes.CountAsync(l => l.UserId == userId);

                // Get activity based on active tab
                switch (activeTab)
                {
                    case "posts":
                        var postEntities = await _context.ForumPosts
                            .Include(p => p.User)
                            .Include(p => p.Category)
                            .Where(p => p.UserId == userId)
                            .OrderByDescending(p => p.CreatedAt)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();
                        viewModel.RecentPosts = postEntities.Select(p => MapToPostViewModel(p, null)).ToList();
                        viewModel.Pagination.TotalItems = viewModel.TotalPosts;
                        break;

                    case "replies":
                        var replies = await _context.ForumReplies
                            .Include(r => r.User)
                            .Include(r => r.Post)
                                .ThenInclude(p => p.Category)
                            .Where(r => r.UserId == userId)
                            .OrderByDescending(r => r.CreatedAt)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();
                        viewModel.RecentReplies = replies.Select(r => MapToReplyViewModel(r, null)).ToList();
                        viewModel.Pagination.TotalItems = viewModel.TotalReplies;
                        break;

                    case "bookmarks":
                        viewModel.BookmarkedPosts = await GetUserBookmarksAsync(userId, page, pageSize);
                        viewModel.Pagination.TotalItems = await _context.UserBookmarks
                            .CountAsync(b => b.UserId == userId && b.ItemType == "post");
                        break;
                }

                viewModel.Pagination.CurrentPage = page;
                viewModel.Pagination.PageSize = pageSize;

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activity");
                return new UserForumActivityViewModel();
            }
        }

        #endregion

        #region Statistics

        public async Task<ForumStatisticsViewModel> GetStatisticsAsync()
        {
            try
            {
                var stats = new ForumStatisticsViewModel
                {
                    TotalPosts = await _context.ForumPosts.CountAsync(),
                    TotalReplies = await _context.ForumReplies.CountAsync(),
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalCategories = await _context.ForumCategories.CountAsync(c => c.IsActive == true)
                };

                var newestUser = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .FirstOrDefaultAsync();

                if (newestUser != null)
                {
                    stats.NewestMember = newestUser.Username;
                    stats.NewestMemberJoinDate = newestUser.CreatedAt;
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return new ForumStatisticsViewModel();
            }
        }

        #endregion

        #region Helper Methods

        public async Task<bool> CanUserEditPostAsync(int postId, int userId)
        {
            var post = await _context.ForumPosts.FindAsync(postId);
            if (post == null) return false;

            // User can edit their own post or if they are a moderator/admin
            return post.UserId == userId || await CanUserModerateAsync(userId);
        }

        private async Task<bool> CanUserEditReplyAsync(int replyId, int userId)
        {
            var reply = await _context.ForumReplies.FindAsync(replyId);
            if (reply == null) return false;

            // User can edit their own reply or if they are a moderator/admin
            return reply.UserId == userId || await CanUserModerateAsync(userId);
        }

        public async Task<bool> CanUserDeletePostAsync(int postId, int userId)
        {
            var post = await _context.ForumPosts.FindAsync(postId);
            if (post == null) return false;

            // User can delete their own post or if they are a moderator/admin
            return post.UserId == userId || await CanUserModerateAsync(userId);
        }

        public async Task<bool> CanUserModerateAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return false;

            // Check if user is admin or moderator
            return user.Role?.RoleName == "Admin" || user.Role?.RoleName == "Moderator";
        }

        private async Task UpdateCategoryPostCountAsync(short categoryId)
        {
            try
            {
                var category = await _context.ForumCategories.FindAsync(categoryId);
                if (category != null)
                {
                    category.PostCount = await _context.ForumPosts.CountAsync(p => p.CategoryId == categoryId);

                    var lastPost = await _context.ForumPosts
                        .Where(p => p.CategoryId == categoryId)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (lastPost != null)
                    {
                        category.LastPostAt = lastPost.CreatedAt;
                        category.LastPostBy = lastPost.UserId;
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category post count");
            }
        }

        private static ForumPostViewModel MapToPostViewModel(ForumPost post, int? currentUserId)
        {
            var viewModel = new ForumPostViewModel
            {
                PostId = post.PostId,
                CategoryId = post.CategoryId,
                CategoryName = post.Category?.CategoryName,
                UserId = post.UserId,
                Username = post.User?.Username,
                UserAvatarUrl = post.User?.AvatarUrl,
                Title = post.Title,
                Content = post.Content,
                IsSticky = post.IsSticky ?? false,
                IsLocked = post.IsLocked ?? false,
                ViewCount = post.ViewCount ?? 0,
                ReplyCount = post.ReplyCount ?? 0,
                LikeCount = post.LikeCount ?? 0,
                IsApproved = post.IsApproved ?? false,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                UpdatedByUsername = post.UpdatedByNavigation?.Username
            };

            // Map attachments if available
            if (post.ForumAttachments != null && post.ForumAttachments.Any())
            {
                viewModel.Attachments = post.ForumAttachments.Select(a => new ForumAttachmentViewModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    OriginalFileName = a.OriginalFileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    MimeType = a.MimeType,
                    FileSize = a.FileSize,
                    FileSizeFormatted = GetFileSizeString(a.FileSize),
                    CreatedAt = a.CreatedAt
                }).ToList();
            }

            return viewModel;
        }

        private static ForumReplyViewModel MapToReplyViewModel(ForumReply reply, int? currentUserId)
        {
            var viewModel = new ForumReplyViewModel
            {
                ReplyId = reply.ReplyId,
                PostId = reply.PostId,
                UserId = reply.UserId,
                Username = reply.User?.Username,
                UserAvatarUrl = reply.User?.AvatarUrl,
                ParentReplyId = reply.ParentReplyId,
                ParentReplyUsername = reply.ParentReply?.User?.Username,
                Content = reply.Content,
                LikeCount = reply.LikeCount ?? 0,
                IsApproved = reply.IsApproved ?? false,
                CreatedAt = reply.CreatedAt,
                UpdatedAt = reply.UpdatedAt,
                UpdatedByUsername = reply.UpdatedByNavigation?.Username
            };

            // Map attachments if available
            if (reply.ForumAttachments != null && reply.ForumAttachments.Any())
            {
                viewModel.Attachments = reply.ForumAttachments.Select(a => new ForumAttachmentViewModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    OriginalFileName = a.OriginalFileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    MimeType = a.MimeType,
                    FileSize = a.FileSize,
                    FileSizeFormatted = GetFileSizeString(a.FileSize),
                    CreatedAt = a.CreatedAt
                }).ToList();
            }

            return viewModel;
        }

        private static string GetFileSizeString(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private async Task SetReplyPermissionsAsync(ForumReplyViewModel reply, int currentUserId)
        {
            // Set permissions for current reply
            reply.IsLikedByCurrentUser = await IsReplyLikedByUserAsync(reply.ReplyId, currentUserId);
            reply.CanEdit = reply.UserId == currentUserId || await CanUserModerateAsync(currentUserId);
            reply.CanDelete = reply.UserId == currentUserId || await CanUserModerateAsync(currentUserId);

            // Recursively set permissions for child replies
            foreach (var child in reply.ChildReplies)
            {
                await SetReplyPermissionsAsync(child, currentUserId);
            }
        }

        private static void BuildReplyHierarchy(ForumReplyViewModel parent, List<ForumReply> allReplies, int? currentUserId, int level = 0)
        {
            parent.Level = level;
            var childReplies = allReplies.Where(r => r.ParentReplyId == parent.ReplyId).ToList();

            foreach (var child in childReplies)
            {
                var childViewModel = MapToReplyViewModel(child, currentUserId);
                BuildReplyHierarchy(childViewModel, allReplies, currentUserId, level + 1);
                parent.ChildReplies.Add(childViewModel);
            }
        }

        #endregion

        #region Attachment Operations

        public async Task<(bool Success, string Message)> DeleteAttachmentAsync(int attachmentId, int userId)
        {
            try
            {
                var attachment = await _context.ForumAttachments
                    .Include(a => a.Post)
                    .Include(a => a.Reply)
                    .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

                if (attachment == null)
                {
                    return (false, "Attachment not found");
                }

                // Check permission - user must be the uploader or have permission to edit the post/reply
                bool hasPermission = false;

                if (attachment.PostId.HasValue)
                {
                    hasPermission = await CanUserEditPostAsync(attachment.PostId.Value, userId);
                }
                else if (attachment.ReplyId.HasValue)
                {
                    hasPermission = await CanUserEditReplyAsync(attachment.ReplyId.Value, userId);
                }

                if (!hasPermission)
                {
                    return (false, "You don't have permission to delete this attachment");
                }

                // Delete physical file
                await _fileUploadService.DeleteFileAsync(attachment.FilePath);

                // Delete database record
                _context.ForumAttachments.Remove(attachment);
                await _context.SaveChangesAsync();

                return (true, "Attachment deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment");
                return (false, "An error occurred while deleting the attachment");
            }
        }
        
        #endregion
    }
}
