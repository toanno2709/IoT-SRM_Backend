using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Group_Members")]
[Index("GroupId", "UserId", Name = "uq_group_user", IsUnique = true)]
public partial class GroupMember
{
    [Key]
    [Column("gm_id")]
    public int GmId { get; set; }

    [Column("group_id")]
    public int GroupId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("role_in_group")]
    [StringLength(50)]
    public string? RoleInGroup { get; set; }

    [Column("joined_at")]
    [Precision(0)]
    public DateTime? JoinedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("GroupMembers")]
    public virtual Group Group { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("GroupMembers")]
    public virtual User User { get; set; } = null!;
}


