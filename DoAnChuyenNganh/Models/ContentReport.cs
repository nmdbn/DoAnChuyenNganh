using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class ContentReport
{
    public int ReportId { get; set; }

    public int ReporterId { get; set; }

    public byte CategoryId { get; set; }

    public string ItemType { get; set; } = null!;

    public int ItemId { get; set; }

    public int? ReportedUserId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Status { get; set; }

    public string? AdminNotes { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ReportCategory Category { get; set; } = null!;

    public virtual User? ReportedUser { get; set; }

    public virtual User Reporter { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }
}
