using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.ViewModels.Forum
{
    public class ForumCategoryViewModel
    {
        public short CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = null!;

        [StringLength(300, ErrorMessage = "Description cannot exceed 300 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Sort Order")]
        public short? SortOrder { get; set; }

        [Display(Name = "Post Count")]
        public int PostCount { get; set; }

        [Display(Name = "Last Post At")]
        public DateTime? LastPostAt { get; set; }

        [Display(Name = "Last Post By")]
        public string? LastPostByUsername { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Created At")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Created By")]
        public string? CreatedByUsername { get; set; }

        // Additional properties for display
        public string? LastPostTitle { get; set; }
        public int? LastPostId { get; set; }
    }
}

