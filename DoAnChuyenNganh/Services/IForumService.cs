using DoAnChuyenNganh.Models;
using DoAnChuyenNganh.ViewModels.Forum;

namespace DoAnChuyenNganh.Services
{
    public interface IForumService
    {
        // Category operations
        Task<List<ForumCategoryViewModel>> GetAllCategoriesAsync();
        Task<ForumCategoryViewModel?> GetCategoryByIdAsync(short categoryId);
        Task<ForumCategoryDetailViewModel> GetCategoryDetailAsync(short categoryId, int page, int pageSize, string sortBy, string filterBy);
        Task<(bool Success, string Message)> CreateCategoryAsync(ForumCategoryViewModel model, int userId);
        Task<(bool Success, string Message)> UpdateCategoryAsync(ForumCategoryViewModel model, int userId);
        Task<(bool Success, string Message)> DeleteCategoryAsync(short categoryId);

        // Post operations
        Task<ForumPostViewModel?> GetPostByIdAsync(int postId, int? currentUserId = null);
        Task<ForumPostDetailViewModel> GetPostDetailAsync(int postId, int page, int pageSize, int? currentUserId = null);
        Task<List<ForumPostViewModel>> GetPostsByCategoryAsync(short categoryId, int page, int pageSize, string sortBy, string filterBy);
        Task<List<ForumPostViewModel>> GetRecentPostsAsync(int count = 10);
        Task<List<ForumPostViewModel>> GetPopularPostsAsync(int count = 10);
        Task<List<ForumPostViewModel>> GetTrendingPostsAsync(int count = 10);
        Task<(bool Success, int? PostId, string Message)> CreatePostAsync(ForumPostViewModel model, int userId);
        Task<(bool Success, string Message)> UpdatePostAsync(ForumPostViewModel model, int userId);
        Task<(bool Success, string Message)> DeletePostAsync(int postId, int userId);
        Task IncrementViewCountAsync(int postId);

        // Reply operations
        Task<ForumReplyViewModel?> GetReplyByIdAsync(int replyId, int? currentUserId = null);
        Task<List<ForumReplyViewModel>> GetRepliesByPostIdAsync(int postId, int page, int pageSize, int? currentUserId = null);
        Task<(bool Success, int? ReplyId, string Message)> CreateReplyAsync(ForumReplyViewModel model, int userId);
        Task<(bool Success, string Message)> UpdateReplyAsync(ForumReplyViewModel model, int userId);
        Task<(bool Success, string Message)> DeleteReplyAsync(int replyId, int userId);

        // Like operations
        Task<(bool Success, bool IsLiked, int NewCount, string Message)> ToggleLikePostAsync(int postId, int userId);
        Task<(bool Success, bool IsLiked, int NewCount, string Message)> ToggleLikeReplyAsync(int replyId, int userId);
        Task<bool> IsPostLikedByUserAsync(int postId, int userId);
        Task<bool> IsReplyLikedByUserAsync(int replyId, int userId);

        // Bookmark operations
        Task<(bool Success, bool IsBookmarked, string Message)> ToggleBookmarkPostAsync(int postId, int userId, string? title = null);
        Task<bool> IsPostBookmarkedByUserAsync(int postId, int userId);
        Task<List<ForumPostViewModel>> GetUserBookmarksAsync(int userId, int page, int pageSize);

        // Moderation operations
        Task<(bool Success, string Message)> ToggleStickyAsync(int postId, int userId);
        Task<(bool Success, string Message)> ToggleLockAsync(int postId, int userId);
        Task<(bool Success, string Message)> ApprovePostAsync(int postId, int userId);
        Task<(bool Success, string Message)> ApproveReplyAsync(int replyId, int userId);

        // Search operations
        Task<ForumSearchViewModel> SearchAsync(ForumSearchViewModel searchModel);

        // User activity
        Task<UserForumActivityViewModel> GetUserActivityAsync(int userId, int page, int pageSize, string activeTab);

        // Statistics
        Task<ForumStatisticsViewModel> GetStatisticsAsync();

        // Helper methods
        Task<bool> CanUserEditPostAsync(int postId, int userId);
        Task<bool> CanUserDeletePostAsync(int postId, int userId);
        Task<bool> CanUserModerateAsync(int userId);
    }
}

