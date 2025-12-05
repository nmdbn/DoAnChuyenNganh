using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class NewsArticle
{
    public int ArticleId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? Excerpt { get; set; }

    public string? FeaturedImageUrl { get; set; }

    public int CategoryId { get; set; }

    public int AuthorId { get; set; }

    public bool? IsPublished { get; set; }

    public bool? IsFeatured { get; set; }

    public int? ViewCount { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public string? Tags { get; set; }

    public virtual User Author { get; set; } = null!;

    public virtual NewsCategory Category { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}

