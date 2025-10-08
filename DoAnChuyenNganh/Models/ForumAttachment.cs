using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class ForumAttachment
{
    public int AttachmentId { get; set; }

    public int? PostId { get; set; }

    public int? ReplyId { get; set; }

    public string FileName { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long FileSize { get; set; }

    public int UploadedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ForumPost? Post { get; set; }

    public virtual ForumReply? Reply { get; set; }

    public virtual User UploadedByNavigation { get; set; } = null!;
}

