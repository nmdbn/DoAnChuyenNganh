using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DoAnChuyenNganh.Models;

public partial class DoAnChuyenNganhContext : DbContext
{
    public DoAnChuyenNganhContext()
    {
    }

    public DoAnChuyenNganhContext(DbContextOptions<DoAnChuyenNganhContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChatAttachment> ChatAttachments { get; set; }

    public virtual DbSet<ContentReport> ContentReports { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseMaterial> CourseMaterials { get; set; }

    public virtual DbSet<CourseRating> CourseRatings { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<ForumAttachment> ForumAttachments { get; set; }

    public virtual DbSet<ForumCategory> ForumCategories { get; set; }

    public virtual DbSet<ForumPost> ForumPosts { get; set; }

    public virtual DbSet<ForumReply> ForumReplies { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<LessonProgress> LessonProgresses { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PrivateMessage> PrivateMessages { get; set; }

    public virtual DbSet<ReportCategory> ReportCategories { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<SystemStat> SystemStats { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserActivityLog> UserActivityLogs { get; set; }

    public virtual DbSet<UserBookmark> UserBookmarks { get; set; }

    public virtual DbSet<UserLike> UserLikes { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<VwCourseSummary> VwCourseSummaries { get; set; }

    public virtual DbSet<VwForumActivity> VwForumActivities { get; set; }

    public virtual DbSet<VwStudentProgress> VwStudentProgresses { get; set; }

    public virtual DbSet<Payment> Payments { get; set; } = null!;
    public virtual DbSet<Quiz> Quizzes { get; set; }
    public virtual DbSet<QuizQuestion> QuizQuestions { get; set; }
    public virtual DbSet<QuizAnswer> QuizAnswers { get; set; }
    public virtual DbSet<UserQuizAttempt> UserQuizAttempts { get; set; }
    public virtual DbSet<UserQuizAnswer> UserQuizAnswers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // =======================
        // FIX TÊN BẢNG QUIZ SYSTEM (vì DB không pluralize)
        // =======================
        modelBuilder.Entity<Quiz>().ToTable("Quiz");
        modelBuilder.Entity<QuizQuestion>().ToTable("QuizQuestion");
        modelBuilder.Entity<QuizAnswer>().ToTable("QuizAnswer");
        modelBuilder.Entity<UserQuizAttempt>().ToTable("UserQuizAttempt");
        modelBuilder.Entity<UserQuizAnswer>().ToTable("UserQuizAnswer");
        modelBuilder.Entity<Quiz>().ToTable("Quiz");
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.Entity<ContentReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__ContentR__D5BD48E525467EF8");

            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.AdminNotes).HasMaxLength(1000);
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.ReportedUserId).HasColumnName("ReportedUserID");
            entity.Property(e => e.ReporterId).HasColumnName("ReporterID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending");

            entity.HasOne(d => d.Category).WithMany(p => p.ContentReports)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContentReports_Category");

            entity.HasOne(d => d.ReportedUser).WithMany(p => p.ContentReportReportedUsers)
                .HasForeignKey(d => d.ReportedUserId)
                .HasConstraintName("FK_ContentReports_ReportedUser");

            entity.HasOne(d => d.Reporter).WithMany(p => p.ContentReportReporters)
                .HasForeignKey(d => d.ReporterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContentReports_Reporter");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ContentReportReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_ContentReports_ReviewedBy");
        });

        modelBuilder.Entity<ChatAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("PK__ChatAtta__442C64BE");

            entity.HasIndex(e => e.MessageId, "IX_ChatAttachments_MessageID");

            entity.HasIndex(e => e.UploadedBy, "IX_ChatAttachments_UploadedBy");

            entity.Property(e => e.AttachmentId).HasColumnName("AttachmentID");
            entity.Property(e => e.MessageId).HasColumnName("MessageID");
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(10);
            entity.Property(e => e.MimeType).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Message).WithMany(p => p.ChatAttachments)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ChatAttachments_Message");

            entity.HasOne(d => d.UploadedByNavigation).WithMany()
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChatAttachments_User");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Courses__C92D7187A7867EC3");

            entity.HasIndex(e => e.CreatedAt, "IX_Courses_CreatedAt");

            entity.HasIndex(e => e.GradeId, "IX_Courses_GradeID");

            entity.HasIndex(e => e.InstructorId, "IX_Courses_InstructorID");

            entity.HasIndex(e => e.IsFeatured, "IX_Courses_IsFeatured");

            entity.HasIndex(e => e.IsPublished, "IX_Courses_IsPublished");

            entity.HasIndex(e => e.SubjectId, "IX_Courses_SubjectID");

            entity.HasIndex(e => e.Slug, "UQ__Courses__BC7B5FB6148CA239").IsUnique();

            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.AverageRating)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(3, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EnrollmentCount).HasDefaultValue(0);
            entity.Property(e => e.EstimatedHours).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.GradeId).HasColumnName("GradeID");
            entity.Property(e => e.InstructorId).HasColumnName("InstructorID");
            entity.Property(e => e.IsFeatured).HasDefaultValue(false);
            entity.Property(e => e.IsPublished).HasDefaultValue(false);
            entity.Property(e => e.Price)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.RatingCount).HasDefaultValue(0);
            entity.Property(e => e.ShortDescription).HasMaxLength(300);
            entity.Property(e => e.Slug)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.SubjectId).HasColumnName("SubjectID");
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(400);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ViewCount).HasDefaultValue(0);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CourseCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Courses_CreatedBy");

            entity.HasOne(d => d.Grade).WithMany(p => p.Courses)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Courses_Grade");

            entity.HasOne(d => d.Instructor).WithMany(p => p.CourseInstructors)
                .HasForeignKey(d => d.InstructorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Courses_Instructor");

            entity.HasOne(d => d.Subject).WithMany(p => p.Courses)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Courses_Subject");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CourseUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_Courses_UpdatedBy");
        });

        modelBuilder.Entity<CourseMaterial>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("PK__CourseMa__C5061317FA6733ED");

            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DownloadCount).HasDefaultValue(0);
            entity.Property(e => e.FileUrl).HasMaxLength(500);
            entity.Property(e => e.IsPublic).HasDefaultValue(false);
            entity.Property(e => e.LessonId).HasColumnName("LessonID");
            entity.Property(e => e.MaterialName).HasMaxLength(200);
            entity.Property(e => e.MaterialType).HasMaxLength(20);

            entity.HasOne(d => d.Course).WithMany(p => p.CourseMaterials)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK_CourseMaterials_Course");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CourseMaterials)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseMaterials_CreatedBy");

            entity.HasOne(d => d.Lesson).WithMany(p => p.CourseMaterials)
                .HasForeignKey(d => d.LessonId)
                .HasConstraintName("FK_CourseMaterials_Lesson");
        });

        modelBuilder.Entity<CourseRating>(entity =>
        {
            entity.HasKey(e => e.RatingId).HasName("PK__CourseRa__FCCDF85C0C047AA4");

            entity.HasIndex(e => new { e.CourseId, e.UserId }, "UQ_CourseRatings_CourseUser").IsUnique();

            entity.Property(e => e.RatingId).HasColumnName("RatingID");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.Review).HasMaxLength(1000);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseRatings)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseRatings_Course");

            entity.HasOne(d => d.User).WithMany(p => p.CourseRatings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CourseRatings_User");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Enrollme__7F6877FB01E3873C");

            entity.HasIndex(e => e.CourseId, "IX_Enrollments_CourseID");

            entity.HasIndex(e => e.EnrolledAt, "IX_Enrollments_EnrolledAt");

            entity.HasIndex(e => e.IsActive, "IX_Enrollments_IsActive");

            entity.HasIndex(e => e.UserId, "IX_Enrollments_UserID");

            entity.HasIndex(e => new { e.UserId, e.CourseId }, "UQ_Enrollments_UserCourse").IsUnique();

            entity.Property(e => e.EnrollmentId).HasColumnName("EnrollmentID");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.EnrolledAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ProgressPercentage)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TotalTimeSpent).HasDefaultValue(0);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Course).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_Course");

            entity.HasOne(d => d.User).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_User");
        });

        modelBuilder.Entity<ForumCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ForumCat__19093A2B41E18815");

            entity.HasIndex(e => e.CategoryName, "UQ__ForumCat__8517B2E0A07B2F13").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PostCount).HasDefaultValue(0);
            entity.Property(e => e.SortOrder).HasDefaultValue((short)0);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ForumCategoryCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ForumCategories_CreatedBy");

            entity.HasOne(d => d.LastPostByNavigation).WithMany(p => p.ForumCategoryLastPostByNavigations)
                .HasForeignKey(d => d.LastPostBy)
                .HasConstraintName("FK_ForumCategories_LastPostBy");
        });

        modelBuilder.Entity<ForumAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("PK__ForumAtt__442C64BE");

            entity.HasIndex(e => e.PostId, "IX_ForumAttachments_PostID");

            entity.HasIndex(e => e.ReplyId, "IX_ForumAttachments_ReplyID");

            entity.HasIndex(e => e.UploadedBy, "IX_ForumAttachments_UploadedBy");

            entity.HasIndex(e => e.CreatedAt, "IX_ForumAttachments_CreatedAt");

            entity.Property(e => e.AttachmentId).HasColumnName("AttachmentID");
            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.ReplyId).HasColumnName("ReplyID");
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(10);
            entity.Property(e => e.MimeType).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Post).WithMany(p => p.ForumAttachments)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ForumAttachments_Post");

            entity.HasOne(d => d.Reply).WithMany(p => p.ForumAttachments)
                .HasForeignKey(d => d.ReplyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ForumAttachments_Reply");

            entity.HasOne(d => d.UploadedByNavigation).WithMany()
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ForumAttachments_User");
        });

        modelBuilder.Entity<ForumPost>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PK__ForumPos__AA126038D22CCEE9");

            entity.HasIndex(e => e.CategoryId, "IX_ForumPosts_CategoryID");

            entity.HasIndex(e => e.CreatedAt, "IX_ForumPosts_CreatedAt");

            entity.HasIndex(e => e.IsApproved, "IX_ForumPosts_IsApproved");

            entity.HasIndex(e => e.UserId, "IX_ForumPosts_UserID");

            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.IsSticky).HasDefaultValue(false);
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.ReplyCount).HasDefaultValue(0);
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.ViewCount).HasDefaultValue(0);

            entity.HasOne(d => d.Category).WithMany(p => p.ForumPosts)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ForumPosts_Category");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ForumPostUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_ForumPosts_UpdatedBy");

            entity.HasOne(d => d.User).WithMany(p => p.ForumPostUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ForumPosts_User");
        });

        modelBuilder.Entity<ForumReply>(entity =>
        {
            entity.HasKey(e => e.ReplyId).HasName("PK__ForumRep__C25E46295CAA6A5B");

            entity.HasIndex(e => e.CreatedAt, "IX_ForumReplies_CreatedAt");

            entity.HasIndex(e => e.PostId, "IX_ForumReplies_PostID");

            entity.HasIndex(e => e.UserId, "IX_ForumReplies_UserID");

            entity.Property(e => e.ReplyId).HasColumnName("ReplyID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.LikeCount).HasDefaultValue(0);
            entity.Property(e => e.ParentReplyId).HasColumnName("ParentReplyID");
            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.ParentReply).WithMany(p => p.InverseParentReply)
                .HasForeignKey(d => d.ParentReplyId)
                .HasConstraintName("FK_ForumReplies_ParentReply");

            entity.HasOne(d => d.Post).WithMany(p => p.ForumReplies)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ForumReplies_Post");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ForumReplyUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_ForumReplies_UpdatedBy");

            entity.HasOne(d => d.User).WithMany(p => p.ForumReplyUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ForumReplies_User");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.GradeId).HasName("PK__Grades__54F87A370C89BB0B");

            entity.HasIndex(e => e.GradeName, "UQ__Grades__4AA309AAAAD6354E").IsUnique();

            entity.Property(e => e.GradeId).HasColumnName("GradeID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(150);
            entity.Property(e => e.GradeName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.LessonId).HasName("PK__Lessons__B084ACB02F1F1081");

            entity.HasIndex(e => e.CourseId, "IX_Lessons_CourseID");

            entity.HasIndex(e => e.IsPublished, "IX_Lessons_IsPublished");

            entity.HasIndex(e => e.LessonOrder, "IX_Lessons_LessonOrder");

            entity.HasIndex(e => new { e.CourseId, e.LessonOrder }, "UQ_Lessons_CourseOrder").IsUnique();

            entity.Property(e => e.LessonId).HasColumnName("LessonID");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsPreviewable).HasDefaultValue(false);
            entity.Property(e => e.IsPublished).HasDefaultValue(false);
            entity.Property(e => e.LessonType)
                .HasMaxLength(20)
                .HasDefaultValue("text");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.VideoUrl).HasMaxLength(400);

            entity.HasOne(d => d.Course).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lessons_Course");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.LessonCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lessons_CreatedBy");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.LessonUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_Lessons_UpdatedBy");
        });

        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("PK__LessonPr__BAE29C853337AEF2");

            entity.ToTable("LessonProgress");

            entity.HasIndex(e => new { e.EnrollmentId, e.LessonId }, "UQ_LessonProgress_EnrollmentLesson").IsUnique();

            entity.Property(e => e.ProgressId).HasColumnName("ProgressID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EnrollmentId).HasColumnName("EnrollmentID");
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);
            entity.Property(e => e.LastPosition).HasDefaultValue(0);
            entity.Property(e => e.LessonId).HasColumnName("LessonID");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.TimeSpent).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.LessonProgresses)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LessonProgress_Enrollment");

            entity.HasOne(d => d.Lesson).WithMany(p => p.LessonProgresses)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LessonProgress_Lesson");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E32D36C52B6");

            entity.HasIndex(e => e.CreatedAt, "IX_Notifications_CreatedAt");

            entity.HasIndex(e => e.IsRead, "IX_Notifications_IsRead");

            entity.HasIndex(e => e.UserId, "IX_Notifications_UserID");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.RelatedItemId).HasColumnName("RelatedItemID");
            entity.Property(e => e.RelatedItemType).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(30);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_User");
        });

        modelBuilder.Entity<PrivateMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__PrivateM__C87C037C1630A4CF");

            entity.Property(e => e.MessageId).HasColumnName("MessageID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.RecipientId).HasColumnName("RecipientID");
            entity.Property(e => e.SenderId).HasColumnName("SenderID");
            entity.Property(e => e.Subject).HasMaxLength(200);

            entity.HasOne(d => d.DeletedByNavigation).WithMany(p => p.PrivateMessageDeletedByNavigations)
                .HasForeignKey(d => d.DeletedBy)
                .HasConstraintName("FK_PrivateMessages_DeletedBy");

            entity.HasOne(d => d.Recipient).WithMany(p => p.PrivateMessageRecipients)
                .HasForeignKey(d => d.RecipientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrivateMessages_Recipient");

            entity.HasOne(d => d.Sender).WithMany(p => p.PrivateMessageSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrivateMessages_Sender");
        });

        modelBuilder.Entity<ReportCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ReportCa__19093A2BAA4E083D");

            entity.HasIndex(e => e.CategoryName, "UQ__ReportCa__8517B2E0D7CDF3C2").IsUnique();

            entity.Property(e => e.CategoryId)
                .ValueGeneratedOnAdd()
                .HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SeverityLevel).HasDefaultValue((byte)2);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3AB9970884");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B616081C5840F").IsUnique();

            entity.Property(e => e.RoleId)
                .ValueGeneratedOnAdd()
                .HasColumnName("RoleID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.RoleName).HasMaxLength(20);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subjects__AC1BA38819921A5A");

            entity.HasIndex(e => e.SubjectName, "UQ__Subjects__4C5A7D559153908F").IsUnique();

            entity.HasIndex(e => e.SubjectCode, "UQ__Subjects__9F7CE1A94E5C27AE").IsUnique();

            entity.Property(e => e.SubjectId).HasColumnName("SubjectID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.IconUrl).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SubjectCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.SubjectName).HasMaxLength(80);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<SystemStat>(entity =>
        {
            entity.HasKey(e => e.StatId).HasName("PK__SystemSt__3A162D1EBEB537FB");

            entity.HasIndex(e => e.StatName, "UQ__SystemSt__7CF10922D8088ACC").IsUnique();

            entity.Property(e => e.StatId).HasColumnName("StatID");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.StatName).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC5252C436");

            entity.HasIndex(e => e.CreatedAt, "IX_Users_CreatedAt");

            entity.HasIndex(e => e.Email, "IX_Users_Email");

            entity.HasIndex(e => e.IsActive, "IX_Users_IsActive");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleID");

            entity.HasIndex(e => e.Username, "IX_Users_Username");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E47F0825F4").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534322A3F24").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.AvatarUrl).HasMaxLength(400);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsEmailVerified).HasDefaultValue(false);
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.LoginAttempts).HasDefaultValue((byte)0);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PasswordSalt)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.RoleId)
                .HasDefaultValue((byte)2)
                .HasColumnName("RoleID");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InverseCreatedByNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Users_CreatedBy");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Role");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.InverseUpdatedByNavigation)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_Users_UpdatedBy");
        });

        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__UserActi__5E5499A8FB38935A");

            entity.ToTable("UserActivityLog");

            entity.HasIndex(e => e.ActivityType, "IX_UserActivityLog_ActivityType");

            entity.HasIndex(e => e.CreatedAt, "IX_UserActivityLog_CreatedAt");

            entity.HasIndex(e => e.UserId, "IX_UserActivityLog_UserID");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.ActivityType).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("IPAddress");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.UserActivityLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserActivityLog_User");
        });

        modelBuilder.Entity<UserBookmark>(entity =>
        {
            entity.HasKey(e => e.BookmarkId).HasName("PK__UserBook__541A3A915FD774EE");

            entity.HasIndex(e => new { e.UserId, e.ItemType, e.ItemId }, "UQ_UserBookmarks_UserItemType").IsUnique();

            entity.Property(e => e.BookmarkId).HasColumnName("BookmarkID");
            entity.Property(e => e.BookmarkTitle).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.UserBookmarks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserBookmarks_User");
        });

        modelBuilder.Entity<UserLike>(entity =>
        {
            entity.HasKey(e => e.LikeId).HasName("PK__UserLike__A2922CF4F93C1D7F");

            entity.HasIndex(e => new { e.UserId, e.ItemType, e.ItemId }, "UQ_UserLikes_UserItemType").IsUnique();

            entity.Property(e => e.LikeId).HasColumnName("LikeID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ItemType).HasMaxLength(10);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.UserLikes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserLikes_User");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__UserSess__C9F49270E08F9D57");

            entity.HasIndex(e => e.SessionToken, "UQ__UserSess__46BDD12468F75D4B").IsUnique();

            entity.Property(e => e.SessionId).HasColumnName("SessionID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("IPAddress");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SessionToken)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserSessions_User");
        });

        modelBuilder.Entity<VwCourseSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CourseSummary");

            entity.Property(e => e.AverageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.EstimatedHours).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.GradeName).HasMaxLength(50);
            entity.Property(e => e.InstructorName).HasMaxLength(101);
            entity.Property(e => e.Price).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.Slug)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.SubjectName).HasMaxLength(80);
            entity.Property(e => e.Title).HasMaxLength(200);
        });

        modelBuilder.Entity<VwForumActivity>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ForumActivity");

            entity.Property(e => e.AuthorName).HasMaxLength(101);
            entity.Property(e => e.AuthorUsername).HasMaxLength(50);
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.Title).HasMaxLength(250);
        });

        modelBuilder.Entity<VwStudentProgress>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StudentProgress");

            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.CourseTitle).HasMaxLength(200);
            entity.Property(e => e.EnrollmentId).HasColumnName("EnrollmentID");
            entity.Property(e => e.ProgressPercentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.StudentName).HasMaxLength(101);
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Username).HasMaxLength(50);
        });
        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.QuizId).HasName("PK_Quiz");
            entity.ToTable("Quiz");

            entity.Property(e => e.QuizId).HasColumnName("QuizID");
            entity.Property(e => e.LessonId).HasColumnName("LessonID");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Description);
            entity.Property(e => e.IsRandomOrder).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
            entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");

            entity.HasIndex(e => e.LessonId)
                .HasDatabaseName("IX_Quiz_LessonID");

            // FK -> Lessons
            entity.HasOne(d => d.Lesson)
                .WithMany() // chưa cần collection Quizzes trong Lesson
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Quiz_Lesson");

            // FK -> Users (CreatedBy, UpdatedBy)
            entity.HasOne(d => d.CreatedByNavigation)
                .WithMany(p => p.QuizzesCreated)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quiz_CreatedBy");

            entity.HasOne(d => d.UpdatedByNavigation)
                .WithMany(p => p.QuizzesUpdated)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_Quiz_UpdatedBy");
        });


        // =======================
        // QUIZ QUESTION
        // =======================

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK_QuizQuestion");
            entity.ToTable("QuizQuestion");

            entity.Property(e => e.QuestionId).HasColumnName("QuestionID");
            entity.Property(e => e.QuizId).HasColumnName("QuizID");
            entity.Property(e => e.QuestionType).HasMaxLength(20);
            entity.Property(e => e.QuestionOrder);
            entity.Property(e => e.Points);
            entity.Property(e => e.Explanation);
            entity.Property(e => e.CreatedAt);

            entity.HasIndex(e => new { e.QuizId, e.QuestionOrder })
                .IsUnique()
                .HasDatabaseName("UQ_QuizQuestion_Order");

            entity.HasOne(d => d.Quiz)
                .WithMany(p => p.QuizQuestions)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_QuizQuestion_Quiz");
        });

        // =======================
        // QUIZ ANSWER
        // =======================

        modelBuilder.Entity<QuizAnswer>(entity =>
        {
            entity.HasKey(e => e.AnswerId).HasName("PK_QuizAnswer");

            entity.Property(e => e.AnswerId).HasColumnName("AnswerID");
            entity.Property(e => e.QuestionId).HasColumnName("QuestionID");
            entity.Property(e => e.AnswerText);
            entity.Property(e => e.IsCorrect).HasDefaultValue(false);
            entity.Property(e => e.AnswerOrder);

            entity.HasIndex(e => new { e.QuestionId, e.AnswerOrder })
                .IsUnique()
                .HasDatabaseName("UQ_QuizAnswer_Order");

            entity.HasOne(d => d.Question)  // Chỉ config cho Question
                .WithMany(p => p.QuizAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_QuizAnswer_Question");
        });


        // =======================
        // USER QUIZ ATTEMPT
        // =======================

        modelBuilder.Entity<UserQuizAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId).HasName("PK_UserQuizAttempt");
            entity.ToTable("UserQuizAttempt");

            entity.Property(e => e.AttemptId).HasColumnName("AttemptID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.QuizId).HasColumnName("QuizID");
            entity.Property(e => e.EnrollmentId).HasColumnName("EnrollmentID");
            entity.Property(e => e.StartedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SubmittedAt);
            entity.Property(e => e.TotalScore).HasColumnType("decimal(5,2)").HasDefaultValue(0);
            entity.Property(e => e.MaxScore).HasDefaultValue(0);
            entity.Property(e => e.PercentageScore).HasColumnType("decimal(5,2)").HasDefaultValue(0);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("in_progress");

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_UserQuizAttempt_UserID");
            entity.HasIndex(e => e.QuizId).HasDatabaseName("IX_UserQuizAttempt_QuizID");
            entity.HasIndex(e => e.EnrollmentId).HasDatabaseName("IX_UserQuizAttempt_EnrollmentID");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_UserQuizAttempt_Status");

            entity.HasOne(d => d.User)
                .WithMany(p => p.UserQuizAttempts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserQuizAttempt_User");

            entity.HasOne(d => d.Quiz)
                .WithMany(p => p.UserQuizAttempts)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserQuizAttempt_Quiz");

            // Không ép Enrollment phải có collection UserQuizAttempts, nên dùng WithMany() trống
            entity.HasOne(d => d.Enrollment)
                .WithMany()
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserQuizAttempt_Enrollment");
        });


        // =======================
        // USER QUIZ ANSWER
        // =======================

        modelBuilder.Entity<UserQuizAnswer>(entity =>
        {
            entity.HasKey(e => e.UserAnswerId).HasName("PK_UserQuizAnswer");
            entity.ToTable("UserQuizAnswer");

            entity.Property(e => e.UserAnswerId).HasColumnName("UserAnswerID");
            entity.Property(e => e.AttemptId).HasColumnName("AttemptID");
            entity.Property(e => e.QuestionId).HasColumnName("QuestionID");
            entity.Property(e => e.SelectedAnswerId).HasColumnName("SelectedAnswerID");
            entity.Property(e => e.EssayAnswer);
            entity.Property(e => e.IsCorrect);
            entity.Property(e => e.EarnedPoints).HasColumnType("decimal(5,2)").HasDefaultValue(0);
            entity.Property(e => e.TeacherFeedback);
            entity.Property(e => e.GradedBy).HasColumnName("GradedBy");
            entity.Property(e => e.GradedAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasIndex(e => new { e.AttemptId, e.QuestionId })
                .IsUnique()
                .HasDatabaseName("UQ_UserQuizAnswer_AttemptQuestion");

            entity.HasOne(d => d.Attempt)
                .WithMany(p => p.UserQuizAnswers)
                .HasForeignKey(d => d.AttemptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserQuizAnswer_Attempt");

            entity.HasOne(d => d.Question)
                .WithMany(p => p.UserQuizAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserQuizAnswer_Question");

            entity.HasOne(d => d.SelectedAnswer)
                .WithMany(p => p.UserQuizAnswers)
                .HasForeignKey(d => d.SelectedAnswerId)
                .HasConstraintName("FK_UserQuizAnswer_SelectedAnswer");

            entity.HasOne(d => d.GradedByNavigation)
                .WithMany(p => p.EssaysGraded)
                .HasForeignKey(d => d.GradedBy)
                .HasConstraintName("FK_UserQuizAnswer_GradedBy");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
