CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    CourseId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'MoMo',
    TransactionId NVARCHAR(100) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Completed, Failed
    CreatedAt DATETIME DEFAULT GETDATE(),
    PaidAt DATETIME NULL,
    MoMoOrderId NVARCHAR(50) NULL,
    MoMoRequestId NVARCHAR(50) NULL,
    CONSTRAINT FK_Payments_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Payments_Course FOREIGN KEY (CourseId) REFERENCES Courses(CourseId)
);