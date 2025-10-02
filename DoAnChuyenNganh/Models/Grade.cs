using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class Grade
{
    public short GradeId { get; set; }

    public string GradeName { get; set; } = null!;

    public byte GradeLevel { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
