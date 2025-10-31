using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.GroupManagement;

public interface IGroupManagementService
{
    Task<ResultModel<GroupResponseDto>> UpdateGroupInfoAsync(int groupId, GroupUpdateRequestDto request);
    Task<ResultModel<GroupMemberOperationResponseDto>> AddMemberAsync(int groupId, AddGroupMemberRequestDto request);
    Task<ResultModel<GroupMemberOperationResponseDto>> RemoveMemberAsync(int groupId, int userId);
    Task<ResultModel<GroupMemberOperationResponseDto>> UpdateMemberRoleAsync(int groupId, int userId, UpdateMemberRoleRequestDto request);
}


