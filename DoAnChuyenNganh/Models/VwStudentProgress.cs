using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class VwStudentProgress
{
    public int EnrollmentId { get; set; }

    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string StudentName { get; set; } = null!;

    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = null!;

    public DateTime? EnrolledAt { get; set; }

    public decimal? ProgressPercentage { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public int? TotalTimeSpent { get; set; }

    public int? LessonsAccessed { get; set; }

    public int? LessonsCompleted { get; set; }

    public int? TotalLessons { get; set; }
}
