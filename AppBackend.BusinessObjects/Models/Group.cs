using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class Group
{
    public int GroupId { get; set; }

    public int ClassId { get; set; }

    public string GroupName { get; set; } = null!;

    public string? Description { get; set; }

    public int? LeaderId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual User? Leader { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
