using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class SystemStat
{
    public int StatId { get; set; }

    public string StatName { get; set; } = null!;

    public long StatValue { get; set; }

    public DateTime? LastUpdated { get; set; }

    public string? Description { get; set; }
}
