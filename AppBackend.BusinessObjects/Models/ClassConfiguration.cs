using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

/// <summary>
/// Class Configuration - Allows instructors to configure class settings including milestone submission and editing deadlines
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

    /// <summary>
    /// Start date for milestone submissions (when students can start submitting)
    /// </summary>
    [Column("submission_start_date")]
    public DateTime? SubmissionStartDate { get; set; }

    /// <summary>
    /// Hard deadline for milestone submissions (after this, submissions are considered late or not allowed)
    /// </summary>
    [Column("submission_deadline")]
    public DateTime? SubmissionDeadline { get; set; }

    /// <summary>
    /// Whether to allow late submissions after the deadline
    /// </summary>
    [Required]
    [Column("allow_late_submission")]
    public bool AllowLateSubmission { get; set; } = true;

    /// <summary>
    /// Penalty percentage for late submissions (0-100)
    /// Example: 10 means 10% will be deducted from the grade
    /// </summary>
    [Column("late_submission_penalty_percent")]
    [Range(0, 100, ErrorMessage = "Late submission penalty must be between 0 and 100")]
    public decimal? LateSubmissionPenaltyPercent { get; set; } = 0;

    /// <summary>
    /// Start date of the window when students can edit their milestone submissions
    /// Typically starts after instructor review/feedback
    /// </summary>
    [Column("edit_window_start_date")]
    public DateTime? EditWindowStartDate { get; set; }

    /// <summary>
    /// End date of the window when students can edit their milestone submissions
    /// After this date, submissions are locked and cannot be edited
    /// </summary>
    [Column("edit_window_end_date")]
    public DateTime? EditWindowEndDate { get; set; }

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
               MinMembersPerGroup >= 1 &&
               ValidateSubmissionDates() &&
               ValidateEditWindowDates();
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

    /// <summary>
    /// Checks if submission deadline has been passed
    /// </summary>
    public bool IsSubmissionDeadlinePassed()
    {
        if (SubmissionDeadline == null)
            return false;

        return DateTime.UtcNow > SubmissionDeadline.Value;
    }

    /// <summary>
    /// Checks if currently within submission period
    /// </summary>
    public bool IsWithinSubmissionPeriod()
    {
        var now = DateTime.UtcNow;
        
        // Check start date
        if (SubmissionStartDate.HasValue && now < SubmissionStartDate.Value)
            return false;
        
        // Check deadline (allow late if configured)
        if (SubmissionDeadline.HasValue && now > SubmissionDeadline.Value)
            return AllowLateSubmission;
        
        return true;
    }

    /// <summary>
    /// Checks if submission is late
    /// </summary>
    public bool IsSubmissionLate()
    {
        if (SubmissionDeadline == null)
            return false;

        return DateTime.UtcNow > SubmissionDeadline.Value;
    }

    /// <summary>
    /// Checks if currently within edit window
    /// </summary>
    public bool IsWithinEditWindow()
    {
        if (EditWindowStartDate == null || EditWindowEndDate == null)
            return false; // No edit window configured, editing not allowed

        var now = DateTime.UtcNow;
        return now >= EditWindowStartDate.Value && now <= EditWindowEndDate.Value;
    }

    /// <summary>
    /// Gets the status of submission period
    /// </summary>
    public string GetSubmissionPeriodStatus()
    {
        var now = DateTime.UtcNow;

        if (SubmissionStartDate.HasValue && now < SubmissionStartDate.Value)
            return "NotStarted";

        if (SubmissionDeadline.HasValue && now > SubmissionDeadline.Value)
            return AllowLateSubmission ? "Late" : "Closed";

        return "Open";
    }

    /// <summary>
    /// Gets the status of edit window
    /// </summary>
    public string GetEditWindowStatus()
    {
        if (EditWindowStartDate == null || EditWindowEndDate == null)
            return "NotConfigured";

        var now = DateTime.UtcNow;

        if (now < EditWindowStartDate.Value)
            return "NotStarted";

        if (now > EditWindowEndDate.Value)
            return "Closed";

        return "Open";
    }

    /// <summary>
    /// Validates submission dates are in correct order
    /// </summary>
    private bool ValidateSubmissionDates()
    {
        if (SubmissionStartDate.HasValue && SubmissionDeadline.HasValue)
            return SubmissionDeadline.Value >= SubmissionStartDate.Value;
        
        return true;
    }

    /// <summary>
    /// Validates edit window dates are in correct order
    /// </summary>
    private bool ValidateEditWindowDates()
    {
        if (EditWindowStartDate.HasValue && EditWindowEndDate.HasValue)
            return EditWindowEndDate.Value >= EditWindowStartDate.Value;
        
        return true;
    }

    /// <summary>
    /// Calculates the penalty-adjusted grade for a late submission
    /// </summary>
    /// <param name="originalGrade">The original grade before penalty</param>
    /// <returns>Grade after applying late penalty</returns>
    public decimal CalculateLateSubmissionGrade(decimal originalGrade)
    {
        if (!IsSubmissionLate() || LateSubmissionPenaltyPercent == null || LateSubmissionPenaltyPercent == 0)
            return originalGrade;

        var penalty = originalGrade * (LateSubmissionPenaltyPercent.Value / 100m);
        var adjustedGrade = originalGrade - penalty;
        
        return Math.Max(0, adjustedGrade); // Ensure grade doesn't go below 0
    }
}
