using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class UserBookmark
{
    public int BookmarkId { get; set; }

    public int UserId { get; set; }

    public string ItemType { get; set; } = null!;

    public int ItemId { get; set; }

    public string? BookmarkTitle { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
