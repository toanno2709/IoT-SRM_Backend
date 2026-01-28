using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.FinalProject;

public interface IFinalProjectService
{
    /// <summary>
    /// Submit final project deliverables
    /// </summary>
    Task<ResultModel<FinalProjectSubmissionResponseDto>> SubmitFinalProjectAsync(
        int projectId, 
        FinalProjectSubmissionRequestDto request, 
        int userId);

    /// <summary>
    /// Upload files for final project submission
    /// </summary>
    Task<ResultModel<FinalProjectFileUploadResponseDto>> UploadFilesAsync(
        int projectId,
        IFormFile? finalReport,
        IFormFile? presentation,
        IFormFile? sourceCode,
        IFormFile? videoDemo,
        int userId);

    /// <summary>
    /// Get final submission by project ID
    /// </summary>
    Task<ResultModel<FinalProjectSubmissionResponseDto>> GetFinalSubmissionAsync(
        int projectId, 
        int userId);

    /// <summary>
    /// Update final submission (before deadline)
    /// </summary>
    Task<ResultModel<FinalProjectSubmissionResponseDto>> UpdateFinalSubmissionAsync(
        int projectId,
        FinalProjectUpdateRequestDto request,
        int userId);

    /// <summary>
    /// Delete a file from final submission
    /// </summary>
    Task<ResultModel<bool>> DeleteFileAsync(
        int projectId,
        string fileType, // "report", "presentation", "sourcecode", "video"
        int userId);

    /// <summary>
    /// Grade final project (Instructor only)
    /// </summary>
    Task<ResultModel<FinalProjectSubmissionResponseDto>> GradeFinalProjectAsync(
        int projectId,
        FinalProjectGradeRequestDto request,
        int instructorId);

    /// <summary>
    /// Get file URL for download with authorization check
    /// </summary>
    Task<ResultModel<string>> GetFileUrlAsync(int projectId, string fileType, int userId);

    /// <summary>
    /// Get file URL for download by submission ID with authorization check (Instructor)
    /// </summary>
    Task<ResultModel<string>> GetFileUrlBySubmissionIdAsync(int finalSubmissionId, string fileType, int userId);
}
