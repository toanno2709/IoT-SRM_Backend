using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// Represents individual grades from instructors for a final submission
/// Supports multiple instructors grading the same submission
/// The average is calculated and stored in FinalProjectSubmission
/// </summary>
[Table("Final_Submission_Grades")]
[Index("FinalSubmissionId", "InstructorId", Name = "UX_FinalGrades_Submission_Instructor", IsUnique = true)]
public partial class FinalSubmissionGrade
{
    [Key]
    [Column("grade_id")]
    public int GradeId { get; set; }

    [Column("final_submission_id")]
    public int FinalSubmissionId { get; set; }

    [Column("instructor_id")]
    public int InstructorId { get; set; }

    [Column("grade")]
    [Precision(5, 2)]
    public decimal Grade { get; set; }

    [Column("feedback")]
    public string? Feedback { get; set; }

    [Column("graded_at")]
    [Precision(0)]
    public DateTime GradedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("FinalSubmissionId")]
    [InverseProperty("FinalSubmissionGrades")]
    public virtual FinalProjectSubmission FinalSubmission { get; set; } = null!;

    [ForeignKey("InstructorId")]
    [InverseProperty("FinalSubmissionGrades")]
    public virtual User Instructor { get; set; } = null!;
}
