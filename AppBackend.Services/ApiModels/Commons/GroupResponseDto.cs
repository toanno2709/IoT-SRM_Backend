using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

public class GroupMemberDto
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? RoleInGroup { get; set; }
    public string? AvatarUrl { get; set; }  // Added for user profile picture
}

public class GroupResponseDto
{
    public int GroupId { get; set; }

    [Required]
    [StringLength(255)]
    public string GroupName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? LeaderId { get; set; }
    public string? LeaderName { get; set; }

    public int ClassId { get; set; }
    public string? ClassName { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int MemberCount { get; set; }

    public List<GroupMemberDto> Members { get; set; } = new();
    
    public int ProjectCount { get; set; }
}

/// <summary>
/// Result DTO for random group creation
/// </summary>
public class RandomGroupCreationResultDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int TotalStudentsInClass { get; set; }
    public int StudentsAlreadyInGroups { get; set; }
    public int UnassignedStudents { get; set; }
    public int GroupsCreated { get; set; }
    public int StudentsAssigned { get; set; }
    public int StudentsRemaining { get; set; }
    public int MinMembersPerGroup { get; set; }
    public int MaxMembersPerGroup { get; set; }
    public List<CreatedGroupSummaryDto> CreatedGroups { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Summary info for each created group
/// </summary>
public class CreatedGroupSummaryDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public List<string> MemberNames { get; set; } = new();
}


