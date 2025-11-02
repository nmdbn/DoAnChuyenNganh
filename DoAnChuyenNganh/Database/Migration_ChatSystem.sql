-- Create ChatAttachments table for file uploads in chat
CREATE TABLE ChatAttachments (
    AttachmentID INT IDENTITY(1,1) PRIMARY KEY,
    MessageID INT NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    OriginalFileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    FileType VARCHAR(10) NOT NULL, -- 'image' or 'file'
    MimeType VARCHAR(100) NOT NULL,
    FileSize BIGINT NOT NULL, -- in bytes
    UploadedBy INT NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_ChatAttachments_Message FOREIGN KEY (MessageID) REFERENCES PrivateMessages(MessageID) ON DELETE CASCADE,
    CONSTRAINT FK_ChatAttachments_User FOREIGN KEY (UploadedBy) REFERENCES Users(UserID)
);

-- Add indexes for better performance
CREATE INDEX IX_ChatAttachments_MessageID ON ChatAttachments(MessageID);
CREATE INDEX IX_ChatAttachments_UploadedBy ON ChatAttachments(UploadedBy);

-- Add indexes to PrivateMessages for better query performance
CREATE INDEX IX_PrivateMessages_SenderID ON PrivateMessages(SenderID);
CREATE INDEX IX_PrivateMessages_RecipientID ON PrivateMessages(RecipientID);
CREATE INDEX IX_PrivateMessages_CreatedAt ON PrivateMessages(CreatedAt DESC);
CREATE INDEX IX_PrivateMessages_IsRead ON PrivateMessages(IsRead);

-- Create a composite index for conversation queries
CREATE INDEX IX_PrivateMessages_Conversation ON PrivateMessages(SenderID, RecipientID, CreatedAt DESC);

-- =============================================
-- VIEWS FOR CHAT SYSTEM
-- =============================================

-- View to get conversation list with last message
CREATE VIEW vw_ChatConversations AS
WITH LastMessages AS (
    SELECT 
        CASE 
            WHEN SenderID < RecipientID THEN CAST(SenderID AS VARCHAR) + '_' + CAST(RecipientID AS VARCHAR)
            ELSE CAST(RecipientID AS VARCHAR) + '_' + CAST(SenderID AS VARCHAR)
        END AS ConversationKey,
        SenderID,
        RecipientID,
        MessageContent,
        IsRead,
        CreatedAt,
        ROW_NUMBER() OVER (
            PARTITION BY 
                CASE 
                    WHEN SenderID < RecipientID THEN CAST(SenderID AS VARCHAR) + '_' + CAST(RecipientID AS VARCHAR)
                    ELSE CAST(RecipientID AS VARCHAR) + '_' + CAST(SenderID AS VARCHAR)
                END 
            ORDER BY CreatedAt DESC
        ) AS RowNum
    FROM PrivateMessages
    WHERE IsDeleted = 0
)
SELECT 
    ConversationKey,
    SenderID,
    RecipientID,
    MessageContent AS LastMessage,
    IsRead AS LastMessageIsRead,
    CreatedAt AS LastMessageAt
FROM LastMessages
WHERE RowNum = 1;

GO

-- View to get unread message counts per user
CREATE VIEW vw_UnreadMessageCounts AS
SELECT 
    RecipientID AS UserID,
    SenderID AS FromUserID,
    COUNT(*) AS UnreadCount
FROM PrivateMessages
WHERE IsRead = 0 AND IsDeleted = 0
GROUP BY RecipientID, SenderID;

GO

-- =============================================
-- STORED PROCEDURES FOR CHAT OPERATIONS
-- =============================================

-- Procedure to get conversation messages with pagination
CREATE PROCEDURE sp_GetConversationMessages
    @UserID INT,
    @OtherUserID INT,
    @PageNumber INT = 1,
    @PageSize INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        pm.MessageID,
        pm.SenderID,
        pm.RecipientID,
        pm.MessageContent,
        pm.IsRead,
        pm.ReadAt,
        pm.CreatedAt,
        s.Username AS SenderUsername,
        s.FirstName AS SenderFirstName,
        s.LastName AS SenderLastName,
        s.AvatarUrl AS SenderAvatarUrl,
        r.Username AS RecipientUsername,
        r.FirstName AS RecipientFirstName,
        r.LastName AS RecipientLastName,
        r.AvatarUrl AS RecipientAvatarUrl
    FROM PrivateMessages pm
    INNER JOIN Users s ON pm.SenderID = s.UserID
    INNER JOIN Users r ON pm.RecipientID = r.UserID
    WHERE 
        ((pm.SenderID = @UserID AND pm.RecipientID = @OtherUserID) OR 
         (pm.SenderID = @OtherUserID AND pm.RecipientID = @UserID))
        AND pm.IsDeleted = 0
    ORDER BY pm.CreatedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;

GO

-- Procedure to mark messages as read
CREATE PROCEDURE sp_MarkMessagesAsRead
    @RecipientID INT,
    @SenderID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE PrivateMessages
    SET IsRead = 1, ReadAt = GETDATE()
    WHERE RecipientID = @RecipientID 
        AND SenderID = @SenderID 
        AND IsRead = 0
        AND IsDeleted = 0;
    
    SELECT @@ROWCOUNT AS UpdatedCount;
END;

GO

-- Procedure to get user's conversation list
CREATE PROCEDURE sp_GetUserConversations
    @UserID INT,
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH ConversationMessages AS (
        SELECT 
            CASE 
                WHEN pm.SenderID = @UserID THEN pm.RecipientID
                ELSE pm.SenderID
            END AS OtherUserID,
            pm.MessageID,
            pm.SenderID,
            pm.RecipientID,
            pm.MessageContent,
            pm.IsRead,
            pm.CreatedAt,
            ROW_NUMBER() OVER (
                PARTITION BY 
                    CASE 
                        WHEN pm.SenderID = @UserID THEN pm.RecipientID
                        ELSE pm.SenderID
                    END
                ORDER BY pm.CreatedAt DESC
            ) AS RowNum
        FROM PrivateMessages pm
        WHERE (pm.SenderID = @UserID OR pm.RecipientID = @UserID)
            AND pm.IsDeleted = 0
    ),
    UnreadCounts AS (
        SELECT 
            SenderID,
            COUNT(*) AS UnreadCount
        FROM PrivateMessages
        WHERE RecipientID = @UserID 
            AND IsRead = 0 
            AND IsDeleted = 0
        GROUP BY SenderID
    )
    SELECT 
        cm.OtherUserID,
        u.Username,
        u.FirstName,
        u.LastName,
        u.AvatarUrl,
        cm.MessageContent AS LastMessage,
        cm.SenderID AS LastMessageSenderID,
        cm.IsRead AS LastMessageIsRead,
        cm.CreatedAt AS LastMessageAt,
        ISNULL(uc.UnreadCount, 0) AS UnreadCount
    FROM ConversationMessages cm
    INNER JOIN Users u ON cm.OtherUserID = u.UserID
    LEFT JOIN UnreadCounts uc ON uc.SenderID = cm.OtherUserID
    WHERE cm.RowNum = 1
    ORDER BY cm.CreatedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;

GO

-- =============================================
-- SAMPLE DATA (Optional - for testing)
-- =============================================

-- You can add sample chat messages here for testing
-- Example:
-- INSERT INTO PrivateMessages (SenderID, RecipientID, MessageContent, IsRead, CreatedAt)
-- VALUES (1, 2, 'Hello! How are you?', 0, GETDATE());

PRINT 'Chat System Migration completed successfully!';

