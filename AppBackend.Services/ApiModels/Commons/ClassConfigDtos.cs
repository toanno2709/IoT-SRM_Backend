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
    public DateTime? GroupFormationDeadline { get; set; }
    public bool AllowStudentCreateGroup { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Additional info
    public int CurrentGroupCount { get; set; }
    public bool IsGroupFormationOpen { get; set; }
    public string? DeadlineStatus { get; set; }
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

    public DateTime? GroupFormationDeadline { get; set; }

    public bool? AllowStudentCreateGroup { get; set; }

    /// <summary>
    /// Validates that max is greater than or equal to min
    /// </summary>
    public bool IsValid(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (MinMembersPerGroup.HasValue && MaxMembersPerGroup.HasValue)
        {
            if (MaxMembersPerGroup.Value < MinMembersPerGroup.Value)
            {
                errorMessage = "Max members per group must be greater than or equal to min members per group";
                return false;
            }
        }

        if (GroupFormationDeadline.HasValue && GroupFormationDeadline.Value <= DateTime.UtcNow)
        {
            errorMessage = "Group formation deadline must be in the future";
            return false;
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
