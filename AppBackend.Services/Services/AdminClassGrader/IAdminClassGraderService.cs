using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.AdminClassGrader;

/// <summary>
/// Service for admin to manage class graders assignment
/// </summary>
public interface IAdminClassGraderService
{
    /// <summary>
    /// Get all graders assigned to a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of assigned graders with details</returns>
    Task<ResultModel<List<ClassGraderDetailDto>>> GetClassGradersAsync(int classId);

    /// <summary>
    /// Assign an instructor to grade projects in a class
    /// </summary>
    /// <param name="request">Assignment request with class and instructor IDs</param>
    /// <param name="adminId">Admin user ID performing the assignment</param>
    /// <returns>Created grader assignment</returns>
    Task<ResultModel<ClassGraderDetailDto>> AssignGraderAsync(AssignClassGraderRequestDto request, int adminId);

    /// <summary>
    /// Remove grader assignment from a class
    /// </summary>
    /// <param name="graderId">Grader ID to remove</param>
    /// <param name="adminId">Admin user ID performing the removal</param>
    /// <returns>Success status</returns>
    Task<ResultModel<bool>> RemoveGraderAsync(int graderId, int adminId);

    /// <summary>
    /// Toggle grader active status
    /// </summary>
    /// <param name="graderId">Grader ID</param>
    /// <param name="isActive">New active status</param>
    /// <param name="adminId">Admin user ID performing the update</param>
    /// <returns>Updated grader assignment</returns>
    Task<ResultModel<ClassGraderDetailDto>> UpdateGraderStatusAsync(int graderId, bool isActive, int adminId);

    /// <summary>
    /// Get all grader assignments across all classes
    /// </summary>
    /// <param name="instructorId">Optional filter by instructor</param>
    /// <param name="classId">Optional filter by class</param>
    /// <param name="isActive">Optional filter by active status</param>
    /// <returns>List of all grader assignments with statistics</returns>
    Task<ResultModel<List<ClassGraderSummaryDto>>> GetAllGraderAssignmentsAsync(
        int? instructorId = null, 
        int? classId = null, 
        bool? isActive = null);

    /// <summary>
    /// Get grading statistics for a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>Comprehensive grading statistics</returns>
    Task<ResultModel<ClassGradingStatisticsDto>> GetClassGradingStatisticsAsync(int classId);

    /// <summary>
    /// Bulk assign multiple graders to a class
    /// </summary>
    /// <param name="request">Bulk assignment request with instructor IDs</param>
    /// <param name="adminId">Admin user ID performing the assignment</param>
    /// <returns>List of created assignments with results</returns>
    Task<ResultModel<BulkAssignGradersResponseDto>> BulkAssignGradersAsync(
        BulkAssignGradersRequestDto request, 
        int adminId);
}
