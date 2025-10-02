using System;
using System.Collections.Generic;

namespace DoAnChuyenNganh.Models;

public partial class PrivateMessage
{
    public int MessageId { get; set; }

    public int SenderId { get; set; }

    public int RecipientId { get; set; }

    public string? Subject { get; set; }

    public string MessageContent { get; set; } = null!;

    public bool? IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public bool? IsDeleted { get; set; }

    public int? DeletedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? DeletedByNavigation { get; set; }

    public virtual User Recipient { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
