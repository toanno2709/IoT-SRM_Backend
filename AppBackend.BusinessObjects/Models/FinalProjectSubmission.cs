using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// Represents the final project submission with all deliverables
/// </summary>
[Table("Final_Project_Submissions")]
[Index("ProjectId", Name = "IX_Final_Submissions_Project", IsUnique = true)]
public partial class FinalProjectSubmission
{
    [Key]
    [Column("final_submission_id")]
    public int FinalSubmissionId { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("final_report_url")]
    [StringLength(500)]
    public string? FinalReportUrl { get; set; }

    [Column("presentation_url")]
    [StringLength(500)]
    public string? PresentationUrl { get; set; }

    [Column("source_code_url")]
    [StringLength(500)]
    public string? SourceCodeUrl { get; set; }

    [Column("video_demo_url")]
    [StringLength(500)]
    public string? VideoDemoUrl { get; set; }

    [Column("repository_url")]
    [StringLength(500)]
    public string? RepositoryUrl { get; set; }

    [Column("submission_notes")]
    public string? SubmissionNotes { get; set; }

    [Column("submitted_by")]
    public int SubmittedBy { get; set; }

    [Column("submitted_at")]
    [Precision(0)]
    public DateTime SubmittedAt { get; set; }

    [Column("last_updated_at")]
    [Precision(0)]
    public DateTime? LastUpdatedAt { get; set; }

    /// <summary>
    /// Average grade calculated from all instructor grades in FinalSubmissionGrades table
    /// </summary>
    [Column("grade")]
    [Precision(5, 2)]
    public decimal? Grade { get; set; }

    [Column("feedback")]
    public string? Feedback { get; set; }

    /// <summary>
    /// Deprecated: Use FinalSubmissionGrades for individual instructor grades
    /// Kept for backward compatibility
    /// </summary>
    [Column("graded_by")]
    public int? GradedBy { get; set; }

    [Column("graded_at")]
    [Precision(0)]
    public DateTime? GradedAt { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "Submitted"; // Submitted, Graded

    // Navigation properties
    [ForeignKey("ProjectId")]
    [InverseProperty("FinalProjectSubmission")]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey("SubmittedBy")]
    [InverseProperty("FinalProjectSubmissionsSubmitted")]
    public virtual User SubmittedByNavigation { get; set; } = null!;

    [ForeignKey("GradedBy")]
    [InverseProperty("FinalProjectSubmissionsGraded")]
    public virtual User? GradedByNavigation { get; set; }

    [InverseProperty("FinalSubmission")]
    public virtual ICollection<FinalSubmissionGrade> FinalSubmissionGrades { get; set; } = new List<FinalSubmissionGrade>();
}
