-- Create ForumAttachments table
CREATE TABLE ForumAttachments (
    AttachmentID INT IDENTITY(1,1) PRIMARY KEY,
    PostID INT NULL,
    ReplyID INT NULL,
    FileName NVARCHAR(255) NOT NULL,
    OriginalFileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    FileType VARCHAR(10) NOT NULL, -- 'image' or 'file'
    MimeType VARCHAR(100) NOT NULL,
    FileSize BIGINT NOT NULL, -- in bytes
    UploadedBy INT NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_ForumAttachments_Post FOREIGN KEY (PostID) REFERENCES ForumPosts(PostID) ON DELETE CASCADE,
    CONSTRAINT FK_ForumAttachments_Reply FOREIGN KEY (ReplyID) REFERENCES ForumReplies(ReplyID) ON DELETE CASCADE,
    CONSTRAINT FK_ForumAttachments_User FOREIGN KEY (UploadedBy) REFERENCES Users(UserID),
    CONSTRAINT CHK_ForumAttachments_PostOrReply CHECK (
        (PostID IS NOT NULL AND ReplyID IS NULL) OR 
        (PostID IS NULL AND ReplyID IS NOT NULL)
    )
);

-- Create indexes for better performance
CREATE INDEX IX_ForumAttachments_PostID ON ForumAttachments(PostID);
CREATE INDEX IX_ForumAttachments_ReplyID ON ForumAttachments(ReplyID);
CREATE INDEX IX_ForumAttachments_UploadedBy ON ForumAttachments(UploadedBy);
CREATE INDEX IX_ForumAttachments_CreatedAt ON ForumAttachments(CreatedAt);

-- Add comment
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Stores file and image attachments for forum posts and replies', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'ForumAttachments';

GO

