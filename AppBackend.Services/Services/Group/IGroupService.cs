using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Group;

public interface IGroupService
{
    Task<ResultModel<List<GroupResponseDto>>> GetGroupsByClassAsync(int classId);
    Task<ResultModel<GroupResponseDto>> GetGroupByIdAsync(int groupId);
}

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;

    public GroupService(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<ResultModel<List<GroupResponseDto>>> GetGroupsByClassAsync(int classId)
    {
        try
        {
            var groups = await _groupRepository.GetGroupsByClassAsync(classId);

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
                Members = (g.GroupMembers ?? new List<AppBackend.BusinessObjects.Models.GroupMember>())
                    .Select(m => new GroupMemberDto
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

    public async Task<ResultModel<GroupResponseDto>> GetGroupByIdAsync(int groupId)
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
                ProjectCount = group.Projects?.Count ?? 0,
                Members = (group.GroupMembers ?? new List<AppBackend.BusinessObjects.Models.GroupMember>())
                    .Select(m => new GroupMemberDto
                    {
                        UserId = m.UserId,
                        FullName = m.User?.FullName,
                        Email = m.User?.Email,
                        RoleInGroup = m.RoleInGroup
                    }).ToList()
            };

            return new ResultModel<GroupResponseDto>
            {
                IsSuccess = true,
                Message = "Group retrieved successfully",
                Data = dto
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<GroupResponseDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving group: {ex.Message}",
                Data = null
            };
        }
    }
}


