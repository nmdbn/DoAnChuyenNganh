using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class CourseRating
{
    public int RatingId { get; set; }

    public int CourseId { get; set; }

    public int UserId { get; set; }

    public byte Rating { get; set; }

    public string? Review { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
