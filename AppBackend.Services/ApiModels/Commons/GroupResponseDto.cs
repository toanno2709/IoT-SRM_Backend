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


