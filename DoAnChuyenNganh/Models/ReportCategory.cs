using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class ReportCategory
{
    public byte CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public byte? SeverityLevel { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ContentReport> ContentReports { get; set; } = new List<ContentReport>();
}
