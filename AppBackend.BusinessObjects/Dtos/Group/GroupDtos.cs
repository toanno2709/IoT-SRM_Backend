using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.BusinessObjects.Dtos.Group
{
    public record GroupCreateDto(int ClassId, string GroupName, string? Description);
    public record GroupCreateResultDto(int GroupId, string GroupName, int? LeaderId, int? ClassId);

    public record GroupInviteDto(int GroupId, int InvitedUserId, int InviterUserId);
    public record GroupAcceptInviteDto(int GroupId, int UserId);
    public record GroupLeaveDto(int GroupId, int UserId);
    public record GroupKickDto(int GroupId, int TargetUserId, int RequesterUserId);
    public record GroupUpdateDto(int GroupId, int RequesterUserId, string? GroupName, string? Description);

    public class GroupMemberDto
    {
        public int GmId { get; set; }
        public int? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string? RoleInGroup { get; set; }
        public DateTime? JoinedAt { get; set; }
    }

    public class GroupDetailDto
    {
        public int GroupId { get; set; }
        public int? ClassId { get; set; }
        public string? GroupName { get; set; }
        public string? Description { get; set; }
        public int? LeaderId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<GroupMemberDto> Members { get; set; } = new();
        public List<int> ProjectIds { get; set; } = new();
    }

    public class GroupListItemDto
    {
        public int GroupId { get; set; }
        public string? GroupName { get; set; }
        public int? LeaderId { get; set; }
        public int MemberCount { get; set; }
    }
}
