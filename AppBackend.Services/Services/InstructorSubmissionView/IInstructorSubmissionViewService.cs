using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.InstructorSubmissionView;

public interface IInstructorSubmissionViewService
{
    /// <summary>
    /// Get all submissions for a specific milestone (all groups)
    /// </summary>
    Task<ResultModel<List<InstructorSubmissionViewDto>>> GetSubmissionsByMilestoneAsync(
        int milestoneId, 
        int instructorId, 
        SubmissionFilterDto? filter = null);

    /// <summary>
    /// Get all submissions in a class, grouped by milestone
    /// </summary>
    Task<ResultModel<ClassSubmissionOverviewDto>> GetClassSubmissionsAsync(
        int classId, 
        int instructorId, 
        SubmissionFilterDto? filter = null);

    /// <summary>
    /// Get detailed files for a specific submission (for grading)
    /// </summary>
    Task<ResultModel<InstructorSubmissionFilesDto>> GetSubmissionFilesAsync(
        int submissionId, 
        int instructorId);

    /// <summary>
    /// Get submissions that need grading
    /// </summary>
    Task<ResultModel<List<InstructorSubmissionViewDto>>> GetPendingGradingSubmissionsAsync(
        int instructorId, 
        int? classId = null);
}
