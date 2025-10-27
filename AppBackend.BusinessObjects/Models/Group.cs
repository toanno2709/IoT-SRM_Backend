using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Index("ClassId", "GroupName", Name = "uq_group_class_name", IsUnique = true)]
public partial class Group
{
    [Key]
    [Column("group_id")]
    public int GroupId { get; set; }

    [Column("class_id")]
    public int ClassId { get; set; }

    [Column("group_name")]
    [StringLength(255)]
    public string GroupName { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("leader_id")]
    public int? LeaderId { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("Groups")]
    public virtual Class Class { get; set; } = null!;

    [InverseProperty("Group")]
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    [ForeignKey("LeaderId")]
    [InverseProperty("Groups")]
    public virtual User? Leader { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
