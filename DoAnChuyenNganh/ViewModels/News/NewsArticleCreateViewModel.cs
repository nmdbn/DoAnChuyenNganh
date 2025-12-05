using System.ComponentModel.DataAnnotations;

namespace DoAnChuyenNganh.ViewModels.News
{
    public class NewsArticleCreateViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(300, ErrorMessage = "Title cannot exceed 300 characters")]
        public string Title { get; set; } = null!;

        [StringLength(350, ErrorMessage = "Slug cannot exceed 350 characters")]
        public string? Slug { get; set; }

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Excerpt cannot exceed 500 characters")]
        public string? Excerpt { get; set; }

        public string? FeaturedImageUrl { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public bool IsPublished { get; set; }

        public bool IsFeatured { get; set; }

        [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters")]
        public string? Tags { get; set; }
    }
}

