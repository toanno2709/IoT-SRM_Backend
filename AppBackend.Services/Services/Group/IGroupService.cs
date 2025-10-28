using AppBackend.BusinessObjects.Dtos.Group;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Group;

public interface IGroupService
{
    // CRUD methods
    Task<GroupCreateResultDto> CreateGroupAsync(GroupCreateDto dto, int creatorUserId);
    Task InviteMemberAsync(GroupInviteDto dto);
    Task AcceptInviteAsync(GroupAcceptInviteDto dto);
    Task LeaveGroupAsync(GroupLeaveDto dto);
    Task KickMemberAsync(GroupKickDto dto);
    Task UpdateGroupAsync(GroupUpdateDto dto);
    Task DeleteGroupAsync(int groupId, int requesterUserId);
    Task<IEnumerable<GroupListItemDto>> GetGroupListByClassAsync(int classId);
    Task<ResultModel<List<GroupResponseDto>>> GetGroupsByClassAsync(int classId);
    Task<GroupDetailDto> GetGroupDetailAsync(int groupId);
}


