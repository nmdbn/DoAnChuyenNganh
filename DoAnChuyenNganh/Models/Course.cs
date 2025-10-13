using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public string Title { get; set; } = null!;

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public short SubjectId { get; set; }

    public short GradeId { get; set; }

    public int InstructorId { get; set; }

    public string? ThumbnailUrl { get; set; }

    public byte? DifficultyLevel { get; set; }

    public decimal? EstimatedHours { get; set; }

    public decimal? Price { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public int? ViewCount { get; set; }

    public int? EnrollmentCount { get; set; }

    public decimal? AverageRating { get; set; }

    public int? RatingCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public int CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ICollection<CourseMaterial> CourseMaterials { get; set; } = new List<CourseMaterial>();

    public virtual ICollection<CourseRating> CourseRatings { get; set; } = new List<CourseRating>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual Grade Grade { get; set; } = null!;

    public virtual User Instructor { get; set; } = null!;

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
