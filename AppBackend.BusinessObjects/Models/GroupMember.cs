using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class GroupMember
{
    public int GmId { get; set; }

    public int GroupId { get; set; }

    public int UserId { get; set; }

    public string? RoleInGroup { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
