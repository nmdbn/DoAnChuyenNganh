using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class Subject
{
    public short SubjectId { get; set; }

    public string SubjectName { get; set; } = null!;

    public string? SubjectCode { get; set; }

    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
