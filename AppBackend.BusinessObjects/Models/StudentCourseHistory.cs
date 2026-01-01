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
public partial class StudentCourseHistory
{
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Required]
    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("class_id")]
    public int? ClassId { get; set; }

    /// <summary>
    /// Current status: "Not Started", "In Progress", "Pass", "Not Pass", "Withdrawn"
    /// </summary>
    [Required]
    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "Not Started";

    [Column("final_submission_id")]
    public int? FinalSubmissionId { get; set; }

    /// <summary>
    /// Average grade from all graders
    /// </summary>
    [Column("final_grade")]
    [Precision(5, 2)]
    public decimal? FinalGrade { get; set; }

    [Column("evaluated_at")]
    [Precision(0)]
    public DateTime? EvaluatedAt { get; set; }

    [Column("evaluated_by")]
    public int? EvaluatedBy { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// When status changed to Pass/Not Pass
    /// </summary>
    [Column("completed_at")]
    [Precision(0)]
    public DateTime? CompletedAt { get; set; }

    [Required]
    [Column("is_retake")]
    public bool IsRetake { get; set; } = false;

    /// <summary>
    /// Only one record per student should have this as true
    /// </summary>
    [Required]
    [Column("is_current")]
    public bool IsCurrent { get; set; } = true;

    [Required]
    [Column("created_at")]
    [Precision(0)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    [Precision(0)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("StudentId")]
    [InverseProperty("StudentCourseHistories")]
    public virtual User Student { get; set; } = null!;

    [ForeignKey("ClassId")]
    [InverseProperty("StudentCourseHistories")]
    public virtual Class? Class { get; set; }

    [ForeignKey("FinalSubmissionId")]
    [InverseProperty("StudentCourseHistories")]
    public virtual FinalProjectSubmission? FinalSubmission { get; set; }

    [ForeignKey("EvaluatedBy")]
    [InverseProperty("StudentCourseHistoriesEvaluated")]
    public virtual User? EvaluatedByUser { get; set; }
}
