using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.MilestoneWarning;

public interface IMilestoneWarningService
{
    /// <summary>
    /// Check all active classes and send warning notifications to instructors
    /// about projects with incomplete milestone weights (not 100%)
    /// </summary>
    /// <returns>Summary of warnings sent</returns>
    Task<ResultModel<MilestoneWarningResultDto>> CheckAndSendMilestoneWarningsAsync();

    /// <summary>
    /// Get all projects with incomplete milestone weights for a specific class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of projects with incomplete milestones</returns>
    Task<ResultModel<List<ProjectMilestoneWarningDto>>> GetProjectsWithIncompleteMilestonesAsync(int classId);

    /// <summary>
    /// Get milestone weight summary for a specific project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Milestone weight details</returns>
    Task<ResultModel<ProjectMilestoneWeightDto>> GetProjectMilestoneWeightsAsync(int projectId);
}
