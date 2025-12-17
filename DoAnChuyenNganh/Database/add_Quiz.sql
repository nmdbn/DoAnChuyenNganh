-- =============================================
-- QUIZ SYSTEM TABLES
-- =============================================

-- Bảng Quiz - Chứa thông tin bài quiz
CREATE TABLE Quiz (
    QuizID INT IDENTITY(1,1) PRIMARY KEY,
    LessonID INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    IsRandomOrder BIT DEFAULT 0, -- Trộn thứ tự câu hỏi
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    UpdatedBy INT,
    
    CONSTRAINT FK_Quiz_Lesson FOREIGN KEY (LessonID) REFERENCES Lessons(LessonID) ON DELETE CASCADE,
    CONSTRAINT FK_Quiz_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserID),
    CONSTRAINT FK_Quiz_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(UserID),
    CONSTRAINT UQ_Quiz_Lesson UNIQUE (LessonID) -- Mỗi lesson chỉ có 1 quiz
);

-- Bảng QuizQuestion - Câu hỏi trong quiz
CREATE TABLE QuizQuestion (
    QuestionID INT IDENTITY(1,1) PRIMARY KEY,
    QuizID INT NOT NULL,
    QuestionText NVARCHAR(MAX) NOT NULL,
    QuestionType NVARCHAR(20) NOT NULL, -- 'multiple_choice' hoặc 'essay'
    QuestionOrder INT NOT NULL,
    Points INT DEFAULT 10, -- Điểm của câu hỏi
    Explanation NVARCHAR(MAX), -- Giải thích đáp án (hiển thị sau khi submit)
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_QuizQuestion_Quiz FOREIGN KEY (QuizID) REFERENCES Quiz(QuizID) ON DELETE CASCADE,
    CONSTRAINT CK_QuizQuestion_Type CHECK (QuestionType IN ('multiple_choice', 'essay')),
    CONSTRAINT UQ_QuizQuestion_Order UNIQUE (QuizID, QuestionOrder)
);

-- Bảng QuizAnswer - Đáp án cho câu hỏi trắc nghiệm (tối đa 4 đáp án, 1 đúng)
CREATE TABLE QuizAnswer (
    AnswerID INT IDENTITY(1,1) PRIMARY KEY,
    QuestionID INT NOT NULL,
    AnswerText NVARCHAR(MAX) NOT NULL,
    IsCorrect BIT DEFAULT 0, -- Chỉ 1 đáp án đúng
    AnswerOrder TINYINT NOT NULL CHECK (AnswerOrder BETWEEN 1 AND 4),
    
    CONSTRAINT FK_QuizAnswer_Question FOREIGN KEY (QuestionID) REFERENCES QuizQuestion(QuestionID) ON DELETE CASCADE,
    CONSTRAINT UQ_QuizAnswer_Order UNIQUE (QuestionID, AnswerOrder)
);

-- Bảng UserQuizAttempt - Lưu lần làm quiz của user
CREATE TABLE UserQuizAttempt (
    AttemptID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    QuizID INT NOT NULL,
    EnrollmentID INT NOT NULL, -- Liên kết với enrollment
    StartedAt DATETIME2 DEFAULT GETDATE(),
    SubmittedAt DATETIME2,
    TotalScore DECIMAL(5,2) DEFAULT 0, -- Điểm tổng
    MaxScore INT DEFAULT 0, -- Điểm tối đa
    PercentageScore DECIMAL(5,2) DEFAULT 0, -- Phần trăm điểm
    Status NVARCHAR(20) DEFAULT 'in_progress', -- 'in_progress', 'submitted', 'graded'
    
    CONSTRAINT FK_UserQuizAttempt_User FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_UserQuizAttempt_Quiz FOREIGN KEY (QuizID) REFERENCES Quiz(QuizID),
    CONSTRAINT FK_UserQuizAttempt_Enrollment FOREIGN KEY (EnrollmentID) REFERENCES Enrollments(EnrollmentID) ON DELETE CASCADE,
    CONSTRAINT CK_UserQuizAttempt_Status CHECK (Status IN ('in_progress', 'submitted', 'graded'))
);

-- Bảng UserQuizAnswer - Câu trả lời của user cho từng câu hỏi
CREATE TABLE UserQuizAnswer (
    UserAnswerID INT IDENTITY(1,1) PRIMARY KEY,
    AttemptID INT NOT NULL,
    QuestionID INT NOT NULL,
    SelectedAnswerID INT, -- NULL nếu là essay
    EssayAnswer NVARCHAR(MAX), -- Câu trả lời tự luận
    IsCorrect BIT, -- NULL cho essay (chưa chấm), TRUE/FALSE cho trắc nghiệm
    EarnedPoints DECIMAL(5,2) DEFAULT 0, -- Điểm nhận được
    TeacherFeedback NVARCHAR(MAX), -- Nhận xét của giáo viên (cho essay)
    GradedBy INT, -- Giáo viên chấm bài (cho essay)
    GradedAt DATETIME2, -- Thời gian chấm
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_UserQuizAnswer_Attempt FOREIGN KEY (AttemptID) REFERENCES UserQuizAttempt(AttemptID) ON DELETE CASCADE,
    CONSTRAINT FK_UserQuizAnswer_Question FOREIGN KEY (QuestionID) REFERENCES QuizQuestion(QuestionID),
    CONSTRAINT FK_UserQuizAnswer_SelectedAnswer FOREIGN KEY (SelectedAnswerID) REFERENCES QuizAnswer(AnswerID),
    CONSTRAINT FK_UserQuizAnswer_GradedBy FOREIGN KEY (GradedBy) REFERENCES Users(UserID),
    CONSTRAINT UQ_UserQuizAnswer_AttemptQuestion UNIQUE (AttemptID, QuestionID)
);

-- =============================================
-- CREATE INDEXES
-- =============================================

CREATE INDEX IX_Quiz_LessonID ON Quiz(LessonID);
CREATE INDEX IX_QuizQuestion_QuizID ON QuizQuestion(QuizID);
CREATE INDEX IX_QuizAnswer_QuestionID ON QuizAnswer(QuestionID);
CREATE INDEX IX_UserQuizAttempt_UserID ON UserQuizAttempt(UserID);
CREATE INDEX IX_UserQuizAttempt_QuizID ON UserQuizAttempt(QuizID);
CREATE INDEX IX_UserQuizAttempt_EnrollmentID ON UserQuizAttempt(EnrollmentID);
CREATE INDEX IX_UserQuizAttempt_Status ON UserQuizAttempt(Status);
CREATE INDEX IX_UserQuizAnswer_AttemptID ON UserQuizAnswer(AttemptID);
CREATE INDEX IX_UserQuizAnswer_QuestionID ON UserQuizAnswer(QuestionID);


ALTER TABLE QuizAnswer
DROP CONSTRAINT IF EXISTS CK_QuizAnswer_AnswerOrder;

-- 2. Add new constraint (1-5 answers)
ALTER TABLE QuizAnswer
ADD CONSTRAINT CK_QuizAnswer_AnswerOrder CHECK (AnswerOrder BETWEEN 1 AND 5);

-- 3. Update UserQuizAnswer to support multiple selections
-- Add new column to track multiple selections
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserQuizAnswer') AND name = 'SelectedAnswerIds')
BEGIN
    ALTER TABLE UserQuizAnswer
    ADD SelectedAnswerIds NVARCHAR(100); -- Format: "1,3,5" (comma-separated)
END

-- 4. Add index for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_QuizAnswer_QuestionID_Order')
BEGIN
    CREATE INDEX IX_QuizAnswer_QuestionID_Order ON QuizAnswer(QuestionID, AnswerOrder);
END


-- 1. Thêm cột AllowMultipleCorrect vào QuizQuestion
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('QuizQuestion') 
    AND name = 'AllowMultipleCorrect'
)
BEGIN
    ALTER TABLE QuizQuestion
    ADD AllowMultipleCorrect BIT NOT NULL DEFAULT 0;
    
    PRINT 'Added AllowMultipleCorrect column to QuizQuestion';
END
GO

-- 2. Update existing questions: Đánh dấu những câu có >= 2 đáp án đúng
UPDATE q
SET q.AllowMultipleCorrect = 1
FROM QuizQuestion q
WHERE (
    SELECT COUNT(*) 
    FROM QuizAnswer a 
    WHERE a.QuestionID = q.QuestionID 
    AND a.IsCorrect = 1
) >= 2;

PRINT 'Updated AllowMultipleCorrect for existing questions';
GO

-- 3. Thêm cột SelectedAnswerIds vào UserQuizAnswer (nếu chưa có)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('UserQuizAnswer') 
    AND name = 'SelectedAnswerIds'
)
BEGIN
    ALTER TABLE UserQuizAnswer
    ADD SelectedAnswerIds NVARCHAR(100); -- Format: "1,3,5" (comma-separated)
    
    PRINT 'Added SelectedAnswerIds column to UserQuizAnswer';
END
GO

-- 4. Migrate dữ liệu cũ từ SelectedAnswerID sang SelectedAnswerIds
UPDATE UserQuizAnswer
SET SelectedAnswerIds = CAST(SelectedAnswerID AS NVARCHAR(100))
WHERE SelectedAnswerID IS NOT NULL 
AND (SelectedAnswerIds IS NULL OR SelectedAnswerIds = '');

PRINT 'Migrated SelectedAnswerID to SelectedAnswerIds';
GO

-- 5. Tạo index mới
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_QuizQuestion_QuizID_Order' 
    AND object_id = OBJECT_ID('QuizQuestion')
)
BEGIN
    CREATE INDEX IX_QuizQuestion_QuizID_Order 
    ON QuizQuestion(QuizID, QuestionOrder);
    
    PRINT 'Created index IX_QuizQuestion_QuizID_Order';
END
GO

-- 6. Kiểm tra dữ liệu
SELECT 
    q.QuestionID,
    q.QuizID,
    q.QuestionOrder,
    q.QuestionText,
    q.AllowMultipleCorrect,
    COUNT(CASE WHEN a.IsCorrect = 1 THEN 1 END) as CorrectAnswersCount
FROM QuizQuestion q
LEFT JOIN QuizAnswer a ON q.QuestionID = a.QuestionID
GROUP BY q.QuestionID, q.QuizID, q.QuestionOrder, q.QuestionText, q.AllowMultipleCorrect
ORDER BY q.QuizID, q.QuestionOrder;

PRINT 'Migration completed successfully!';
GO