using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class UserLike
{
    public int LikeId { get; set; }

    public int UserId { get; set; }

    public string ItemType { get; set; } = null!;

    public int ItemId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
