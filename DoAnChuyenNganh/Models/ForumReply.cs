using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class ForumReply
{
    public int ReplyId { get; set; }

    public int PostId { get; set; }

    public int UserId { get; set; }

    public int? ParentReplyId { get; set; }

    public string Content { get; set; } = null!;

    public int? LikeCount { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual ICollection<ForumReply> InverseParentReply { get; set; } = new List<ForumReply>();

    public virtual ForumReply? ParentReply { get; set; }

    public virtual ForumPost Post { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
