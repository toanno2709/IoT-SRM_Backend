using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.ProjectGrade;

public interface IProjectGradeService
{
    /// <summary>
    /// Get all graders and their grades for a specific project (Student view)
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="studentId">Student ID (for authorization)</param>
    /// <returns>List of all graders with their grades</returns>
    Task<ResultModel<ProjectGradersResponseDto>> GetProjectGradersAsync(int projectId, int studentId);
}
