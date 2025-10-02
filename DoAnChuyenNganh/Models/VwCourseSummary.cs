using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class VwCourseSummary
{
    public int CourseId { get; set; }

    public string Title { get; set; } = null!;

    public string? Slug { get; set; }

    public string? SubjectName { get; set; }

    public string? GradeName { get; set; }

    public string? InstructorName { get; set; }

    public byte? DifficultyLevel { get; set; }

    public decimal? EstimatedHours { get; set; }

    public decimal? Price { get; set; }

    public bool? IsPublished { get; set; }

    public bool? IsFeatured { get; set; }

    public int? ViewCount { get; set; }

    public int? EnrollmentCount { get; set; }

    public decimal? AverageRating { get; set; }

    public int? RatingCount { get; set; }

    public int? TotalLessons { get; set; }

    public int? TotalMaterials { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }
}
