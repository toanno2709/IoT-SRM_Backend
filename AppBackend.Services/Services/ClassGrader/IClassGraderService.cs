using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.ClassGrader;

public interface IClassGraderService
{
    /// <summary>
    /// Get all classes where instructor is assigned as grader
    /// </summary>
    /// <param name="instructorId">Instructor user ID</param>
    /// <returns>List of grading classes with statistics</returns>
    Task<ResultModel<List<GradingClassDto>>> GetGradingClassesAsync(int instructorId);

    /// <summary>
    /// Get all approved projects in a specific class for grading
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="instructorId">Instructor user ID</param>
    /// <returns>List of approved projects with grading status</returns>
    Task<ResultModel<List<ApprovedProjectForGradingDto>>> GetApprovedProjectsForGradingAsync(int classId, int instructorId);

    /// <summary>
    /// Get detailed final submission for grading
    /// </summary>
    /// <param name="finalSubmissionId">Final submission ID</param>
    /// <param name="instructorId">Instructor user ID</param>
    /// <returns>Detailed submission info with all grades</returns>
    Task<ResultModel<GraderFinalSubmissionDetailDto>> GetFinalSubmissionForGradingAsync(int finalSubmissionId, int instructorId);

    /// <summary>
    /// Grade or update grade for final project submission
    /// </summary>
    /// <param name="finalSubmissionId">Final submission ID</param>
    /// <param name="request">Grade and feedback</param>
    /// <param name="instructorId">Instructor user ID</param>
    /// <returns>Grading result with average</returns>
    Task<ResultModel<GraderFinalProjectGradeResponseDto>> GradeFinalSubmissionAsync(
        int finalSubmissionId, 
        GraderFinalProjectGradeRequestDto request, 
        int instructorId);
}
