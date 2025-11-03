using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.ProjectMilestone;

public interface IProjectMilestoneService
{
    Task<ResultModel<List<ProjectMilestoneResponseDto>>> GetByProjectAsync(int projectId);
    Task<ResultModel<ProjectMilestoneResponseDto>> CreateAsync(ProjectMilestoneCreateRequestDto request);
    Task<ResultModel<ProjectMilestoneResponseDto>> UpdateAsync(ProjectMilestoneUpdateRequestDto request);
    Task<ResultModel<bool>> DeleteAsync(int milestoneId);
}


