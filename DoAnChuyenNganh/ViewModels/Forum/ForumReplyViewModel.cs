using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DoAnChuyenNganh.ViewModels.Forum
{
    public class ForumReplyViewModel
    {
        public int ReplyId { get; set; }

        [Required]
        public int PostId { get; set; }

        public int UserId { get; set; }

        [Display(Name = "Author")]
        public string? Username { get; set; }

        public string? UserAvatarUrl { get; set; }

        public int? ParentReplyId { get; set; }

        public string? ParentReplyUsername { get; set; }

        [Required(ErrorMessage = "Reply content is required")]
        [StringLength(5000, MinimumLength = 1, ErrorMessage = "Reply must be between 1 and 5000 characters")]
        [Display(Name = "Reply")]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = null!;

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
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public List<ForumReplyViewModel> ChildReplies { get; set; } = new List<ForumReplyViewModel>();
        public int Level { get; set; } // For nested reply display

        // File upload properties
        [Display(Name = "Upload Images")]
        public List<IFormFile>? UploadedImages { get; set; }

        [Display(Name = "Upload Files")]
        public List<IFormFile>? UploadedFiles { get; set; }

        // Attachments
        public List<ForumAttachmentViewModel> Attachments { get; set; } = new List<ForumAttachmentViewModel>();
    }
}

