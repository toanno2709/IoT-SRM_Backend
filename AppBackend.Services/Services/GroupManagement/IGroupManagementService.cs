using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.GroupMemberRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.BusinessObjects.Models;

namespace AppBackend.Services.Services.GroupManagement;

public interface IGroupManagementService
{
    Task<ResultModel<GroupResponseDto>> UpdateGroupInfoAsync(int groupId, GroupUpdateRequestDto request);
    Task<ResultModel<GroupMemberOperationResponseDto>> AddMemberAsync(int groupId, AddGroupMemberRequestDto request);
    Task<ResultModel<GroupMemberOperationResponseDto>> RemoveMemberAsync(int groupId, int userId);
    Task<ResultModel<GroupMemberOperationResponseDto>> UpdateMemberRoleAsync(int groupId, int userId, UpdateMemberRoleRequestDto request);
}

public class GroupManagementService : IGroupManagementService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserRepository _userRepository;

    public GroupManagementService(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IUserRepository userRepository)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _userRepository = userRepository;
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

            await _groupRepository.UpdateAsync(group);
            await _groupRepository.SaveChangesAsync();

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

            // Add member
            var newMember = new GroupMember
            {
                GroupId = groupId,
                UserId = request.UserId,
                RoleInGroup = request.RoleInGroup ?? "Member",
                JoinedAt = DateTime.UtcNow
            };

            await _groupMemberRepository.AddAsync(newMember);
            await _groupMemberRepository.SaveChangesAsync();

            return new ResultModel<GroupMemberOperationResponseDto>
            {
                IsSuccess = true,
                Message = "Member added successfully",
                Data = new GroupMemberOperationResponseDto
                {
                    GroupId = groupId,
                    GroupName = group.GroupName,
                    UserId = request.UserId,
                    UserName = user.FullName,
                    Email = user.Email,
                    RoleInGroup = newMember.RoleInGroup,
                    Operation = "Added",
                    OperationDate = newMember.JoinedAt
                }
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<GroupMemberOperationResponseDto>
            {
                IsSuccess = false,
                Message = $"Error adding member: {ex.Message}",
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

            // Prevent removing the leader
            if (group.LeaderId == userId)
            {
                return new ResultModel<GroupMemberOperationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Cannot remove group leader. Please assign a new leader first.",
                    Data = null
                };
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


