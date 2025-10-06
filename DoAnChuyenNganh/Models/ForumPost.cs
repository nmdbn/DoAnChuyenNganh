using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class ForumPost
{
    public int PostId { get; set; }

    public short CategoryId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool? IsSticky { get; set; }

    public bool? IsLocked { get; set; }

    public int? ViewCount { get; set; }

    public int? ReplyCount { get; set; }

    public int? LikeCount { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ForumCategory Category { get; set; } = null!;

    public virtual ICollection<ForumAttachment> ForumAttachments { get; set; } = new List<ForumAttachment>();

    public virtual ICollection<ForumReply> ForumReplies { get; set; } = new List<ForumReply>();

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
