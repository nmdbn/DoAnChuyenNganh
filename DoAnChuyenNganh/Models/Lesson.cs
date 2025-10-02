using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class Lesson
{
    public int LessonId { get; set; }

    public int CourseId { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public short LessonOrder { get; set; }

    public string? LessonType { get; set; }

    public string? VideoUrl { get; set; }

    public int? VideoDuration { get; set; }

    public bool? IsPreviewable { get; set; }

    public bool? IsPublished { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<CourseMaterial> CourseMaterials { get; set; } = new List<CourseMaterial>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();

    public virtual User? UpdatedByNavigation { get; set; }
}
