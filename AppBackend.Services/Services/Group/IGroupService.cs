using AppBackend.BusinessObjects.Dtos.Group;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Services.Services.Group
{
    public interface IGroupService
    {
        Task<GroupCreateResultDto> CreateGroupAsync(GroupCreateDto dto, int creatorUserId);
        Task InviteMemberAsync(GroupInviteDto dto);
        Task AcceptInviteAsync(GroupAcceptInviteDto dto);
        Task LeaveGroupAsync(GroupLeaveDto dto);
        Task KickMemberAsync(GroupKickDto dto);
        Task UpdateGroupAsync(GroupUpdateDto dto);
        Task DeleteGroupAsync(int groupId, int requesterUserId);
        Task<IEnumerable<GroupListItemDto>> GetGroupsByClassAsync(int classId);
        Task<GroupDetailDto> GetGroupDetailAsync(int groupId);
    }
}
