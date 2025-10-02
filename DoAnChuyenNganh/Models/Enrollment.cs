using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class Enrollment
{
    public int EnrollmentId { get; set; }

    public int UserId { get; set; }

    public int CourseId { get; set; }

    public DateTime? EnrolledAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public decimal? ProgressPercentage { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public int? TotalTimeSpent { get; set; }

    public bool? IsActive { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();

    public virtual User User { get; set; } = null!;
}
