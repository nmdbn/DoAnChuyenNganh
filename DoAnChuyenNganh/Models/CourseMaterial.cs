using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class CourseMaterial
{
    public int MaterialId { get; set; }

    public int? CourseId { get; set; }

    public int? LessonId { get; set; }

    public string MaterialName { get; set; } = null!;

    public string MaterialType { get; set; } = null!;

    public string? FileUrl { get; set; }

    public long? FileSize { get; set; }

    public int? DownloadCount { get; set; }

    public bool? IsPublic { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public virtual Course? Course { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Lesson? Lesson { get; set; }
}
