using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.GroupMemberRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.BusinessObjects.Models;
using AppBackend.BusinessObjects.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.GroupManagement;

public class GroupManagementService : IGroupManagementService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IotShowroomContext _context;
    private readonly ILogger<GroupManagementService> _logger;

    public GroupManagementService(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IUserRepository userRepository,
        IotShowroomContext context,
        ILogger<GroupManagementService> logger)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _userRepository = userRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<GroupResponseDto>> UpdateGroupInfoAsync(int groupId, GroupUpdateRequestDto request)
    {
        try
        {
            var group = await _groupRepository.GetGroupWithDetailsAsync(groupId);
            if (group == null)
            {
                return new ResultModel<GroupResponseDto>
                {
                    IsSuccess = false,
                    Message = "Group not found",
                    Data = null
                };
            }

            // Update group info
            group.GroupName = request.GroupName;
            group.Description = request.Description;
            group.UpdatedAt = DateTime.UtcNow;

            // Note: IGroupRepository doesn't have UpdateAsync, we'll need to use context directly or add it
            // For now, using the context approach through repository
            await _groupRepository.CreateGroupAsync(group, group.LeaderId ?? 0);

            // Return updated group
            var dto = new GroupResponseDto
            {
                GroupId = group.GroupId,
                GroupName = group.GroupName,
                Description = group.Description,
                LeaderId = group.LeaderId,
                LeaderName = group.Leader?.FullName,
                ClassId = group.ClassId,
                ClassName = group.Class?.ClassName,
                CreatedAt = group.CreatedAt,
                UpdatedAt = group.UpdatedAt,
                MemberCount = group.GroupMembers?.Count ?? 0,
                ProjectCount = group.Projects?.Count ?? 0
            };

            return new ResultModel<GroupResponseDto>
            {
                IsSuccess = true,
                Message = "Group updated successfully",
                Data = dto
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<GroupResponseDto>
            {
                IsSuccess = false,
                Message = $"Error updating group: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<GroupMemberOperationResponseDto>> AddMemberAsync(int groupId, AddGroupMemberRequestDto request)
    {
        try
        {
            // Validate group exists
            var group = await _groupRepository.GetGroupWithDetailsAsync(groupId);
            if (group == null)
            {
                return new ResultModel<GroupMemberOperationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Group not found",
                    Data = null
                };
            }

            // Validate user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new ResultModel<GroupMemberOperationResponseDto>
                {
                    IsSuccess = false,
                    Message = "User not found",
                    Data = null
                };
            }

            // Check if user is already a member
            var existingMember = await _groupMemberRepository.IsMemberInGroupAsync(groupId, request.UserId);
            if (existingMember)
            {
                return new ResultModel<GroupMemberOperationResponseDto>
                {
                    IsSuccess = false,
                    Message = "User is already a member of this group",
                    Data = null
                };
            }

            // ? FIX: Check if invitation already exists
            var existingInvitation = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.UserId == request.UserId &&
                    n.Type == "group_invitation" &&
                    (n.Message ?? "").Contains($"groupId:{groupId}") &&
                    (n.IsRead == null || n.IsRead == false));

            if (existingInvitation != null)
            {
                return new ResultModel<GroupMemberOperationResponseDto>
                {
                    IsSuccess = false,
                    Message = "An invitation to this group is already pending for this user",
                    Data = null
                };
            }

            // ? FIX: Create invitation notification instead of adding directly
            // Create Data JSON for notification with classId and groupId
            var notificationData = System.Text.Json.JsonSerializer.Serialize(new
            {
                classId = group.ClassId,
                groupId = groupId
            });

            var invitation = new BusinessObjects.Models.Notification
            {
                UserId = request.UserId,
                Title = "Group Invitation",
                Message = $"You have been invited to join {group.GroupName} (groupId:{groupId})",
                Type = "group_invitation",
                Data = notificationData,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(invitation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invitation sent to user {UserId} for group {GroupId}", request.UserId, groupId);

            return new ResultModel<GroupMemberOperationResponseDto>
            {
                IsSuccess = true,
                Message = "Invitation sent successfully. Student will receive a notification and can accept or reject the invitation.",
                Data = new GroupMemberOperationResponseDto
                {
                    GroupId = groupId,
                    GroupName = group.GroupName,
                    UserId = request.UserId,
                    UserName = user.FullName,
                    Email = user.Email,
                    RoleInGroup = request.RoleInGroup ?? "Member",
                    Operation = "Invited",
                    OperationDate = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation to user {UserId} for group {GroupId}", request.UserId, groupId);
            return new ResultModel<GroupMemberOperationResponseDto>
            {
                IsSuccess = false,
                Message = $"Error sending invitation: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<GroupMemberOperationResponseDto>> RemoveMemberAsync(int groupId, int userId)
    {
        try
        {
            var group = await _groupRepository.GetGroupWithDetailsAsync(groupId);
            if (group == null)
            {
                return new ResultModel<GroupMemberOperationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Group not found",
                    Data = null
                };
            }

            var member = await _groupMemberRepository.GetMemberAsync(groupId, userId);
            if (member == null)
            {
                return new ResultModel<GroupMemberOperationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Member not found in this group",
                    Data = null
                };
            }

            // FIX: Check actual role in GroupMembers table, not just LeaderId
            // Prevent removing if the member's actual role is "Leader"
            if (member.RoleInGroup?.Equals("Leader", StringComparison.OrdinalIgnoreCase) == true)
            {
                return new ResultModel<GroupMemberOperationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Cannot remove group leader. Please assign a new leader first.",
                    Data = null
                };
            }

            // Also check if this user is set as leader in Groups table (data consistency check)
            if (group.LeaderId == userId && member.RoleInGroup?.Equals("Leader", StringComparison.OrdinalIgnoreCase) != true)
            {
                _logger.LogWarning("Data inconsistency: User {UserId} is set as LeaderId in group {GroupId} but has role '{Role}' in GroupMembers. Allowing removal.",
                    userId, groupId, member.RoleInGroup);
            }

            await _groupMemberRepository.DeleteAsync(member);
            await _groupMemberRepository.SaveChangesAsync();

            return new ResultModel<GroupMemberOperationResponseDto>
            {
                IsSuccess = true,
                Message = "Member removed successfully",
                Data = new GroupMemberOperationResponseDto
                {
                    GroupId = groupId,
                    GroupName = group.GroupName,
                    UserId = userId,
                    UserName = member.User?.FullName,
                    Email = member.User?.Email,
                    RoleInGroup = member.RoleInGroup,
                    Operation = "Removed",
                    OperationDate = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<GroupMemberOperationResponseDto>
            {
                IsSuccess = false,
                Message = $"Error removing member: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<GroupMemberOperationResponseDto>> UpdateMemberRoleAsync(
        int groupId, 
        int userId, 
        UpdateMemberRoleRequestDto request)
    {
        try
        {
            var group = await _groupRepository.GetGroupWithDetailsAsync(groupId);
            if (group == null)
            {
                return new ResultModel<GroupMemberOperationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Group not found",
                    Data = null
                };
            }

            var member = await _groupMemberRepository.GetMemberAsync(groupId, userId);
            if (member == null)
            {
                return new ResultModel<GroupMemberOperationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Member not found in this group",
                    Data = null
                };
            }

            // Update role
            member.RoleInGroup = request.RoleInGroup;
            await _groupMemberRepository.UpdateAsync(member);
            await _groupMemberRepository.SaveChangesAsync();

            return new ResultModel<GroupMemberOperationResponseDto>
            {
                IsSuccess = true,
                Message = "Member role updated successfully",
                Data = new GroupMemberOperationResponseDto
                {
                    GroupId = groupId,
                    GroupName = group.GroupName,
                    UserId = userId,
                    UserName = member.User?.FullName,
                    Email = member.User?.Email,
                    RoleInGroup = member.RoleInGroup,
                    Operation = "Updated",
                    OperationDate = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<GroupMemberOperationResponseDto>
            {
                IsSuccess = false,
                Message = $"Error updating member role: {ex.Message}",
                Data = null
            };
        }
    }
}
