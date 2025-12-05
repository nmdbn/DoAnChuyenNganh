-- =============================================
-- NEWS SYSTEM MIGRATION
-- =============================================
-- This script creates the News System tables
-- Run this script after the main database setup
-- =============================================

USE DoAnChuyenNganh;
GO

-- News Categories Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NewsCategories]') AND type in (N'U'))
BEGIN
    CREATE TABLE NewsCategories (
        CategoryID INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName NVARCHAR(100) NOT NULL UNIQUE,
        Description NVARCHAR(300),
        IconUrl NVARCHAR(400),
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    );

    -- Create index for active categories
    CREATE INDEX IX_NewsCategories_IsActive ON NewsCategories(IsActive);
    
    PRINT 'NewsCategories table created successfully';
END
ELSE
BEGIN
    PRINT 'NewsCategories table already exists';
END
GO

-- News Articles Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NewsArticles]') AND type in (N'U'))
BEGIN
    CREATE TABLE NewsArticles (
        ArticleID INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(300) NOT NULL,
        Slug VARCHAR(350) NOT NULL UNIQUE,
        Content NVARCHAR(MAX) NOT NULL,
        Excerpt NVARCHAR(500),
        FeaturedImageUrl NVARCHAR(400),
        CategoryID INT NOT NULL,
        AuthorID INT NOT NULL,
        IsPublished BIT DEFAULT 0,
        IsFeatured BIT DEFAULT 0,
        ViewCount INT DEFAULT 0,
        PublishedAt DATETIME2,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedBy INT,
        Tags NVARCHAR(500),
        
        CONSTRAINT FK_NewsArticles_Category FOREIGN KEY (CategoryID) REFERENCES NewsCategories(CategoryID),
        CONSTRAINT FK_NewsArticles_Author FOREIGN KEY (AuthorID) REFERENCES Users(UserID),
        CONSTRAINT FK_NewsArticles_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(UserID)
    );

    -- Create indexes for better query performance
    CREATE INDEX IX_NewsArticles_CategoryID ON NewsArticles(CategoryID);
    CREATE INDEX IX_NewsArticles_AuthorID ON NewsArticles(AuthorID);
    CREATE INDEX IX_NewsArticles_IsPublished ON NewsArticles(IsPublished);
    CREATE INDEX IX_NewsArticles_IsFeatured ON NewsArticles(IsFeatured);
    CREATE INDEX IX_NewsArticles_PublishedAt ON NewsArticles(PublishedAt DESC);
    CREATE INDEX IX_NewsArticles_ViewCount ON NewsArticles(ViewCount DESC);
    CREATE INDEX IX_NewsArticles_CreatedAt ON NewsArticles(CreatedAt DESC);
    
    PRINT 'NewsArticles table created successfully';
END
ELSE
BEGIN
    PRINT 'NewsArticles table already exists';
END
GO

-- Insert default news categories
IF NOT EXISTS (SELECT * FROM NewsCategories)
BEGIN
    INSERT INTO NewsCategories (CategoryName, Description, IsActive) VALUES
    (N'General News', N'General news and announcements', 1),
    (N'Updates', N'System and feature updates', 1),
    (N'Events', N'Upcoming events and activities', 1),
    (N'Tutorials', N'Educational tutorials and guides', 1),
    (N'Community', N'Community highlights and stories', 1);
    
    PRINT 'Default news categories inserted successfully';
END
GO

PRINT 'News System Migration completed successfully!';
GO

