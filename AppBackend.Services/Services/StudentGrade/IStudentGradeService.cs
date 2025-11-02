using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.StudentGrade;

public interface IStudentGradeService
{
    /// <summary>
    /// Get all grades for a student across all projects
    /// </summary>
    Task<ResultModel<StudentGradesResponseDto>> GetMyGradesAsync(int userId);

    /// <summary>
    /// Get grades for a specific project
    /// </summary>
    Task<ResultModel<StudentProjectGradeDto>> GetProjectGradesAsync(int projectId, int userId);

    /// <summary>
    /// Get all feedback for a project (proposal + milestones)
    /// </summary>
    Task<ResultModel<ProjectFeedbackResponseDto>> GetProjectFeedbackAsync(int projectId, int userId);

    /// <summary>
    /// Get calculated overall grade for a project
    /// </summary>
    Task<ResultModel<ProjectOverallGradeDto>> GetProjectOverallGradeAsync(int projectId, int userId);
}
