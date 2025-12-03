using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.FinalProject;
using AppBackend.Services.ApiModels.Commons;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

[ApiController]
[Route("api/student/projects")]

public class FinalProjectController : ControllerBase
{
    private readonly IFinalProjectService _finalProjectService;

    public FinalProjectController(IFinalProjectService finalProjectService)
    {
        _finalProjectService = finalProjectService;
    }

    /// <summary>
    /// Submit final project deliverables (create initial submission)
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="request">Submission details (notes and repository URL)</param>
    /// <returns>Submission confirmation</returns>
    /// <remarks>
    /// This creates the initial final project submission record.
    /// After creating, use the upload endpoint to add files.
    /// Can only be done once per project.
    /// </remarks>
    [HttpPost("{projectId}/final-submission")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ResultModel<FinalProjectSubmissionResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<FinalProjectSubmissionResponseDto>>> SubmitFinalProject(
        [FromRoute] int projectId,
        [FromBody] FinalProjectSubmissionRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<FinalProjectSubmissionResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid input data",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                isSuccess = false,
                message = "User not authenticated"
            });
        }

        var result = await _finalProjectService.SubmitFinalProjectAsync(projectId, request, userId);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetFinalSubmission), new { projectId }, result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Upload files for final project submission
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="files">Files to upload</param>
    /// <returns>Upload status for each file</returns>
    /// <remarks>
    /// Upload final project files to Cloudinary.
    /// All files are optional - you can upload them separately.
    /// Can be called multiple times to update files before deadline.
    /// Maximum file size: 500MB per file.
    /// 
    /// Sample request using form-data:
    /// - finalReport: [file]
    /// - presentation: [file]
    /// - sourceCode: [file]
    /// - videoDemo: [file]
    /// </remarks>
    [HttpPost("{projectId}/final-submission/upload")]
    [Authorize(Roles = "Student")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResultModel<FinalProjectFileUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(524288000)] // 500 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)] // 500 MB
    public async Task<ActionResult<ResultModel<FinalProjectFileUploadResponseDto>>> UploadFinalProjectFiles(
        [FromRoute] int projectId,
        [FromForm] FinalProjectFileUploadRequest files)
    {
        if (files.FinalReport == null && files.Presentation == null && files.SourceCode == null && files.VideoDemo == null)
        {
            return BadRequest(new
            {
                isSuccess = false,
                message = "At least one file must be provided"
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                isSuccess = false,
                message = "User not authenticated"
            });
        }

        var result = await _finalProjectService.UploadFilesAsync(
            projectId, files.FinalReport, files.Presentation, files.SourceCode, files.VideoDemo, userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get final project submission
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Final submission details with all files and grade</returns>
    [HttpGet("{projectId}/final-submission")]
    [Authorize(Roles = "Student,Instructor")] // ? Allow both Student and Instructor
    [ProducesResponseType(typeof(ResultModel<FinalProjectSubmissionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<FinalProjectSubmissionResponseDto>>> GetFinalSubmission(
        [FromRoute] int projectId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                isSuccess = false,
                message = "User not authenticated"
            });
        }

        var result = await _finalProjectService.GetFinalSubmissionAsync(projectId, userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update final project submission (notes and repository URL)
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="request">Updated submission details</param>
    /// <returns>Updated submission</returns>
    /// <remarks>
    /// Can only update before the deadline.
    /// Use this to update submission notes or repository URL.
    /// To update files, use the upload endpoint.
    /// </remarks>
    [HttpPut("{projectId}/final-submission")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ResultModel<FinalProjectSubmissionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<FinalProjectSubmissionResponseDto>>> UpdateFinalSubmission(
        [FromRoute] int projectId,
        [FromBody] FinalProjectUpdateRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<FinalProjectSubmissionResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid input data",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                isSuccess = false,
                message = "User not authenticated"
            });
        }

        var result = await _finalProjectService.UpdateFinalSubmissionAsync(projectId, request, userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete a file from final submission
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="fileType">Type of file to delete: report, presentation, sourcecode, video</param>
    /// <returns>Success status</returns>
    /// <remarks>
    /// Can only delete before the deadline.
    /// File is also deleted from Cloudinary cloud storage.
    /// Valid fileType values: report, presentation, sourcecode, video
    /// </remarks>
    [HttpDelete("{projectId}/final-submission/files/{fileType}")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<bool>>> DeleteFile(
        [FromRoute] int projectId,
        [FromRoute] string fileType)
    {
        var validTypes = new[] { "report", "presentation", "sourcecode", "video" };
        if (!validTypes.Contains(fileType.ToLower()))
        {
            return BadRequest(new
            {
                isSuccess = false,
                message = "Invalid file type. Valid types: report, presentation, sourcecode, video"
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                isSuccess = false,
                message = "User not authenticated"
            });
        }

        var result = await _finalProjectService.DeleteFileAsync(projectId, fileType, userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }
}
