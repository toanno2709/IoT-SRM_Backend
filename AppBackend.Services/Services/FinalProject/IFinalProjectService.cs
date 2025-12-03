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
}
