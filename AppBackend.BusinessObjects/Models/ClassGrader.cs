using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// Represents an instructor assigned to grade projects in a class
/// Allows multiple instructors to be assigned as graders for a class
/// </summary>
[Table("Class_Graders")]
[Index("ClassId", "InstructorId", Name = "UX_ClassGraders_Class_Instructor", IsUnique = true)]
public partial class ClassGrader
{
    [Key]
    [Column("grader_id")]
    public int GraderId { get; set; }

    [Column("class_id")]
    public int ClassId { get; set; }

    [Column("instructor_id")]
    public int InstructorId { get; set; }

    [Column("assigned_at")]
    [Precision(0)]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("assigned_by")]
    public int? AssignedBy { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey("ClassId")]
    [InverseProperty("ClassGraders")]
    public virtual Class Class { get; set; } = null!;

    [ForeignKey("InstructorId")]
    [InverseProperty("ClassGradersAsInstructor")]
    public virtual User Instructor { get; set; } = null!;

    [ForeignKey("AssignedBy")]
    [InverseProperty("ClassGradersAsAssigner")]
    public virtual User? AssignedByNavigation { get; set; }
}
