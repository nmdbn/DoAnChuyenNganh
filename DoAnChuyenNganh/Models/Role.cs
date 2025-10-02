using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class Role
{
    public byte RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public byte PermissionLevel { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
