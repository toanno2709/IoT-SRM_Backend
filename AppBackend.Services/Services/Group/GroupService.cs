using AppBackend.BusinessObjects.Dtos.Group;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroupEntity = AppBackend.BusinessObjects.Models.Group;

using AppBackend.BusinessObjects.Data;

namespace AppBackend.Services.Services.Group
{
    public class GroupService : IGroupService
    {
        private readonly IotShowroomContext _db;
        private readonly ILogger<GroupService> _logger;

        public GroupService(IotShowroomContext db, ILogger<GroupService> logger)
        {
            _db = db;
            _logger = logger;
        }

        private async Task SendNotificationAsync(int userId, string title, string message, string type = "system")
        {
            var note = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.Notifications.Add(note);
            await _db.SaveChangesAsync();
        }
        // 1. Create group: check user not in any group in same class -> create group and add leader as GroupMember role "Leader"
        public async Task<GroupCreateResultDto> CreateGroupAsync(GroupCreateDto dto, int creatorUserId)
        {
            // check user not already in a group in same class
            var inSameClass = await _db.GroupMembers
                .Include(gm => gm.Group)
                .Where(gm => gm.UserId == creatorUserId && gm.Group != null && gm.Group.ClassId == dto.ClassId)
                .AnyAsync();

            if (inSameClass)
                throw new InvalidOperationException("User already in a group in this class.");

            var group = new GroupEntity
            {
                ClassId = dto.ClassId,
                GroupName = dto.GroupName,
                Description = dto.Description,
                LeaderId = creatorUserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Groups.Add(group);
            await _db.SaveChangesAsync(); // have GroupId

            // add group member as leader
            var gm = new GroupMember
            {
                GroupId = group.GroupId,
                UserId = creatorUserId,
                RoleInGroup = "Leader",
                JoinedAt = DateTime.UtcNow
            };
            _db.GroupMembers.Add(gm);
            await _db.SaveChangesAsync();
            // send notification to creator
            await SendNotificationAsync(creatorUserId, "Group Created", $"You have successfully created group '{group.GroupName}'.", "group_create");

            return new GroupCreateResultDto(group.GroupId, group.GroupName, group.LeaderId, group.ClassId);
        }

        // 2. Invite: inviter must be group leader; invited user must not belong to any group in same class
        public async Task InviteMemberAsync(GroupInviteDto dto)
        {
            var group = await _db.Groups.FindAsync(dto.GroupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            if (group.LeaderId != dto.InviterUserId)
                throw new InvalidOperationException("Only group leader can invite members.");

            // check invited user existence
            var invited = await _db.Users.FindAsync(dto.InvitedUserId);
            if (invited == null) throw new KeyNotFoundException("Invited user not found.");

            // check invited not in group in same class
            var alreadyIn = await _db.GroupMembers
                .Include(gm => gm.Group)
                .Where(gm => gm.UserId == dto.InvitedUserId && gm.Group != null && gm.Group.ClassId == group.ClassId)
                .AnyAsync();

            if (alreadyIn) throw new InvalidOperationException("Invited user already in a group in this class.");

            // create a notification row (simple invite model)
            //var note = new Notification
            //{
            //    UserId = dto.InvitedUserId,
            //    Title = $"Invitation to join group {group.GroupName}",
            //    Message = $"You have been invited to join group '{group.GroupName}' in class {group.ClassId} by user {dto.InviterUserId}.",
            //    Type = "invite",
            //    IsRead = false,
            //    CreatedAt = DateTime.UtcNow
            //};
            //_db.Notifications.Add(note);
            //await _db.SaveChangesAsync();
            await SendNotificationAsync(dto.InvitedUserId,
                $"Invitation to join group {group.GroupName}",
                $"You have been invited to join group '{group.GroupName}' in class {group.ClassId} by {dto.InviterUserId}.",
                "invite");
        }

        // 3. Accept invite: add to group if not in other group in same class, remove any previous invite-notif? (we keep simple)
        public async Task AcceptInviteAsync(GroupAcceptInviteDto dto)
        {
            var group = await _db.Groups.FindAsync(dto.GroupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            // check user not already in a group in same class
            var inSameClass = await _db.GroupMembers
                .Include(gm => gm.Group)
                .Where(gm => gm.UserId == dto.UserId && gm.Group != null && gm.Group.ClassId == group.ClassId)
                .AnyAsync();

            if (inSameClass)
                throw new InvalidOperationException("User already in a group in this class.");

            // add membership
            var gm = new GroupMember
            {
                GroupId = dto.GroupId,
                UserId = dto.UserId,
                RoleInGroup = "Member",
                JoinedAt = DateTime.UtcNow
            };
            _db.GroupMembers.Add(gm);

            // optional: mark invite notification as read — try to find invite notification and mark read
            var possibleInvite = await _db.Notifications
                .Where(n => n.UserId == dto.UserId && n.Type == "invite" && n.Title != null && n.Title.Contains(group.GroupName!))
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            if (possibleInvite != null)
            {
                possibleInvite.IsRead = true;
            }

            await _db.SaveChangesAsync();
            // send notification to group leader
            await SendNotificationAsync(group.LeaderId ?? 0,
                "Member Joined Group",
                $"A new member has joined your group '{group.GroupName}'.",
                "group_update");
        }

        // 4. Leave group
        public async Task LeaveGroupAsync(GroupLeaveDto dto)
        {
            var gm = await _db.GroupMembers.FirstOrDefaultAsync(x => x.GroupId == dto.GroupId && x.UserId == dto.UserId);
            if (gm == null) throw new InvalidOperationException("Membership not found.");

            var group = await _db.Groups.Include(g => g.GroupMembers).FirstOrDefaultAsync(g => g.GroupId == dto.GroupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            // if leaving member is leader
            if (group.LeaderId == dto.UserId)
            {
                // if other members exist -> promote first member to leader
                var otherMember = await _db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == dto.GroupId && m.UserId != dto.UserId);
                if (otherMember != null)
                {
                    group.LeaderId = otherMember.UserId;
                    otherMember.RoleInGroup = "Leader";
                    _db.GroupMembers.Remove(gm); // remove old leader membership
                }
                else
                {
                    // no other members: remove group and its members
                    // remove membership and group
                    _db.GroupMembers.Remove(gm);
                    _db.Groups.Remove(group);
                    await _db.SaveChangesAsync();
                    return;
                }
            }
            else
            {
                // normal member leaving
                _db.GroupMembers.Remove(gm);
            }

            await _db.SaveChangesAsync();
            // send notification to group leader
            await SendNotificationAsync(group.LeaderId ?? 0,
               "Member Left Group",
               $"A member has left your group '{group.GroupName}'.",
               "group_update");
        }

        // 5. Kick member (leader only)
        public async Task KickMemberAsync(GroupKickDto dto)
        {
            var group = await _db.Groups.FindAsync(dto.GroupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            if (group.LeaderId != dto.RequesterUserId) throw new InvalidOperationException("Only leader can remove members.");

            var gm = await _db.GroupMembers.FirstOrDefaultAsync(x => x.GroupId == dto.GroupId && x.UserId == dto.TargetUserId);
            if (gm == null) throw new KeyNotFoundException("Member not found in the group.");

            // can't kick yourself
            if (dto.TargetUserId == dto.RequesterUserId) throw new InvalidOperationException("Leader cannot kick themselves.");

            _db.GroupMembers.Remove(gm);

            
            //var note = new Notification
            //{
            //    UserId = dto.TargetUserId,
            //    Title = $"Removed from group {group.GroupName}",
            //    Message = $"You have been removed from group '{group.GroupName}'.",
            //    Type = "group_removed",
            //    IsRead = false,
            //    CreatedAt = DateTime.UtcNow
            //};
            //_db.Notifications.Add(note);

            await _db.SaveChangesAsync();
// send notification to kicked user
            await SendNotificationAsync(dto.TargetUserId,
                $"Removed from group {group.GroupName}",
                $"You have been removed from group '{group.GroupName}'.",
                "group_removed");
        }

        // 6. Update group (leader only)
        public async Task UpdateGroupAsync(GroupUpdateDto dto)
        {
            var group = await _db.Groups.FindAsync(dto.GroupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            if (group.LeaderId != dto.RequesterUserId) throw new InvalidOperationException("Only leader can update group.");

            if (!string.IsNullOrWhiteSpace(dto.GroupName)) group.GroupName = dto.GroupName;
            if (dto.Description != null) group.Description = dto.Description;
            group.UpdatedAt = DateTime.UtcNow;

            _db.Groups.Update(group);
            await _db.SaveChangesAsync();
            var memberIds = await _db.GroupMembers
                .Where(m => m.GroupId == group.GroupId && m.UserId != group.LeaderId)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var uid in memberIds)
                await SendNotificationAsync(uid, "Group Updated", $"Group '{group.GroupName}' information has been updated.", "group_update");

        }

        // 7. Delete group (Admin or Instructor or Leader depending policy) — here only Admin (role id 1) or Instructor (role id 3?) or leader
        public async Task DeleteGroupAsync(int groupId, int requesterUserId)
        {
            var group = await _db.Groups.Include(g => g.GroupMembers).FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null) throw new KeyNotFoundException("Group not found.");

            var requester = await _db.Users.FindAsync(requesterUserId);
            if (requester == null) throw new KeyNotFoundException("Requester user not found.");

            var isLeader = group.LeaderId == requesterUserId;
            var isAdmin = requester.RoleId == 1;
            var isInstructor = requester.RoleId == 2 || requester.RoleId == 3; // adjust based on your role mapping

            if (!isLeader && !isAdmin && !isInstructor) throw new InvalidOperationException("Not allowed to delete group.");

            // optional: check there is no project associated
            var hasProject = await _db.Projects.AnyAsync(p => p.GroupId == groupId);
            if (hasProject) throw new InvalidOperationException("Cannot delete group with an associated project.");

            var memberIds = group.GroupMembers.Select(m => m.UserId).ToList();

            // remove members and group
            _db.GroupMembers.RemoveRange(group.GroupMembers);
            _db.Groups.Remove(group);
            await _db.SaveChangesAsync();

            foreach (var uid in memberIds)
                await SendNotificationAsync(uid, "Group Deleted", $"Your group '{group.GroupName}' has been deleted.", "group_deleted");

        }

        // 8. Get groups by class (simple list)
        public async Task<IEnumerable<GroupListItemDto>> GetGroupListByClassAsync(int classId)
        {
            var results = await _db.Groups
                .Where(g => g.ClassId == classId)
                .Select(g => new GroupListItemDto
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    LeaderId = g.LeaderId,
                    MemberCount = _db.GroupMembers.Count(m => m.GroupId == g.GroupId)
                })
                .ToListAsync();

            return results;
        }

        // Get groups by class with ResultModel (for controller compatibility)
        public async Task<ResultModel<List<GroupResponseDto>>> GetGroupsByClassAsync(int classId)
        {
            try
            {
                var groups = await _db.Groups
                    .Include(g => g.Leader)
                    .Include(g => g.Class)
                    .Include(g => g.GroupMembers).ThenInclude(gm => gm.User)
                    .Include(g => g.Projects)
                    .Where(g => g.ClassId == classId)
                    .ToListAsync();

                var dtos = groups.Select(g => new GroupResponseDto
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    Description = g.Description,
                    LeaderId = g.LeaderId,
                    LeaderName = g.Leader?.FullName,
                    ClassId = g.ClassId,
                    ClassName = g.Class?.ClassName,
                    CreatedAt = g.CreatedAt,
                    UpdatedAt = g.UpdatedAt,
                    MemberCount = g.GroupMembers?.Count ?? 0,
                    ProjectCount = g.Projects?.Count ?? 0,
                    Members = (g.GroupMembers ?? new List<GroupMember>())
                        .Select(m => new AppBackend.Services.ApiModels.Commons.GroupMemberDto
                        {
                            UserId = m.UserId,
                            FullName = m.User?.FullName,
                            Email = m.User?.Email,
                            RoleInGroup = m.RoleInGroup
                        }).ToList()
                }).ToList();

                return new ResultModel<List<GroupResponseDto>>
                {
                    IsSuccess = true,
                    Message = "Groups retrieved successfully",
                    Data = dtos
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<List<GroupResponseDto>>
                {
                    IsSuccess = false,
                    Message = $"Error retrieving groups: {ex.Message}",
                    Data = null
                };
            }
        }

        // 9. Get group detail
        public async Task<GroupDetailDto> GetGroupDetailAsync(int groupId)
        {
            var group = await _db.Groups
                .Include(g => g.GroupMembers)
                .Include(g => g.Projects)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null) throw new KeyNotFoundException("Group not found.");

            var dto = new GroupDetailDto
            {
                GroupId = group.GroupId,
                ClassId = group.ClassId,
                GroupName = group.GroupName,
                Description = group.Description,
                LeaderId = group.LeaderId,
                CreatedAt = group.CreatedAt,
                UpdatedAt = group.UpdatedAt,
                Members = group.GroupMembers.Select(m => new AppBackend.BusinessObjects.Dtos.Group.GroupMemberDto
                {
                    GmId = m.GmId,
                    UserId = m.UserId,
                    RoleInGroup = m.RoleInGroup,
                    JoinedAt = m.JoinedAt
                }).ToList(),
                ProjectIds = group.Projects?.Select(p => p.ProjectId).ToList() ?? new List<int>()
            };

            return dto;
        }
    }
}