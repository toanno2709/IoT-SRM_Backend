using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.ProjectMilestone;

public interface IProjectMilestoneService
{
    Task<ResultModel<List<ProjectMilestoneResponseDto>>> GetByProjectAsync(int projectId);
    Task<ResultModel<ProjectMilestoneResponseDto>> CreateAsync(ProjectMilestoneCreateRequestDto request);
    Task<ResultModel<ProjectMilestoneResponseDto>> UpdateAsync(ProjectMilestoneUpdateRequestDto request);
    Task<ResultModel<bool>> DeleteAsync(int milestoneId);
    
    /// <summary>
    /// Create milestones for all approved projects in a class
    /// </summary>
    /// <param name="request">Bulk creation request containing milestone details</param>
    /// <returns>Result with details of created milestones</returns>
    Task<ResultModel<BulkCreateMilestoneResponseDto>> BulkCreateMilestoneForClassAsync(BulkCreateMilestoneRequestDto request);
}


