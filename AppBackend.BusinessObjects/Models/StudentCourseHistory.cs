using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// Tracks student progress and completion status for IoT course
/// Each student should have only one record with IsCurrent = true
/// </summary>
[Table("Student_Course_History")]
[Index("StudentId", Name = "IX_StudentCourseHistory_Student")]
[Index("StudentId", "IsCurrent", Name = "IX_StudentCourseHistory_IsCurrent")]
[Index("Status", Name = "IX_StudentCourseHistory_Status")]
[Index("SemesterId", Name = "IX_StudentCourseHistory_Semester")]
public partial class StudentCourseHistory
{
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Required]
    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("semester_id")]
    public int? SemesterId { get; set; }

    /// <summary>
    /// Current status: "Not Started", "In Progress", "Pass", "Not Pass", "Withdrawn"
    /// </summary>
    [Column("status")]
    [StringLength(50)]
    public string? Status { get; set; }

    [Column("final_submission_id")]
    public int? FinalSubmissionId { get; set; }

    /// <summary>
    /// Average grade from all graders
    /// </summary>
    [Column("final_grade")]
    [Precision(5, 2)]
    public decimal? FinalGrade { get; set; }

    /// <summary>
    /// Average grade from other instructors (excluding primary instructor)
    /// </summary>
    [Column("average_grade_from_other_instructors")]
    [Precision(5, 2)]
    public decimal? AverageGradeFromOtherInstructors { get; set; }

    [Column("evaluated_at")]
    [Precision(0)]
    public DateTime? EvaluatedAt { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// When status changed to Pass/Not Pass
    /// </summary>
    [Column("completed_at")]
    [Precision(0)]
    public DateTime? CompletedAt { get; set; }

    [Column("is_retake")]
    public bool? IsRetake { get; set; }

    /// <summary>
    /// Only one record per student should have this as true
    /// </summary>
    [Column("is_current")]
    public bool? IsCurrent { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("StudentId")]
    [InverseProperty("StudentCourseHistories")]
    public virtual User Student { get; set; } = null!;

    [ForeignKey("SemesterId")]
    [InverseProperty("StudentCourseHistories")]
    public virtual Semester? Semester { get; set; }

    [ForeignKey("FinalSubmissionId")]
    [InverseProperty("StudentCourseHistories")]
    public virtual FinalProjectSubmission? FinalSubmission { get; set; }
}
