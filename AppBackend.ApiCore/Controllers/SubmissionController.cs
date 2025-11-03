using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.Submission;
using AppBackend.Services.ApiModels.Commons;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

[ApiController]
[Route("api/student/milestones")]
[Authorize(Roles = "Student")]
public class SubmissionController : ControllerBase
{
    private readonly ISubmissionService _submissionService;

    public SubmissionController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    /// <summary>
    /// Submit a milestone (create new or resubmit)
    /// </summary>
    /// <param name="request">Submission details</param>
    /// <returns>Submission confirmation with version number</returns>
    /// <remarks>
    /// Creates a new submission or increments version if resubmitting before deadline.
    /// Files must be uploaded separately using the upload endpoint.
    /// </remarks>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(ResultModel<MilestoneSubmissionResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<MilestoneSubmissionResponseDto>>> SubmitMilestone(
        [FromBody] MilestoneSubmissionRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<MilestoneSubmissionResponseDto>
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

        var result = await _submissionService.SubmitMilestoneAsync(request, userId);
        
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetLatestSubmission), 
                new { projectId = request.ProjectId, milestoneId = request.MilestoneId }, 
                result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get submission history for a milestone
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="milestoneId">Milestone ID</param>
    /// <returns>All versions of submissions with files and grades</returns>
    [HttpGet("{milestoneId}/submissions")]
    [ProducesResponseType(typeof(ResultModel<MilestoneSubmissionHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<MilestoneSubmissionHistoryDto>>> GetSubmissionHistory(
        [FromRoute] int milestoneId,
        [FromQuery] int projectId)
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

        var result = await _submissionService.GetSubmissionHistoryAsync(projectId, milestoneId, userId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get latest submission for a milestone
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="milestoneId">Milestone ID</param>
    /// <returns>Most recent submission with files and grade</returns>
    [HttpGet("{milestoneId}/latest")]
    [ProducesResponseType(typeof(ResultModel<MilestoneSubmissionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<MilestoneSubmissionResponseDto>>> GetLatestSubmission(
        [FromRoute] int milestoneId,
        [FromQuery] int projectId)
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

        var result = await _submissionService.GetLatestSubmissionAsync(projectId, milestoneId, userId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Upload files to a submission
    /// </summary>
    /// <param name="submissionId">Submission ID</param>
    /// <param name="files">Files to upload (multiple files supported)</param>
    /// <returns>List of uploaded files with URLs</returns>
    /// <remarks>
    /// Supports multiple file upload. Maximum file size and allowed types depend on server configuration.
    /// Files are uploaded to Cloudinary cloud storage.
    /// </remarks>
    [HttpPost("{submissionId}/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResultModel<FileUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(104857600)] // 100 MB
    public async Task<ActionResult<ResultModel<FileUploadResponseDto>>> UploadFiles(
        [FromRoute] int submissionId,
        [FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new
            {
                isSuccess = false,
                message = "No files provided"
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

        var result = await _submissionService.UploadFilesAsync(submissionId, files, userId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get files for a submission
    /// </summary>
    /// <param name="submissionId">Submission ID</param>
    /// <param name="versionNo">Optional: Filter by version number</param>
    /// <returns>List of files with download URLs</returns>
    [HttpGet("{submissionId}/files")]
    [ProducesResponseType(typeof(ResultModel<List<MilestoneFileDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<List<MilestoneFileDto>>>> GetSubmissionFiles(
        [FromRoute] int submissionId,
        [FromQuery] int? versionNo = null)
    {
        var result = await _submissionService.GetSubmissionFilesAsync(submissionId, versionNo);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete a file from submission
    /// </summary>
    /// <param name="fileId">File ID to delete</param>
    /// <returns>Success status</returns>
    /// <remarks>
    /// Only the file uploader or group members can delete files.
    /// File is also deleted from Cloudinary cloud storage.
    /// </remarks>
    [HttpDelete("files/{fileId}")]
    [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<bool>>> DeleteFile([FromRoute] int fileId)
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

        var result = await _submissionService.DeleteFileAsync(fileId, userId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }
}
