using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Project;

public interface IProjectService
{
    Task<ResultModel<List<ProjectGroupResponseDto>>> GetProjectsByClassAsync(int classId);
}

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<ResultModel<List<ProjectGroupResponseDto>>> GetProjectsByClassAsync(int classId)
    {
        try
        {
            var projects = await _projectRepository.GetProjectsByClassAsync(classId);

            var dtos = projects.Select(p => new ProjectGroupResponseDto
            {
                ProjectId = p.ProjectId,
                Title = p.Title,
                Description = p.Description,
                LeaderId = p.LeaderId,
                LeaderName = p.Leader?.FullName,
                ClassId = p.ClassId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                MemberCount = p.ProjectMembers?.Count ?? 0,
                Members = (p.ProjectMembers ?? new List<AppBackend.BusinessObjects.Models.ProjectMember>())
                    .Select(m => new ProjectMemberDto
                    {
                        UserId = m.UserId ?? 0,
                        FullName = m.User?.FullName,
                        Email = m.User?.Email,
                        RoleInProject = m.RoleInProject
                    }).ToList()
            }).ToList();

            return new ResultModel<List<ProjectGroupResponseDto>>
            {
                IsSuccess = true,
                Message = "Projects retrieved successfully",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<ProjectGroupResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving projects: {ex.Message}",
                Data = null
            };
        }
    }
}





