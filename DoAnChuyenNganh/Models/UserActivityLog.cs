using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class UserActivityLog
{
    public long LogId { get; set; }

    public int? UserId { get; set; }

    public string ActivityType { get; set; } = null!;

    public string? ItemType { get; set; }

    public int? ItemId { get; set; }

    public string? Ipaddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
