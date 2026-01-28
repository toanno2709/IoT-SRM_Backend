using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Submission;

public interface ISubmissionService
{
    /// <summary>
    /// Submit a milestone (create or update submission)
    /// </summary>
    Task<ResultModel<MilestoneSubmissionResponseDto>> SubmitMilestoneAsync(MilestoneSubmissionRequestDto request, int userId);

    /// <summary>
    /// Get submission history for a milestone
    /// </summary>
    Task<ResultModel<MilestoneSubmissionHistoryDto>> GetSubmissionHistoryAsync(int projectId, int milestoneDefId, int userId);

    /// <summary>
    /// Get latest submission for a milestone
    /// </summary>
    Task<ResultModel<MilestoneSubmissionResponseDto>> GetLatestSubmissionAsync(int projectId, int milestoneDefId, int userId);

    /// <summary>
    /// Upload files to a submission
    /// </summary>
    Task<ResultModel<FileUploadResponseDto>> UploadFilesAsync(int submissionId, List<Microsoft.AspNetCore.Http.IFormFile> files, int userId);

    /// <summary>
    /// Get submission files
    /// </summary>
    Task<ResultModel<List<MilestoneFileDto>>> GetSubmissionFilesAsync(int submissionId, int? versionNo = null);

    /// <summary>
    /// Delete a submission file
    /// </summary>
    Task<ResultModel<bool>> DeleteFileAsync(int fileId, int userId);

    /// <summary>
    /// Get file information with authorization check
    /// </summary>
    Task<ResultModel<MilestoneFileDto>> GetFileInfoAsync(int fileId, int userId);
}
