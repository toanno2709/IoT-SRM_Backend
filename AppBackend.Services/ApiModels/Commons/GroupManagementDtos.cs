using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO để cập nhật thông tin group
/// </summary>
public class GroupUpdateRequestDto
{
    [Required]
    [StringLength(255, MinimumLength = 3)]
    public string GroupName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}

/// <summary>
/// DTO để thêm member vào group
/// </summary>
public class AddGroupMemberRequestDto
{
    [Required]
    public int UserId { get; set; }

    [StringLength(50)]
    public string? RoleInGroup { get; set; } = "Member"; // "Leader", "Member", "Deputy"
}

/// <summary>
/// DTO để cập nhật role của member
/// </summary>
public class UpdateMemberRoleRequestDto
{
    [Required]
    [StringLength(50)]
    public string RoleInGroup { get; set; } = string.Empty;
}

/// <summary>
/// Response sau khi thao tác với group member
/// </summary>
public class GroupMemberOperationResponseDto
{
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? RoleInGroup { get; set; }
    public string? Operation { get; set; } // "Added", "Removed", "Updated"
    public DateTime? OperationDate { get; set; }
}


