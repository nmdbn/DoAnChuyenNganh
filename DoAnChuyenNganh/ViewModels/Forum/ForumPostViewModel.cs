using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DoAnChuyenNganh.ViewModels.Forum
{
    public class ForumPostViewModel
    {
        public int PostId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public short CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public int UserId { get; set; }

        [Display(Name = "Author")]
        public string? Username { get; set; }

        public string? UserAvatarUrl { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(250, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 250 characters")]
        [Display(Name = "Title")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Content is required")]
        [StringLength(10000, MinimumLength = 10, ErrorMessage = "Content must be at least 10 characters")]
        [Display(Name = "Content")]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = null!;

        [Display(Name = "Sticky")]
        public bool IsSticky { get; set; }

        [Display(Name = "Locked")]
        public bool IsLocked { get; set; }

        [Display(Name = "Views")]
        public int ViewCount { get; set; }

        [Display(Name = "Replies")]
        public int ReplyCount { get; set; }

        [Display(Name = "Likes")]
        public int LikeCount { get; set; }

        [Display(Name = "Approved")]
        public bool IsApproved { get; set; }

        [Display(Name = "Created At")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Updated By")]
        public string? UpdatedByUsername { get; set; }

        // Additional properties for display
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsBookmarkedByCurrentUser { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanModerate { get; set; }
        public string? LastReplyUsername { get; set; }
        public DateTime? LastReplyAt { get; set; }
        public List<string> Tags { get; set; } = new List<string>();

        // File upload properties
        [Display(Name = "Upload Images")]
        public List<IFormFile>? UploadedImages { get; set; }

        [Display(Name = "Upload Files")]
        public List<IFormFile>? UploadedFiles { get; set; }

        // Attachments
        public List<ForumAttachmentViewModel> Attachments { get; set; } = new List<ForumAttachmentViewModel>();
    }
}

