using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class Class
{
    public int ClassId { get; set; }

    public string? ClassName { get; set; }

    public int? InstructorId { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? SemesterId { get; set; }

    [Column("max_groups")]
    public int? MaxGroups { get; set; }

    [Column("max_members_per_group")]
    public int? MaxMembersPerGroup { get; set; }

    [Column("min_members_per_group")]
    public int? MinMembersPerGroup { get; set; }

    [InverseProperty("Class")]
    public virtual ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();

    public virtual ICollection<ClassMessage> ClassMessages { get; set; } = new List<ClassMessage>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual User? Instructor { get; set; }

    [InverseProperty("Class")]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    [InverseProperty("Class")]
    public virtual ICollection<ClassMessage> ClassMessages { get; set; } = new List<ClassMessage>();

    [InverseProperty("Class")]
    public virtual ICollection<RubricWeight> RubricWeights { get; set; } = new List<RubricWeight>();

    public virtual Semester? Semester { get; set; }
}
