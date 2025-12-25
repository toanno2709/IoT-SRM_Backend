using System;
using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO for class configuration response
/// </summary>
public class ClassConfigResponseDto
{
    public int ConfigId { get; set; }
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int MaxGroupsAllowed { get; set; }
    public int MinMembersPerGroup { get; set; }
    public int MaxMembersPerGroup { get; set; }
    
    // Team Formation Settings
    public DateTime? GroupFormationDeadline { get; set; }
    public bool AllowStudentCreateGroup { get; set; }
    
    // Project Creation Settings
    public DateTime? ProjectCreationDeadline { get; set; }
    
    // Milestone Submission Settings
    public DateTime? SubmissionStartDate { get; set; }
    public DateTime? SubmissionDeadline { get; set; }
    public bool AllowLateSubmission { get; set; }
    public decimal? LateSubmissionPenaltyPercent { get; set; }
    
    // Milestone Edit Window Settings
    public DateTime? EditWindowStartDate { get; set; }
    public DateTime? EditWindowEndDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Additional info
    public int CurrentGroupCount { get; set; }
    
    // Team Formation Status
    public bool IsGroupFormationOpen { get; set; }
    public string? GroupFormationStatus { get; set; }
    
    // Project Creation Status
    public bool IsProjectCreationOpen { get; set; }
    public string? ProjectCreationStatus { get; set; }
    
    // Submission Period Status
    public string? SubmissionPeriodStatus { get; set; } // "NotStarted", "Open", "Late", "Closed"
    public bool CanSubmitNow { get; set; }
    public bool IsSubmissionLate { get; set; }
    
    // Edit Window Status
    public string? EditWindowStatus { get; set; } // "NotConfigured", "NotStarted", "Open", "Closed"
    public bool CanEditNow { get; set; }
}

/// <summary>
/// DTO for updating class configuration
/// </summary>
public class ClassConfigUpdateDto
{
    [Range(1, 100, ErrorMessage = "Max groups allowed must be between 1 and 100")]
    public int? MaxGroupsAllowed { get; set; }

    [Range(1, 10, ErrorMessage = "Min members per group must be between 1 and 10")]
    public int? MinMembersPerGroup { get; set; }

    [Range(1, 20, ErrorMessage = "Max members per group must be between 1 and 20")]
    public int? MaxMembersPerGroup { get; set; }

    /// <summary>
    /// Deadline for students to form/create teams
    /// </summary>
    public DateTime? GroupFormationDeadline { get; set; }

    /// <summary>
    /// Deadline for teams to create their projects
    /// </summary>
    public DateTime? ProjectCreationDeadline { get; set; }

    public bool? AllowStudentCreateGroup { get; set; }

    // Milestone Submission Settings
    public DateTime? SubmissionStartDate { get; set; }
    public DateTime? SubmissionDeadline { get; set; }
    public bool? AllowLateSubmission { get; set; }
    
    [Range(0, 100, ErrorMessage = "Late submission penalty must be between 0 and 100")]
    public decimal? LateSubmissionPenaltyPercent { get; set; }

    // Milestone Edit Window Settings
    public DateTime? EditWindowStartDate { get; set; }
    public DateTime? EditWindowEndDate { get; set; }

    /// <summary>
    /// Validates that all configurations are valid
    /// </summary>
    public bool IsValid(out string errorMessage)
    {
        errorMessage = string.Empty;

        // Validate member counts
        if (MinMembersPerGroup.HasValue && MaxMembersPerGroup.HasValue)
        {
            if (MaxMembersPerGroup.Value < MinMembersPerGroup.Value)
            {
                errorMessage = "Max members per group must be greater than or equal to min members per group";
                return false;
            }
        }

        // Validate group formation deadline
        if (GroupFormationDeadline.HasValue && GroupFormationDeadline.Value <= DateTime.UtcNow)
        {
            errorMessage = "Group formation deadline must be in the future";
            return false;
        }

        // Validate project creation deadline
        if (ProjectCreationDeadline.HasValue && ProjectCreationDeadline.Value <= DateTime.UtcNow)
        {
            errorMessage = "Project creation deadline must be in the future";
            return false;
        }

        // Validate deadline order: project creation should be after or equal to team formation
        if (GroupFormationDeadline.HasValue && ProjectCreationDeadline.HasValue)
        {
            if (ProjectCreationDeadline.Value < GroupFormationDeadline.Value)
            {
                errorMessage = "Project creation deadline must be after or equal to group formation deadline";
                return false;
            }
        }

        // Validate submission dates
        if (SubmissionStartDate.HasValue && SubmissionDeadline.HasValue)
        {
            if (SubmissionDeadline.Value < SubmissionStartDate.Value)
            {
                errorMessage = "Submission deadline must be after or equal to submission start date";
                return false;
            }
        }

        // Validate edit window dates
        if (EditWindowStartDate.HasValue && EditWindowEndDate.HasValue)
        {
            if (EditWindowEndDate.Value < EditWindowStartDate.Value)
            {
                errorMessage = "Edit window end date must be after or equal to start date";
                return false;
            }
        }

        // Validate late submission penalty
        if (LateSubmissionPenaltyPercent.HasValue)
        {
            if (LateSubmissionPenaltyPercent.Value < 0 || LateSubmissionPenaltyPercent.Value > 100)
            {
                errorMessage = "Late submission penalty must be between 0 and 100";
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// DTO for validating group creation against class configuration
/// </summary>
public class GroupValidationDto
{
    public int ClassId { get; set; }
    public int ProposedMemberCount { get; set; }
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// DTO for validating project creation against class configuration
/// </summary>
public class ProjectCreationValidationDto
{
    public int ClassId { get; set; }
    public int GroupId { get; set; }
    public bool CanCreate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime? DeadlineDate { get; set; }
}

/// <summary>
/// DTO for submission deadline validation result
/// </summary>
public class SubmissionDeadlineValidationDto
{
    public bool CanSubmit { get; set; }
    public bool IsLate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime? DeadlineDate { get; set; }
    public decimal? ApplicablePenaltyPercent { get; set; }
}

/// <summary>
/// DTO for edit window validation result
/// </summary>
public class EditWindowValidationDto
{
    public bool CanEdit { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime? WindowStartDate { get; set; }
    public DateTime? WindowEndDate { get; set; }
}
