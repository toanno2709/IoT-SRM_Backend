using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.FinalProjectRepo;

public interface IFinalProjectRepository : IGenericRepository<FinalProjectSubmission>
{
    /// <summary>
    /// Get final submission by project ID
    /// </summary>
    Task<FinalProjectSubmission?> GetByProjectIdAsync(int projectId);

    /// <summary>
    /// Get final submission with all details (project, group, user)
    /// </summary>
    Task<FinalProjectSubmission?> GetByProjectIdWithDetailsAsync(int projectId);

    /// <summary>
    /// Check if project has final submission
    /// </summary>
    Task<bool> HasSubmissionAsync(int projectId);

    /// <summary>
    /// Check if can update submission (before deadline)
    /// </summary>
    Task<bool> CanUpdateAsync(int projectId);

    /// <summary>
    /// Get all final submissions for a class (Instructor view)
    /// </summary>
    Task<List<FinalProjectSubmission>> GetByClassIdAsync(int classId);

    /// <summary>
    /// Get all graded final submissions
    /// </summary>
    Task<List<FinalProjectSubmission>> GetGradedSubmissionsAsync(int classId);

    /// <summary>
    /// Get all ungraded final submissions
    /// </summary>
    Task<List<FinalProjectSubmission>> GetUngradedSubmissionsAsync(int classId);
}
