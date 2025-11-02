using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// Class Configuration - Allows instructors to configure class settings
/// </summary>
[Table("Class_Configurations")]
public class ClassConfiguration
{
    [Key]
    [Column("config_id")]
    public int ConfigId { get; set; }

    [Required]
    [Column("class_id")]
    public int ClassId { get; set; }

    [Required]
    [Column("max_groups_allowed")]
    [Range(1, 100, ErrorMessage = "Max groups allowed must be between 1 and 100")]
    public int MaxGroupsAllowed { get; set; } = 20;

    [Required]
    [Column("min_members_per_group")]
    [Range(1, 10, ErrorMessage = "Min members per group must be between 1 and 10")]
    public int MinMembersPerGroup { get; set; } = 2;

    [Required]
    [Column("max_members_per_group")]
    [Range(1, 20, ErrorMessage = "Max members per group must be between 1 and 20")]
    public int MaxMembersPerGroup { get; set; } = 5;

    [Column("group_formation_deadline")]
    public DateTime? GroupFormationDeadline { get; set; }

    [Required]
    [Column("allow_student_create_group")]
    public bool AllowStudentCreateGroup { get; set; } = true;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(ClassId))]
    public virtual Class? Class { get; set; }

    /// <summary>
    /// Validates that max members is greater than or equal to min members
    /// </summary>
    public bool IsValid()
    {
        return MaxMembersPerGroup >= MinMembersPerGroup && 
               MaxGroupsAllowed > 0 && 
               MinMembersPerGroup >= 1;
    }

    /// <summary>
    /// Checks if group formation is still allowed based on deadline
    /// </summary>
    public bool IsGroupFormationAllowed()
    {
        if (GroupFormationDeadline == null)
            return true;

        return DateTime.UtcNow <= GroupFormationDeadline.Value;
    }
}
