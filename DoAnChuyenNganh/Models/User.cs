using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string PasswordSalt { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public byte RoleId { get; set; }

    public bool? IsEmailVerified { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsLocked { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public byte? LoginAttempts { get; set; }

    public DateTime? LockoutUntil { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ICollection<ContentReport> ContentReportReportedUsers { get; set; } = new List<ContentReport>();

    public virtual ICollection<ContentReport> ContentReportReporters { get; set; } = new List<ContentReport>();

    public virtual ICollection<ContentReport> ContentReportReviewedByNavigations { get; set; } = new List<ContentReport>();

    public virtual ICollection<Course> CourseCreatedByNavigations { get; set; } = new List<Course>();

    public virtual ICollection<Course> CourseInstructors { get; set; } = new List<Course>();

    public virtual ICollection<CourseMaterial> CourseMaterials { get; set; } = new List<CourseMaterial>();

    public virtual ICollection<CourseRating> CourseRatings { get; set; } = new List<CourseRating>();

    public virtual ICollection<Course> CourseUpdatedByNavigations { get; set; } = new List<Course>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<ForumCategory> ForumCategoryCreatedByNavigations { get; set; } = new List<ForumCategory>();

    public virtual ICollection<ForumCategory> ForumCategoryLastPostByNavigations { get; set; } = new List<ForumCategory>();

    public virtual ICollection<ForumPost> ForumPostUpdatedByNavigations { get; set; } = new List<ForumPost>();

    public virtual ICollection<ForumPost> ForumPostUsers { get; set; } = new List<ForumPost>();

    public virtual ICollection<ForumReply> ForumReplyUpdatedByNavigations { get; set; } = new List<ForumReply>();

    public virtual ICollection<ForumReply> ForumReplyUsers { get; set; } = new List<ForumReply>();

    public virtual ICollection<User> InverseCreatedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<User> InverseUpdatedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<Lesson> LessonCreatedByNavigations { get; set; } = new List<Lesson>();

    public virtual ICollection<Lesson> LessonUpdatedByNavigations { get; set; } = new List<Lesson>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PrivateMessage> PrivateMessageDeletedByNavigations { get; set; } = new List<PrivateMessage>();

    public virtual ICollection<PrivateMessage> PrivateMessageRecipients { get; set; } = new List<PrivateMessage>();

    public virtual ICollection<PrivateMessage> PrivateMessageSenders { get; set; } = new List<PrivateMessage>();

    public virtual Role Role { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual ICollection<UserActivityLog> UserActivityLogs { get; set; } = new List<UserActivityLog>();

    public virtual ICollection<UserBookmark> UserBookmarks { get; set; } = new List<UserBookmark>();

    public virtual ICollection<UserLike> UserLikes { get; set; } = new List<UserLike>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
