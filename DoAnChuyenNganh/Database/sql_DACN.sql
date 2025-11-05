-- Create Database
USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DoAnChuyenNganh')
BEGIN
    CREATE DATABASE DoAnChuyenNganh;
END
GO

USE DoAnChuyenNganh;
GO

-- =============================================
-- CORE REFERENCE TABLES
-- =============================================

-- Roles Table - User permission levels
CREATE TABLE Roles (
    RoleID TINYINT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(20) NOT NULL UNIQUE,
    Description NVARCHAR(100),
    PermissionLevel TINYINT NOT NULL, -- 1=Guest, 2=User, 3=Instructor, 4=Admin
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- Subjects Table - Academic subjects
CREATE TABLE Subjects (
    SubjectID SMALLINT IDENTITY(1,1) PRIMARY KEY,
    SubjectName NVARCHAR(80) NOT NULL UNIQUE,
    SubjectCode VARCHAR(10) UNIQUE,
    Description NVARCHAR(200),
    IconUrl NVARCHAR(300),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Grades Table - Academic grade levels
CREATE TABLE Grades (
    GradeID SMALLINT IDENTITY(1,1) PRIMARY KEY,
    GradeName NVARCHAR(50) NOT NULL UNIQUE,
    GradeLevel TINYINT NOT NULL,
    Description NVARCHAR(150),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- =============================================
-- USER MANAGEMENT SYSTEM
-- =============================================

-- Users Table - Main user accounts
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    PasswordSalt VARCHAR(100) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    DateOfBirth DATE,
    PhoneNumber VARCHAR(15),
    AvatarUrl NVARCHAR(400),
    Bio NVARCHAR(500),
    RoleID TINYINT NOT NULL DEFAULT 2, -- Default to User role
    IsEmailVerified BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    IsLocked BIT DEFAULT 0,
    LastLoginAt DATETIME2,
    LoginAttempts TINYINT DEFAULT 0,
    LockoutUntil DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedBy INT,
	GoogleId NVARCHAR(50) NULL,
    
    CONSTRAINT FK_Users_Role FOREIGN KEY (RoleID) REFERENCES Roles(RoleID),
    CONSTRAINT FK_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserID),
    CONSTRAINT FK_Users_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(UserID),
    CONSTRAINT CK_Users_Email CHECK (Email LIKE '%@%.%'),
    CONSTRAINT CK_Users_Username CHECK (LEN(Username) >= 3)
);

-- User Sessions Table - Login session management
CREATE TABLE UserSessions (
    SessionID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    SessionToken VARCHAR(255) NOT NULL UNIQUE,
    IPAddress VARCHAR(45),
    UserAgent NVARCHAR(500),
    ExpiresAt DATETIME2 NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_UserSessions_User FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- =============================================
-- COURSE AND CONTENT MANAGEMENT
-- =============================================

-- Courses Table - Course catalog
CREATE TABLE Courses (
    CourseID INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Slug VARCHAR(150) UNIQUE,
    Description NVARCHAR(MAX),
    ShortDescription NVARCHAR(300),
    SubjectID SMALLINT NOT NULL,
    GradeID SMALLINT NOT NULL,
    InstructorID INT NOT NULL,
    ThumbnailUrl NVARCHAR(400),
    DifficultyLevel TINYINT CHECK (DifficultyLevel BETWEEN 1 AND 5),
    EstimatedHours DECIMAL(5,2),
    Price DECIMAL(8,2) DEFAULT 0.00,
    IsPublished BIT DEFAULT 0,
    IsFeatured BIT DEFAULT 0,
    ViewCount INT DEFAULT 0,
    EnrollmentCount INT DEFAULT 0,
    AverageRating DECIMAL(3,2) DEFAULT 0.00,
    RatingCount INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    PublishedAt DATETIME2,
    CreatedBy INT NOT NULL,
    UpdatedBy INT,
    
    CONSTRAINT FK_Courses_Subject FOREIGN KEY (SubjectID) REFERENCES Subjects(SubjectID),
    CONSTRAINT FK_Courses_Grade FOREIGN KEY (GradeID) REFERENCES Grades(GradeID),
    CONSTRAINT FK_Courses_Instructor FOREIGN KEY (InstructorID) REFERENCES Users(UserID),
    CONSTRAINT FK_Courses_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserID),
    CONSTRAINT FK_Courses_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(UserID)
);

-- Lessons Table - Individual lessons within courses
CREATE TABLE Lessons (
    LessonID INT IDENTITY(1,1) PRIMARY KEY,
    CourseID INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX),
    LessonOrder SMALLINT NOT NULL,
    LessonType NVARCHAR(20) DEFAULT 'text', -- 'text', 'video', 'quiz', 'assignment'
    VideoUrl NVARCHAR(400),
    VideoDuration INT, -- in seconds
    IsPreviewable BIT DEFAULT 0,
    IsPublished BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    UpdatedBy INT,
    
    CONSTRAINT FK_Lessons_Course FOREIGN KEY (CourseID) REFERENCES Courses(CourseID),
    CONSTRAINT FK_Lessons_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserID),
    CONSTRAINT FK_Lessons_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(UserID),
    CONSTRAINT UQ_Lessons_CourseOrder UNIQUE (CourseID, LessonOrder),
    CONSTRAINT CK_Lessons_Type CHECK (LessonType IN ('text', 'video', 'quiz', 'assignment'))
);

-- Course Materials Table - Additional files and resources
CREATE TABLE CourseMaterials (
    MaterialID INT IDENTITY(1,1) PRIMARY KEY,
    CourseID INT,
    LessonID INT,
    MaterialName NVARCHAR(200) NOT NULL,
    MaterialType NVARCHAR(20) NOT NULL, -- 'document', 'image', 'video', 'audio', 'link'
    FileUrl NVARCHAR(500),
    FileSize BIGINT,
    DownloadCount INT DEFAULT 0,
    IsPublic BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    
    CONSTRAINT FK_CourseMaterials_Course FOREIGN KEY (CourseID) REFERENCES Courses(CourseID),
    CONSTRAINT FK_CourseMaterials_Lesson FOREIGN KEY (LessonID) REFERENCES Lessons(LessonID),
    CONSTRAINT FK_CourseMaterials_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserID),
    CONSTRAINT CK_CourseMaterials_Type CHECK (MaterialType IN ('document', 'image', 'video', 'audio', 'link')),
    CONSTRAINT CK_CourseMaterials_Reference CHECK ((CourseID IS NOT NULL AND LessonID IS NULL) OR (CourseID IS NULL AND LessonID IS NOT NULL))
);

-- =============================================
-- ENROLLMENT AND PROGRESS TRACKING
-- =============================================

-- Enrollments Table - Student course enrollments
CREATE TABLE Enrollments (
    EnrollmentID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    CourseID INT NOT NULL,
    EnrolledAt DATETIME2 DEFAULT GETDATE(),
    CompletedAt DATETIME2,
    ProgressPercentage DECIMAL(5,2) DEFAULT 0.00,
    LastAccessedAt DATETIME2,
    TotalTimeSpent INT DEFAULT 0, -- in minutes
    IsActive BIT DEFAULT 1,
    
    CONSTRAINT FK_Enrollments_User FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Enrollments_Course FOREIGN KEY (CourseID) REFERENCES Courses(CourseID),
    CONSTRAINT UQ_Enrollments_UserCourse UNIQUE (UserID, CourseID),
    CONSTRAINT CK_Enrollments_Progress CHECK (ProgressPercentage BETWEEN 0 AND 100)
);

-- Lesson Progress Table - Individual lesson completion tracking
CREATE TABLE LessonProgress (
    ProgressID INT IDENTITY(1,1) PRIMARY KEY,
    EnrollmentID INT NOT NULL,
    LessonID INT NOT NULL,
    IsCompleted BIT DEFAULT 0,
    CompletedAt DATETIME2,
    TimeSpent INT DEFAULT 0, -- in minutes
    LastPosition INT DEFAULT 0, -- for video lessons - seconds
    Notes NVARCHAR(1000),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_LessonProgress_Enrollment FOREIGN KEY (EnrollmentID) REFERENCES Enrollments(EnrollmentID),
    CONSTRAINT FK_LessonProgress_Lesson FOREIGN KEY (LessonID) REFERENCES Lessons(LessonID),
    CONSTRAINT UQ_LessonProgress_EnrollmentLesson UNIQUE (EnrollmentID, LessonID)
);

-- =============================================
-- FORUM AND COMMUNITY SYSTEM
-- =============================================

-- Forum Categories Table - Discussion categories
CREATE TABLE ForumCategories (
    CategoryID SMALLINT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(300),
    SortOrder SMALLINT DEFAULT 0,
    PostCount INT DEFAULT 0,
    LastPostAt DATETIME2,
    LastPostBy INT,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    
    CONSTRAINT FK_ForumCategories_LastPostBy FOREIGN KEY (LastPostBy) REFERENCES Users(UserID),
    CONSTRAINT FK_ForumCategories_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
);

-- Forum Posts Table - Discussion posts
CREATE TABLE ForumPosts (
    PostID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID SMALLINT NOT NULL,
    UserID INT NOT NULL,
    Title NVARCHAR(250) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    IsSticky BIT DEFAULT 0,
    IsLocked BIT DEFAULT 0,
    ViewCount INT DEFAULT 0,
    ReplyCount INT DEFAULT 0,
    LikeCount INT DEFAULT 0,
    IsApproved BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedBy INT,
    
    CONSTRAINT FK_ForumPosts_Category FOREIGN KEY (CategoryID) REFERENCES ForumCategories(CategoryID),
    CONSTRAINT FK_ForumPosts_User FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_ForumPosts_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(UserID)
);

-- Forum Replies Table - Replies to posts
CREATE TABLE ForumReplies (
    ReplyID INT IDENTITY(1,1) PRIMARY KEY,
    PostID INT NOT NULL,
    UserID INT NOT NULL,
    ParentReplyID INT, -- For nested replies
    Content NVARCHAR(MAX) NOT NULL,
    LikeCount INT DEFAULT 0,
    IsApproved BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedBy INT,

    CONSTRAINT FK_ForumReplies_Post FOREIGN KEY (PostID) REFERENCES ForumPosts(PostID),
    CONSTRAINT FK_ForumReplies_User FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_ForumReplies_ParentReply FOREIGN KEY (ParentReplyID) REFERENCES ForumReplies(ReplyID),
    CONSTRAINT FK_ForumReplies_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(UserID)
);

-- =============================================
-- INTERACTION AND ENGAGEMENT SYSTEM
-- =============================================

-- User Bookmarks Table - Save posts, courses, lessons
CREATE TABLE UserBookmarks (
    BookmarkID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    ItemType NVARCHAR(20) NOT NULL, -- 'course', 'lesson', 'post', 'reply'
    ItemID INT NOT NULL,
    BookmarkTitle NVARCHAR(200),
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),

    CONSTRAINT FK_UserBookmarks_User FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT CK_UserBookmarks_ItemType CHECK (ItemType IN ('course', 'lesson', 'post', 'reply')),
    CONSTRAINT UQ_UserBookmarks_UserItemType UNIQUE (UserID, ItemType, ItemID)
);

-- User Likes Table - Like system for posts and replies
CREATE TABLE UserLikes (
    LikeID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    ItemType NVARCHAR(10) NOT NULL, -- 'post', 'reply'
    ItemID INT NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),

    CONSTRAINT FK_UserLikes_User FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT CK_UserLikes_ItemType CHECK (ItemType IN ('post', 'reply')),
    CONSTRAINT UQ_UserLikes_UserItemType UNIQUE (UserID, ItemType, ItemID)
);

-- Course Ratings Table - Course rating and review system
CREATE TABLE CourseRatings (
    RatingID INT IDENTITY(1,1) PRIMARY KEY,
    CourseID INT NOT NULL,
    UserID INT NOT NULL,
    Rating TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Review NVARCHAR(1000),
    IsApproved BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),

    CONSTRAINT FK_CourseRatings_Course FOREIGN KEY (CourseID) REFERENCES Courses(CourseID),
    CONSTRAINT FK_CourseRatings_User FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT UQ_CourseRatings_CourseUser UNIQUE (CourseID, UserID)
);

-- =============================================
-- NOTIFICATION SYSTEM
-- =============================================

-- Notifications Table - User notifications
CREATE TABLE Notifications (
    NotificationID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    Type NVARCHAR(30) NOT NULL, -- 'course_enrollment', 'new_lesson', 'forum_reply', 'system_alert'
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    RelatedItemType NVARCHAR(20), -- 'course', 'lesson', 'post', 'reply'
    RelatedItemID INT,
    IsRead BIT DEFAULT 0,
    ReadAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),

    CONSTRAINT FK_Notifications_User FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT CK_Notifications_Type CHECK (Type IN ('course_enrollment', 'new_lesson', 'forum_reply', 'system_alert', 'course_completion', 'new_material'))
);

-- =============================================
-- REPORTING AND MODERATION SYSTEM
-- =============================================

-- Report Categories Table - Types of content violations
CREATE TABLE ReportCategories (
    CategoryID TINYINT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200),
    SeverityLevel TINYINT DEFAULT 2, -- 1=Low, 2=Medium, 3=High
    IsActive BIT DEFAULT 1
);

-- Content Reports Table - User reports for inappropriate content
CREATE TABLE ContentReports (
    ReportID INT IDENTITY(1,1) PRIMARY KEY,
    ReporterID INT NOT NULL,
    CategoryID TINYINT NOT NULL,
    ItemType NVARCHAR(20) NOT NULL, -- 'post', 'reply', 'course', 'user'
    ItemID INT NOT NULL,
    ReportedUserID INT, -- User being reported (if applicable)
    Reason NVARCHAR(1000) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'pending', -- 'pending', 'reviewed', 'resolved', 'dismissed'
    AdminNotes NVARCHAR(1000),
    ReviewedBy INT,
    ReviewedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),

    CONSTRAINT FK_ContentReports_Reporter FOREIGN KEY (ReporterID) REFERENCES Users(UserID),
    CONSTRAINT FK_ContentReports_Category FOREIGN KEY (CategoryID) REFERENCES ReportCategories(CategoryID),
    CONSTRAINT FK_ContentReports_ReportedUser FOREIGN KEY (ReportedUserID) REFERENCES Users(UserID),
    CONSTRAINT FK_ContentReports_ReviewedBy FOREIGN KEY (ReviewedBy) REFERENCES Users(UserID),
    CONSTRAINT CK_ContentReports_ItemType CHECK (ItemType IN ('post', 'reply', 'course', 'user')),
    CONSTRAINT CK_ContentReports_Status CHECK (Status IN ('pending', 'reviewed', 'resolved', 'dismissed'))
);

-- =============================================
-- MESSAGING SYSTEM (Extended Feature)
-- =============================================

-- Private Messages Table - Direct messaging between users
CREATE TABLE PrivateMessages (
    MessageID INT IDENTITY(1,1) PRIMARY KEY,
    SenderID INT NOT NULL,
    RecipientID INT NOT NULL,
    Subject NVARCHAR(200),
    MessageContent NVARCHAR(MAX) NOT NULL,
    IsRead BIT DEFAULT 0,
    ReadAt DATETIME2,
    IsDeleted BIT DEFAULT 0,
    DeletedBy INT, -- Who deleted it (sender or recipient)
    CreatedAt DATETIME2 DEFAULT GETDATE(),

    CONSTRAINT FK_PrivateMessages_Sender FOREIGN KEY (SenderID) REFERENCES Users(UserID),
    CONSTRAINT FK_PrivateMessages_Recipient FOREIGN KEY (RecipientID) REFERENCES Users(UserID),
    CONSTRAINT FK_PrivateMessages_DeletedBy FOREIGN KEY (DeletedBy) REFERENCES Users(UserID)
);

-- =============================================
-- ANALYTICS AND SYSTEM TRACKING
-- =============================================

-- System Statistics Table - Track key metrics
CREATE TABLE SystemStats (
    StatID INT IDENTITY(1,1) PRIMARY KEY,
    StatName NVARCHAR(100) NOT NULL UNIQUE,
    StatValue BIGINT NOT NULL DEFAULT 0,
    LastUpdated DATETIME2 DEFAULT GETDATE(),
    Description NVARCHAR(200)
);

-- User Activity Log Table - Track user actions for analytics
CREATE TABLE UserActivityLog (
    LogID BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserID INT,
    ActivityType NVARCHAR(50) NOT NULL, -- 'login', 'course_view', 'lesson_complete', 'post_create'
    ItemType NVARCHAR(20), -- 'course', 'lesson', 'post'
    ItemID INT,
    IPAddress VARCHAR(45),
    UserAgent NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),

    CONSTRAINT FK_UserActivityLog_User FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- =============================================
-- CREATE PERFORMANCE INDEXES
-- =============================================

-- User indexes
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_RoleID ON Users(RoleID);
CREATE INDEX IX_Users_IsActive ON Users(IsActive);
CREATE INDEX IX_Users_CreatedAt ON Users(CreatedAt);

-- Course indexes
CREATE INDEX IX_Courses_SubjectID ON Courses(SubjectID);
CREATE INDEX IX_Courses_GradeID ON Courses(GradeID);
CREATE INDEX IX_Courses_InstructorID ON Courses(InstructorID);
CREATE INDEX IX_Courses_IsPublished ON Courses(IsPublished);
CREATE INDEX IX_Courses_IsFeatured ON Courses(IsFeatured);
CREATE INDEX IX_Courses_CreatedAt ON Courses(CreatedAt);

-- Lesson indexes
CREATE INDEX IX_Lessons_CourseID ON Lessons(CourseID);
CREATE INDEX IX_Lessons_IsPublished ON Lessons(IsPublished);
CREATE INDEX IX_Lessons_LessonOrder ON Lessons(LessonOrder);

-- Enrollment indexes
CREATE INDEX IX_Enrollments_UserID ON Enrollments(UserID);
CREATE INDEX IX_Enrollments_CourseID ON Enrollments(CourseID);
CREATE INDEX IX_Enrollments_EnrolledAt ON Enrollments(EnrolledAt);
CREATE INDEX IX_Enrollments_IsActive ON Enrollments(IsActive);

-- Forum indexes
CREATE INDEX IX_ForumPosts_CategoryID ON ForumPosts(CategoryID);
CREATE INDEX IX_ForumPosts_UserID ON ForumPosts(UserID);
CREATE INDEX IX_ForumPosts_CreatedAt ON ForumPosts(CreatedAt);
CREATE INDEX IX_ForumPosts_IsApproved ON ForumPosts(IsApproved);

CREATE INDEX IX_ForumReplies_PostID ON ForumReplies(PostID);
CREATE INDEX IX_ForumReplies_UserID ON ForumReplies(UserID);
CREATE INDEX IX_ForumReplies_CreatedAt ON ForumReplies(CreatedAt);

-- Notification indexes
CREATE INDEX IX_Notifications_UserID ON Notifications(UserID);
CREATE INDEX IX_Notifications_IsRead ON Notifications(IsRead);
CREATE INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt);

-- Activity log indexes
CREATE INDEX IX_UserActivityLog_UserID ON UserActivityLog(UserID);
CREATE INDEX IX_UserActivityLog_ActivityType ON UserActivityLog(ActivityType);
CREATE INDEX IX_UserActivityLog_CreatedAt ON UserActivityLog(CreatedAt);

-- =============================================
-- INSERT INITIAL REFERENCE DATA
-- =============================================

-- Insert Roles
INSERT INTO Roles (RoleName, Description, PermissionLevel) VALUES
('Guest', 'Unauthenticated users', 1),
('User', 'Regular authenticated users', 2),
('Instructor', 'Users who can create courses', 3),
('Admin', 'System administrators', 4);

-- Create default admin user (CHANGE PASSWORD IMMEDIATELY)
INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, FirstName, LastName, RoleID, IsEmailVerified, IsActive, CreatedBy)
VALUES ('admin', 'admin@vietjack.com', 'CHANGE_THIS_HASH', 'CHANGE_THIS_SALT', 'System', 'Administrator', 4, 1, 1, 1);

-- Insert Subjects
INSERT INTO Subjects (SubjectName, SubjectCode, Description, IsActive) VALUES
('Mathematics', 'MATH', 'Mathematics and arithmetic', 1),
('Physics', 'PHYS', 'Physics and physical sciences', 1),
('Chemistry', 'CHEM', 'Chemistry and chemical sciences', 1),
('Biology', 'BIO', 'Biology and life sciences', 1),
('English', 'ENG', 'English language and literature', 1),
('Vietnamese', 'VIE', 'Vietnamese language and literature', 1),
('History', 'HIST', 'History and social studies', 1),
('Geography', 'GEO', 'Geography and earth sciences', 1),
('Computer Science', 'CS', 'Programming and computer science', 1),
('Art', 'ART', 'Visual arts and creativity', 1);

-- Insert Grades
INSERT INTO Grades (GradeName, GradeLevel, Description, IsActive) VALUES
('Grade 1', 1, 'First grade elementary', 1),
('Grade 2', 2, 'Second grade elementary', 1),
('Grade 3', 3, 'Third grade elementary', 1),
('Grade 4', 4, 'Fourth grade elementary', 1),
('Grade 5', 5, 'Fifth grade elementary', 1),
('Grade 6', 6, 'Sixth grade elementary', 1),
('Grade 7', 7, 'Seventh grade middle school', 1),
('Grade 8', 8, 'Eighth grade middle school', 1),
('Grade 9', 9, 'Ninth grade high school', 1),
('Grade 10', 10, 'Tenth grade high school', 1),
('Grade 11', 11, 'Eleventh grade high school', 1),
('Grade 12', 12, 'Twelfth grade high school', 1),
('University', 13, 'University level', 1);

-- Insert Forum Categories
INSERT INTO ForumCategories (CategoryName, Description, SortOrder, CreatedBy) VALUES
('General Discussion', 'General topics and conversations', 1, 1),
('Study Help', 'Get help with homework and assignments', 2, 1),
('Course Q&A', 'Questions about specific courses', 3, 1),
('Technical Support', 'Technical issues and support', 4, 1),
('Announcements', 'Official announcements', 5, 1);

-- Insert Report Categories
INSERT INTO ReportCategories (CategoryName, Description, SeverityLevel) VALUES
('Spam', 'Spam or unwanted content', 2),
('Inappropriate Content', 'Violates community guidelines', 3),
('Harassment', 'Bullying or harassment', 3),
('Copyright Violation', 'Unauthorized copyrighted content', 2),
('Misinformation', 'False or misleading information', 2),
('Other', 'Other violations', 1);

-- Insert System Statistics
INSERT INTO SystemStats (StatName, StatValue, Description) VALUES
('total_users', 0, 'Total registered users'),
('total_courses', 0, 'Total courses created'),
('total_lessons', 0, 'Total lessons created'),
('total_enrollments', 0, 'Total course enrollments'),
('total_forum_posts', 0, 'Total forum posts'),
('active_users_today', 0, 'Users active today');

-- =============================================
-- CREATE USEFUL VIEWS
-- =============================================

-- Course Summary View
CREATE VIEW vw_CourseSummary AS
SELECT
    c.CourseID,
    c.Title,
    c.Slug,
    s.SubjectName,
    g.GradeName,
    u.FirstName + ' ' + u.LastName AS InstructorName,
    c.DifficultyLevel,
    c.EstimatedHours,
    c.Price,
    c.IsPublished,
    c.IsFeatured,
    c.ViewCount,
    c.EnrollmentCount,
    c.AverageRating,
    c.RatingCount,
    COUNT(DISTINCT l.LessonID) AS TotalLessons,
    COUNT(DISTINCT cm.MaterialID) AS TotalMaterials,
    c.CreatedAt,
    c.PublishedAt
FROM Courses c
LEFT JOIN Subjects s ON c.SubjectID = s.SubjectID
LEFT JOIN Grades g ON c.GradeID = g.GradeID
LEFT JOIN Users u ON c.InstructorID = u.UserID
LEFT JOIN Lessons l ON c.CourseID = l.CourseID AND l.IsPublished = 1
LEFT JOIN CourseMaterials cm ON c.CourseID = cm.CourseID
GROUP BY c.CourseID, c.Title, c.Slug, s.SubjectName, g.GradeName, u.FirstName, u.LastName,
         c.DifficultyLevel, c.EstimatedHours, c.Price, c.IsPublished, c.IsFeatured,
         c.ViewCount, c.EnrollmentCount, c.AverageRating, c.RatingCount, c.CreatedAt, c.PublishedAt;

-- Student Progress View
CREATE VIEW vw_StudentProgress AS
SELECT
    e.EnrollmentID,
    u.UserID,
    u.Username,
    u.FirstName + ' ' + u.LastName AS StudentName,
    c.CourseID,
    c.Title AS CourseTitle,
    e.EnrolledAt,
    e.ProgressPercentage,
    e.LastAccessedAt,
    e.TotalTimeSpent,
    COUNT(DISTINCT lp.LessonID) AS LessonsAccessed,
    COUNT(DISTINCT CASE WHEN lp.IsCompleted = 1 THEN lp.LessonID END) AS LessonsCompleted,
    COUNT(DISTINCT l.LessonID) AS TotalLessons
FROM Enrollments e
INNER JOIN Users u ON e.UserID = u.UserID
INNER JOIN Courses c ON e.CourseID = c.CourseID
LEFT JOIN LessonProgress lp ON e.EnrollmentID = lp.EnrollmentID
LEFT JOIN Lessons l ON c.CourseID = l.CourseID AND l.IsPublished = 1
WHERE e.IsActive = 1
GROUP BY e.EnrollmentID, u.UserID, u.Username, u.FirstName, u.LastName, c.CourseID, c.Title,
         e.EnrolledAt, e.ProgressPercentage, e.LastAccessedAt, e.TotalTimeSpent;

-- Forum Activity View
CREATE VIEW vw_ForumActivity AS
SELECT
    fp.PostID,
    fp.Title,
    fc.CategoryName,
    u.Username AS AuthorUsername,
    u.FirstName + ' ' + u.LastName AS AuthorName,
    fp.ViewCount,
    fp.ReplyCount,
    fp.LikeCount,
    fp.IsSticky,
    fp.IsLocked,
    fp.IsApproved,
    fp.CreatedAt,
    fp.UpdatedAt
FROM ForumPosts fp
INNER JOIN ForumCategories fc ON fp.CategoryID = fc.CategoryID
INNER JOIN Users u ON fp.UserID = u.UserID
WHERE fc.IsActive = 1;

-- =============================================
-- CREATE STORED PROCEDURES
-- =============================================

-- Procedure to enroll user in course
CREATE PROCEDURE sp_EnrollUserInCourse
    @UserID INT,
    @CourseID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if already enrolled
    IF EXISTS (SELECT 1 FROM Enrollments WHERE UserID = @UserID AND CourseID = @CourseID AND IsActive = 1)
    BEGIN
        SELECT 'ERROR' AS Result, 'User is already enrolled in this course' AS Message;
        RETURN;
    END

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Create enrollment
        INSERT INTO Enrollments (UserID, CourseID)
        VALUES (@UserID, @CourseID);

        -- Update course enrollment count
        UPDATE Courses
        SET EnrollmentCount = EnrollmentCount + 1
        WHERE CourseID = @CourseID;

        -- Update system stats
        UPDATE SystemStats
        SET StatValue = StatValue + 1, LastUpdated = GETDATE()
        WHERE StatName = 'total_enrollments';

        -- Create notification
        INSERT INTO Notifications (UserID, Type, Title, Message, RelatedItemType, RelatedItemID)
        SELECT @UserID, 'course_enrollment', 'Course Enrollment Confirmed',
               'You have successfully enrolled in: ' + c.Title, 'course', @CourseID
        FROM Courses c WHERE c.CourseID = @CourseID;

        COMMIT TRANSACTION;

        SELECT 'SUCCESS' AS Result, 'User enrolled successfully' AS Message;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SELECT 'ERROR' AS Result, ERROR_MESSAGE() AS Message;
    END CATCH
END;

-- Procedure to update lesson progress
CREATE PROCEDURE sp_UpdateLessonProgress
    @EnrollmentID INT,
    @LessonID INT,
    @IsCompleted BIT = 0,
    @TimeSpent INT = 0,
    @LastPosition INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Update or insert lesson progress
        MERGE LessonProgress AS target
        USING (SELECT @EnrollmentID AS EnrollmentID, @LessonID AS LessonID) AS source
        ON target.EnrollmentID = source.EnrollmentID AND target.LessonID = source.LessonID
        WHEN MATCHED THEN
            UPDATE SET
                IsCompleted = @IsCompleted,
                CompletedAt = CASE WHEN @IsCompleted = 1 THEN GETDATE() ELSE CompletedAt END,
                TimeSpent = TimeSpent + @TimeSpent,
                LastPosition = @LastPosition,
                UpdatedAt = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (EnrollmentID, LessonID, IsCompleted, CompletedAt, TimeSpent, LastPosition)
            VALUES (@EnrollmentID, @LessonID, @IsCompleted,
                    CASE WHEN @IsCompleted = 1 THEN GETDATE() ELSE NULL END,
                    @TimeSpent, @LastPosition);

        -- Update enrollment progress
        DECLARE @TotalLessons INT, @CompletedLessons INT, @ProgressPercentage DECIMAL(5,2);

        SELECT @TotalLessons = COUNT(*)
        FROM Enrollments e
        INNER JOIN Courses c ON e.CourseID = c.CourseID
        INNER JOIN Lessons l ON c.CourseID = l.CourseID
        WHERE e.EnrollmentID = @EnrollmentID AND l.IsPublished = 1;

        SELECT @CompletedLessons = COUNT(*)
        FROM LessonProgress lp
        WHERE lp.EnrollmentID = @EnrollmentID AND lp.IsCompleted = 1;

        SET @ProgressPercentage = CASE WHEN @TotalLessons > 0 THEN (@CompletedLessons * 100.0 / @TotalLessons) ELSE 0 END;

        UPDATE Enrollments
        SET ProgressPercentage = @ProgressPercentage,
            TotalTimeSpent = TotalTimeSpent + @TimeSpent,
            LastAccessedAt = GETDATE(),
            CompletedAt = CASE WHEN @ProgressPercentage = 100 THEN GETDATE() ELSE NULL END
        WHERE EnrollmentID = @EnrollmentID;

        COMMIT TRANSACTION;

        SELECT 'SUCCESS' AS Result, 'Progress updated successfully' AS Message, @ProgressPercentage AS NewProgress;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SELECT 'ERROR' AS Result, ERROR_MESSAGE() AS Message;
    END CATCH
END;

GO