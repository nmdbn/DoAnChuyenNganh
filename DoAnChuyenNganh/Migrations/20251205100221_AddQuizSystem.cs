using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnChuyenNganh.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    GradeID = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GradeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GradeLevel = table.Column<byte>(type: "tinyint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Grades__54F87A370C89BB0B", x => x.GradeID);
                });

            migrationBuilder.CreateTable(
                name: "ReportCategories",
                columns: table => new
                {
                    CategoryID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SeverityLevel = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)2),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ReportCa__19093A2BAA4E083D", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PermissionLevel = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__8AFACE3AB9970884", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectID = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SubjectCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IconUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Subjects__AC1BA38819921A5A", x => x.SubjectID);
                });

            migrationBuilder.CreateTable(
                name: "SystemStats",
                columns: table => new
                {
                    StatID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StatValue = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SystemSt__3A162D1EBEB537FB", x => x.StatID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    PasswordSalt = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RoleID = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)2),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoginAttempts = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0),
                    LockoutUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    GoogleId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FacebookId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__1788CCAC5252C436", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Users_Role",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                    table.ForeignKey(
                        name: "FK_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ContentReports",
                columns: table => new
                {
                    ReportID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<byte>(type: "tinyint", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    ReportedUserID = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "pending"),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ContentR__D5BD48E525467EF8", x => x.ReportID);
                    table.ForeignKey(
                        name: "FK_ContentReports_Category",
                        column: x => x.CategoryID,
                        principalTable: "ReportCategories",
                        principalColumn: "CategoryID");
                    table.ForeignKey(
                        name: "FK_ContentReports_ReportedUser",
                        column: x => x.ReportedUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ContentReports_Reporter",
                        column: x => x.ReporterID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ContentReports_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SubjectID = table.Column<short>(type: "smallint", nullable: false),
                    GradeID = table.Column<short>(type: "smallint", nullable: false),
                    InstructorID = table.Column<int>(type: "int", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DifficultyLevel = table.Column<byte>(type: "tinyint", nullable: true),
                    EstimatedHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(8,2)", nullable: true, defaultValue: 0m),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ViewCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    EnrollmentCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", nullable: true, defaultValue: 0m),
                    RatingCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Courses__C92D7187A7867EC3", x => x.CourseID);
                    table.ForeignKey(
                        name: "FK_Courses_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Courses_Grade",
                        column: x => x.GradeID,
                        principalTable: "Grades",
                        principalColumn: "GradeID");
                    table.ForeignKey(
                        name: "FK_Courses_Instructor",
                        column: x => x.InstructorID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Courses_Subject",
                        column: x => x.SubjectID,
                        principalTable: "Subjects",
                        principalColumn: "SubjectID");
                    table.ForeignKey(
                        name: "FK_Courses_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ForumCategories",
                columns: table => new
                {
                    CategoryID = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<short>(type: "smallint", nullable: true, defaultValue: (short)0),
                    PostCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    LastPostAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPostBy = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ForumCat__19093A2B41E18815", x => x.CategoryID);
                    table.ForeignKey(
                        name: "FK_ForumCategories_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ForumCategories_LastPostBy",
                        column: x => x.LastPostBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RelatedItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RelatedItemID = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__20CF2E32D36C52B6", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_Notifications_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "PrivateMessages",
                columns: table => new
                {
                    MessageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderID = table.Column<int>(type: "int", nullable: false),
                    RecipientID = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MessageContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PrivateM__C87C037C1630A4CF", x => x.MessageID);
                    table.ForeignKey(
                        name: "FK_PrivateMessages_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_PrivateMessages_Recipient",
                        column: x => x.RecipientID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_PrivateMessages_Sender",
                        column: x => x.SenderID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserActivityLog",
                columns: table => new
                {
                    LogID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    ActivityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ItemID = table.Column<int>(type: "int", nullable: true),
                    IPAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserActi__5E5499A8FB38935A", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_UserActivityLog_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserBookmarks",
                columns: table => new
                {
                    BookmarkID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    BookmarkTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserBook__541A3A915FD774EE", x => x.BookmarkID);
                    table.ForeignKey(
                        name: "FK_UserBookmarks_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserLikes",
                columns: table => new
                {
                    LikeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserLike__A2922CF4F93C1D7F", x => x.LikeID);
                    table.ForeignKey(
                        name: "FK_UserLikes_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    SessionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    SessionToken = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    IPAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserSess__C9F49270E08F9D57", x => x.SessionID);
                    table.ForeignKey(
                        name: "FK_UserSessions_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "CourseRatings",
                columns: table => new
                {
                    RatingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<byte>(type: "tinyint", nullable: false),
                    Review = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CourseRa__FCCDF85C0C047AA4", x => x.RatingID);
                    table.ForeignKey(
                        name: "FK_CourseRatings_Course",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID");
                    table.ForeignKey(
                        name: "FK_CourseRatings_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    EnrollmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CourseID = table.Column<int>(type: "int", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProgressPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true, defaultValue: 0m),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalTimeSpent = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Enrollme__7F6877FB01E3873C", x => x.EnrollmentID);
                    table.ForeignKey(
                        name: "FK_Enrollments_Course",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID");
                    table.ForeignKey(
                        name: "FK_Enrollments_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    LessonID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LessonOrder = table.Column<short>(type: "smallint", nullable: false),
                    LessonType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "text"),
                    VideoUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    VideoDuration = table.Column<int>(type: "int", nullable: true),
                    IsPreviewable = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Lessons__B084ACB02F1F1081", x => x.LessonID);
                    table.ForeignKey(
                        name: "FK_Lessons_Course",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID");
                    table.ForeignKey(
                        name: "FK_Lessons_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Lessons_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MoMoOrderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MoMoRequestId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    District = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ward = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecificAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForumPosts",
                columns: table => new
                {
                    PostID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<short>(type: "smallint", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSticky = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    ViewCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    ReplyCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    LikeCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ForumPos__AA126038D22CCEE9", x => x.PostID);
                    table.ForeignKey(
                        name: "FK_ForumPosts_Category",
                        column: x => x.CategoryID,
                        principalTable: "ForumCategories",
                        principalColumn: "CategoryID");
                    table.ForeignKey(
                        name: "FK_ForumPosts_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ForumPosts_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ChatAttachments",
                columns: table => new
                {
                    AttachmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageID = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ChatAtta__442C64BE", x => x.AttachmentID);
                    table.ForeignKey(
                        name: "FK_ChatAttachments_Message",
                        column: x => x.MessageID,
                        principalTable: "PrivateMessages",
                        principalColumn: "MessageID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatAttachments_User",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "CourseMaterials",
                columns: table => new
                {
                    MaterialID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseID = table.Column<int>(type: "int", nullable: true),
                    LessonID = table.Column<int>(type: "int", nullable: true),
                    MaterialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaterialType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    DownloadCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    IsPublic = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CourseMa__C5061317FA6733ED", x => x.MaterialID);
                    table.ForeignKey(
                        name: "FK_CourseMaterials_Course",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID");
                    table.ForeignKey(
                        name: "FK_CourseMaterials_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_CourseMaterials_Lesson",
                        column: x => x.LessonID,
                        principalTable: "Lessons",
                        principalColumn: "LessonID");
                });

            migrationBuilder.CreateTable(
                name: "LessonProgress",
                columns: table => new
                {
                    ProgressID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentID = table.Column<int>(type: "int", nullable: false),
                    LessonID = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeSpent = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    LastPosition = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LessonPr__BAE29C853337AEF2", x => x.ProgressID);
                    table.ForeignKey(
                        name: "FK_LessonProgress_Enrollment",
                        column: x => x.EnrollmentID,
                        principalTable: "Enrollments",
                        principalColumn: "EnrollmentID");
                    table.ForeignKey(
                        name: "FK_LessonProgress_Lesson",
                        column: x => x.LessonID,
                        principalTable: "Lessons",
                        principalColumn: "LessonID");
                });

            migrationBuilder.CreateTable(
                name: "Quiz",
                columns: table => new
                {
                    QuizID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LessonID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRandomOrder = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quiz", x => x.QuizID);
                    table.ForeignKey(
                        name: "FK_Quiz_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Quiz_Lesson",
                        column: x => x.LessonID,
                        principalTable: "Lessons",
                        principalColumn: "LessonID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quiz_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ForumReplies",
                columns: table => new
                {
                    ReplyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ParentReplyID = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LikeCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ForumRep__C25E46295CAA6A5B", x => x.ReplyID);
                    table.ForeignKey(
                        name: "FK_ForumReplies_ParentReply",
                        column: x => x.ParentReplyID,
                        principalTable: "ForumReplies",
                        principalColumn: "ReplyID");
                    table.ForeignKey(
                        name: "FK_ForumReplies_Post",
                        column: x => x.PostID,
                        principalTable: "ForumPosts",
                        principalColumn: "PostID");
                    table.ForeignKey(
                        name: "FK_ForumReplies_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ForumReplies_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "QuizQuestion",
                columns: table => new
                {
                    QuestionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizID = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuestionOrder = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestion", x => x.QuestionID);
                    table.ForeignKey(
                        name: "FK_QuizQuestion_Quiz",
                        column: x => x.QuizID,
                        principalTable: "Quiz",
                        principalColumn: "QuizID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserQuizAttempt",
                columns: table => new
                {
                    AttemptID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    QuizID = table.Column<int>(type: "int", nullable: false),
                    EnrollmentID = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    MaxScore = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PercentageScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "in_progress")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuizAttempt", x => x.AttemptID);
                    table.ForeignKey(
                        name: "FK_UserQuizAttempt_Enrollment",
                        column: x => x.EnrollmentID,
                        principalTable: "Enrollments",
                        principalColumn: "EnrollmentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserQuizAttempt_Quiz",
                        column: x => x.QuizID,
                        principalTable: "Quiz",
                        principalColumn: "QuizID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserQuizAttempt_User",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForumAttachments",
                columns: table => new
                {
                    AttachmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostID = table.Column<int>(type: "int", nullable: true),
                    ReplyID = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ForumAtt__442C64BE", x => x.AttachmentID);
                    table.ForeignKey(
                        name: "FK_ForumAttachments_Post",
                        column: x => x.PostID,
                        principalTable: "ForumPosts",
                        principalColumn: "PostID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumAttachments_Reply",
                        column: x => x.ReplyID,
                        principalTable: "ForumReplies",
                        principalColumn: "ReplyID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumAttachments_User",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "QuizAnswers",
                columns: table => new
                {
                    AnswerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionID = table.Column<int>(type: "int", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AnswerOrder = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAnswer", x => x.AnswerID);
                    table.ForeignKey(
                        name: "FK_QuizAnswer_Question",
                        column: x => x.QuestionID,
                        principalTable: "QuizQuestion",
                        principalColumn: "QuestionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserQuizAnswer",
                columns: table => new
                {
                    UserAnswerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptID = table.Column<int>(type: "int", nullable: false),
                    QuestionID = table.Column<int>(type: "int", nullable: false),
                    SelectedAnswerID = table.Column<int>(type: "int", nullable: true),
                    EssayAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    EarnedPoints = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    TeacherFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GradedBy = table.Column<int>(type: "int", nullable: true),
                    GradedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuizAnswer", x => x.UserAnswerID);
                    table.ForeignKey(
                        name: "FK_UserQuizAnswer_Attempt",
                        column: x => x.AttemptID,
                        principalTable: "UserQuizAttempt",
                        principalColumn: "AttemptID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserQuizAnswer_GradedBy",
                        column: x => x.GradedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_UserQuizAnswer_Question",
                        column: x => x.QuestionID,
                        principalTable: "QuizQuestion",
                        principalColumn: "QuestionID");
                    table.ForeignKey(
                        name: "FK_UserQuizAnswer_SelectedAnswer",
                        column: x => x.SelectedAnswerID,
                        principalTable: "QuizAnswers",
                        principalColumn: "AnswerID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatAttachments_MessageID",
                table: "ChatAttachments",
                column: "MessageID");

            migrationBuilder.CreateIndex(
                name: "IX_ChatAttachments_UploadedBy",
                table: "ChatAttachments",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_CategoryID",
                table: "ContentReports",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ReportedUserID",
                table: "ContentReports",
                column: "ReportedUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ReporterID",
                table: "ContentReports",
                column: "ReporterID");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ReviewedBy",
                table: "ContentReports",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CourseID",
                table: "CourseMaterials",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CreatedBy",
                table: "CourseMaterials",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_LessonID",
                table: "CourseMaterials",
                column: "LessonID");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRatings_UserID",
                table: "CourseRatings",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ_CourseRatings_CourseUser",
                table: "CourseRatings",
                columns: new[] { "CourseID", "UserID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CreatedAt",
                table: "Courses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CreatedBy",
                table: "Courses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_GradeID",
                table: "Courses",
                column: "GradeID");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_InstructorID",
                table: "Courses",
                column: "InstructorID");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_IsFeatured",
                table: "Courses",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_IsPublished",
                table: "Courses",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SubjectID",
                table: "Courses",
                column: "SubjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_UpdatedBy",
                table: "Courses",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ__Courses__BC7B5FB6148CA239",
                table: "Courses",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseID",
                table: "Enrollments",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_EnrolledAt",
                table: "Enrollments",
                column: "EnrolledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_IsActive",
                table: "Enrollments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserID",
                table: "Enrollments",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrollments_UserCourse",
                table: "Enrollments",
                columns: new[] { "UserID", "CourseID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForumAttachments_CreatedAt",
                table: "ForumAttachments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ForumAttachments_PostID",
                table: "ForumAttachments",
                column: "PostID");

            migrationBuilder.CreateIndex(
                name: "IX_ForumAttachments_ReplyID",
                table: "ForumAttachments",
                column: "ReplyID");

            migrationBuilder.CreateIndex(
                name: "IX_ForumAttachments_UploadedBy",
                table: "ForumAttachments",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ForumCategories_CreatedBy",
                table: "ForumCategories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ForumCategories_LastPostBy",
                table: "ForumCategories",
                column: "LastPostBy");

            migrationBuilder.CreateIndex(
                name: "UQ__ForumCat__8517B2E0A07B2F13",
                table: "ForumCategories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_CategoryID",
                table: "ForumPosts",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_CreatedAt",
                table: "ForumPosts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_IsApproved",
                table: "ForumPosts",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_UpdatedBy",
                table: "ForumPosts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_UserID",
                table: "ForumPosts",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_CreatedAt",
                table: "ForumReplies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_ParentReplyID",
                table: "ForumReplies",
                column: "ParentReplyID");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_PostID",
                table: "ForumReplies",
                column: "PostID");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_UpdatedBy",
                table: "ForumReplies",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ForumReplies_UserID",
                table: "ForumReplies",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ__Grades__4AA309AAAAD6354E",
                table: "Grades",
                column: "GradeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_LessonID",
                table: "LessonProgress",
                column: "LessonID");

            migrationBuilder.CreateIndex(
                name: "UQ_LessonProgress_EnrollmentLesson",
                table: "LessonProgress",
                columns: new[] { "EnrollmentID", "LessonID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseID",
                table: "Lessons",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CreatedBy",
                table: "Lessons",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_IsPublished",
                table: "Lessons",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_LessonOrder",
                table: "Lessons",
                column: "LessonOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UpdatedBy",
                table: "Lessons",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_Lessons_CourseOrder",
                table: "Lessons",
                columns: new[] { "CourseID", "LessonOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CourseId",
                table: "Payments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessages_DeletedBy",
                table: "PrivateMessages",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessages_RecipientID",
                table: "PrivateMessages",
                column: "RecipientID");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessages_SenderID",
                table: "PrivateMessages",
                column: "SenderID");

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_CreatedBy",
                table: "Quiz",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_LessonID",
                table: "Quiz",
                column: "LessonID");

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_UpdatedBy",
                table: "Quiz",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_QuizAnswer_Order",
                table: "QuizAnswers",
                columns: new[] { "QuestionID", "AnswerOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_QuizQuestion_Order",
                table: "QuizQuestion",
                columns: new[] { "QuizID", "QuestionOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__ReportCa__8517B2E0D7CDF3C2",
                table: "ReportCategories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Roles__8A2B616081C5840F",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Subjects__4C5A7D559153908F",
                table: "Subjects",
                column: "SubjectName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Subjects__9F7CE1A94E5C27AE",
                table: "Subjects",
                column: "SubjectCode",
                unique: true,
                filter: "[SubjectCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ__SystemSt__7CF10922D8088ACC",
                table: "SystemStats",
                column: "StatName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_ActivityType",
                table: "UserActivityLog",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_CreatedAt",
                table: "UserActivityLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLog_UserID",
                table: "UserActivityLog",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ_UserBookmarks_UserItemType",
                table: "UserBookmarks",
                columns: new[] { "UserID", "ItemType", "ItemID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UserLikes_UserItemType",
                table: "UserLikes",
                columns: new[] { "UserID", "ItemType", "ItemID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizAnswer_GradedBy",
                table: "UserQuizAnswer",
                column: "GradedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizAnswer_QuestionID",
                table: "UserQuizAnswer",
                column: "QuestionID");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizAnswer_SelectedAnswerID",
                table: "UserQuizAnswer",
                column: "SelectedAnswerID");

            migrationBuilder.CreateIndex(
                name: "UQ_UserQuizAnswer_AttemptQuestion",
                table: "UserQuizAnswer",
                columns: new[] { "AttemptID", "QuestionID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizAttempt_EnrollmentID",
                table: "UserQuizAttempt",
                column: "EnrollmentID");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizAttempt_QuizID",
                table: "UserQuizAttempt",
                column: "QuizID");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizAttempt_Status",
                table: "UserQuizAttempt",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizAttempt_UserID",
                table: "UserQuizAttempt",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedBy",
                table: "Users",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedBy",
                table: "Users",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__536C85E47F0825F4",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D10534322A3F24",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserID",
                table: "UserSessions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ__UserSess__46BDD12468F75D4B",
                table: "UserSessions",
                column: "SessionToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatAttachments");

            migrationBuilder.DropTable(
                name: "ContentReports");

            migrationBuilder.DropTable(
                name: "CourseMaterials");

            migrationBuilder.DropTable(
                name: "CourseRatings");

            migrationBuilder.DropTable(
                name: "ForumAttachments");

            migrationBuilder.DropTable(
                name: "LessonProgress");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "SystemStats");

            migrationBuilder.DropTable(
                name: "UserActivityLog");

            migrationBuilder.DropTable(
                name: "UserBookmarks");

            migrationBuilder.DropTable(
                name: "UserLikes");

            migrationBuilder.DropTable(
                name: "UserQuizAnswer");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "PrivateMessages");

            migrationBuilder.DropTable(
                name: "ReportCategories");

            migrationBuilder.DropTable(
                name: "ForumReplies");

            migrationBuilder.DropTable(
                name: "UserQuizAttempt");

            migrationBuilder.DropTable(
                name: "QuizAnswers");

            migrationBuilder.DropTable(
                name: "ForumPosts");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "QuizQuestion");

            migrationBuilder.DropTable(
                name: "ForumCategories");

            migrationBuilder.DropTable(
                name: "Quiz");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
