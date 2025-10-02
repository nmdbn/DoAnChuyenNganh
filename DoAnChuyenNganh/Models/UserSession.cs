using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class UserSession
{
    public int SessionId { get; set; }

    public int UserId { get; set; }

    public string SessionToken { get; set; } = null!;

    public string? Ipaddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
