using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class VwForumActivity
{
    public int PostId { get; set; }

    public string Title { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string AuthorUsername { get; set; } = null!;

    public string AuthorName { get; set; } = null!;

    public int? ViewCount { get; set; }

    public int? ReplyCount { get; set; }

    public int? LikeCount { get; set; }

    public bool? IsSticky { get; set; }

    public bool? IsLocked { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
