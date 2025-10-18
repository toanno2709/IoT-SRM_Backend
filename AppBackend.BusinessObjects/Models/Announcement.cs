using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class Announcement
{
    public int AnnouncementId { get; set; }

    public int? AdminId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public string? TargetAudience { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Admin { get; set; }
}
