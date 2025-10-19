USE DoAnChuyenNganh;
GO

-- =============================================
-- Check if Notifications table exists
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
    PRINT 'Creating Notifications table...';
    
    -- Create Notifications Table
    CREATE TABLE Notifications (
        NotificationID INT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NOT NULL,
        Type NVARCHAR(30) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        RelatedItemType NVARCHAR(20),
        RelatedItemID INT,
        IsRead BIT DEFAULT 0,
        ReadAt DATETIME2,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        
        CONSTRAINT FK_Notifications_User FOREIGN KEY (UserID) REFERENCES Users(UserID)
    );
    
    PRINT 'Notifications table created successfully.';
END
ELSE
BEGIN
    PRINT 'Notifications table already exists. Checking for required columns...';
    
    -- Add missing columns if they don't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND name = 'RelatedItemType')
    BEGIN
        ALTER TABLE Notifications ADD RelatedItemType NVARCHAR(20);
        PRINT 'Added RelatedItemType column.';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND name = 'RelatedItemID')
    BEGIN
        ALTER TABLE Notifications ADD RelatedItemID INT;
        PRINT 'Added RelatedItemID column.';
    END
END
GO

-- =============================================
-- Create or Update Indexes for Performance
-- =============================================
PRINT 'Creating/updating indexes...';

-- Index on UserID for fast user notification queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_UserID' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Notifications_UserID ON Notifications(UserID);
    PRINT 'Created index: IX_Notifications_UserID';
END

-- Index on IsRead for filtering unread notifications
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_IsRead' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Notifications_IsRead ON Notifications(IsRead);
    PRINT 'Created index: IX_Notifications_IsRead';
END

-- Index on CreatedAt for sorting by date
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_CreatedAt' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt DESC);
    PRINT 'Created index: IX_Notifications_CreatedAt';
END

-- Composite index for common query pattern (UserID + IsRead + CreatedAt)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_UserID_IsRead_CreatedAt' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Notifications_UserID_IsRead_CreatedAt 
    ON Notifications(UserID, IsRead, CreatedAt DESC);
    PRINT 'Created composite index: IX_Notifications_UserID_IsRead_CreatedAt';
END

-- Index on Type for filtering by notification type
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_Type' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Notifications_Type ON Notifications(Type);
    PRINT 'Created index: IX_Notifications_Type';
END

GO

-- =============================================
-- Update Type Check Constraint (if exists)
-- =============================================
PRINT 'Updating notification type constraints...';

-- Drop old constraint if it exists
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Notifications_Type')
BEGIN
    ALTER TABLE Notifications DROP CONSTRAINT CK_Notifications_Type;
    PRINT 'Dropped old CK_Notifications_Type constraint.';
END

-- Add updated constraint with all notification types
ALTER TABLE Notifications ADD CONSTRAINT CK_Notifications_Type 
CHECK (Type IN (
    'forum_reply',          -- Reply to forum post
    'post_like',            -- Like on forum post
    'reply_like',           -- Like on forum reply
    'mention',              -- User mentioned in content
    'system_alert',         -- System announcement
    'course_enrollment',    -- Course enrollment notification
    'new_lesson',           -- New lesson added
    'course_completion',    -- Course completed
    'new_material'          -- New course material added
));
PRINT 'Added updated CK_Notifications_Type constraint.';

GO

-- =============================================
-- Sample Data (Optional - for testing)
-- =============================================
-- Uncomment the following section to insert sample notifications for testing

/*
PRINT 'Inserting sample notifications...';

-- Get a sample user ID (adjust as needed)
DECLARE @SampleUserID INT = (SELECT TOP 1 UserID FROM Users WHERE IsActive = 1);

IF @SampleUserID IS NOT NULL
BEGIN
    -- Sample forum reply notification
    INSERT INTO Notifications (UserID, Type, Title, Message, RelatedItemType, RelatedItemID, IsRead, CreatedAt)
    VALUES (@SampleUserID, 'forum_reply', 'New Reply to Your Post', 'Someone replied to your post about ASP.NET Core', 'post', 1, 0, GETDATE());
    
    -- Sample post like notification
    INSERT INTO Notifications (UserID, Type, Title, Message, RelatedItemType, RelatedItemID, IsRead, CreatedAt)
    VALUES (@SampleUserID, 'post_like', 'Someone Liked Your Post', 'Your post received a new like', 'post', 1, 0, DATEADD(MINUTE, -30, GETDATE()));
    
    -- Sample mention notification
    INSERT INTO Notifications (UserID, Type, Title, Message, RelatedItemType, RelatedItemID, IsRead, CreatedAt)
    VALUES (@SampleUserID, 'mention', 'You Were Mentioned', 'You were mentioned in a forum discussion', 'post', 2, 0, DATEADD(HOUR, -2, GETDATE()));
    
    -- Sample system alert (read)
    INSERT INTO Notifications (UserID, Type, Title, Message, IsRead, ReadAt, CreatedAt)
    VALUES (@SampleUserID, 'system_alert', 'System Maintenance', 'Scheduled maintenance on Sunday at 2 AM', 1, DATEADD(HOUR, -1, GETDATE()), DATEADD(DAY, -1, GETDATE()));
    
    PRINT 'Sample notifications inserted successfully.';
END
ELSE
BEGIN
    PRINT 'No active users found. Skipping sample data insertion.';
END
*/

GO

-- =============================================
-- Verification Queries
-- =============================================
PRINT '';
PRINT '==============================================';
PRINT 'Migration completed successfully!';
PRINT '==============================================';
PRINT '';
PRINT 'Verification:';
PRINT '-------------';

-- Show table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Notifications'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT 'Indexes:';
PRINT '--------';

-- Show indexes
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    STRING_AGG(c.name, ', ') AS Columns
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('Notifications')
GROUP BY i.name, i.type_desc
ORDER BY i.name;

PRINT '';
PRINT 'Notification count by type:';
PRINT '---------------------------';

-- Show notification statistics
SELECT 
    Type,
    COUNT(*) AS Count,
    SUM(CASE WHEN IsRead = 0 THEN 1 ELSE 0 END) AS UnreadCount
FROM Notifications
GROUP BY Type
ORDER BY Count DESC;

PRINT '';
PRINT '==============================================';
PRINT 'You can now use the notification system!';
PRINT '==============================================';

GO

