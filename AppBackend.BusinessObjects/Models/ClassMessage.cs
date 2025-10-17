using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class ClassMessage
{
    public int MessageId { get; set; }

    public int ClassId { get; set; }

    public int SenderId { get; set; }

    public string? Content { get; set; }

    public DateTime SentAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
