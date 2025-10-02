using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class ForumCategory
{
    public short CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public short? SortOrder { get; set; }

    public int? PostCount { get; set; }

    public DateTime? LastPostAt { get; set; }

    public int? LastPostBy { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<ForumPost> ForumPosts { get; set; } = new List<ForumPost>();

    public virtual User? LastPostByNavigation { get; set; }
}
