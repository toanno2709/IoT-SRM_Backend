using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

public class ProjectMemberDto
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? RoleInProject { get; set; }
}

public class ProjectGroupResponseDto
{
    public int ProjectId { get; set; }

    [Required]
    [StringLength(255)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Project status: Pending, Approved, Revision, Rejected, InProgress, Completed
    /// </summary>
    public string? Status { get; set; }

    public int? LeaderId { get; set; }
    public string? LeaderName { get; set; }

    public int GroupId { get; set; }
    public string? GroupName { get; set; }

    public int? ClassId { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int MemberCount { get; set; }

    public List<ProjectMemberDto> Members { get; set; } = new();
}





