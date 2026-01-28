using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.Submission;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

[ApiController]

public class SubmissionController : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    private readonly ICloudinaryService _cloudinaryService;

    public SubmissionController(ISubmissionService submissionService, ICloudinaryService cloudinaryService)
    {
        _submissionService = submissionService;
        _cloudinaryService = cloudinaryService;
    }

    /// <summary>
    /// Submit a milestone (create new or resubmit)
    /// </summary>
    [HttpPost("api/student/milestones/submit")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ResultModel<MilestoneSubmissionResponseDto>), StatusCodes.Status201Created)]
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
            return Unauthorized(new { isSuccess = false, message = "User not authenticated" });
        }

        var result = await _submissionService.SubmitMilestoneAsync(request, userId);
        
        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get submission history for a project's milestone
    /// </summary>
    [HttpGet("api/student/projects/{projectId}/milestones/{milestoneId}/submissions")]
    [Authorize(Roles = "Student,Instructor")] // ? Allow both Student and Instructor
    [ProducesResponseType(typeof(ResultModel<MilestoneSubmissionHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultModel<MilestoneSubmissionHistoryDto>>> GetSubmissionHistory(
        [FromRoute] int projectId,
        [FromRoute] int milestoneId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { isSuccess = false, message = "User not authenticated" });
        }

        var result = await _submissionService.GetSubmissionHistoryAsync(projectId, milestoneId, userId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get latest submission for a project's milestone
    /// </summary>
    [HttpGet("api/student/projects/{projectId}/milestones/{milestoneId}/latest")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ResultModel<MilestoneSubmissionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultModel<MilestoneSubmissionResponseDto>>> GetLatestSubmission(
        [FromRoute] int projectId,
        [FromRoute] int milestoneId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { isSuccess = false, message = "User not authenticated" });
        }

        var result = await _submissionService.GetLatestSubmissionAsync(projectId, milestoneId, userId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Upload files to a submission
    /// </summary>
    [HttpPost("api/student/milestones/{submissionId}/upload")]
    [Authorize(Roles = "Student")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResultModel<FileUploadResponseDto>), StatusCodes.Status200OK)]
    [RequestSizeLimit(104857600)] // 100 MB
    public async Task<ActionResult<ResultModel<FileUploadResponseDto>>> UploadFiles(
        [FromRoute] int submissionId,
        [FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { isSuccess = false, message = "No files provided" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { isSuccess = false, message = "User not authenticated" });
        }

        var result = await _submissionService.UploadFilesAsync(submissionId, files, userId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get files for a submission
    /// </summary>
    [HttpGet("api/student/milestones/{submissionId}/files")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ResultModel<List<MilestoneFileDto>>), StatusCodes.Status200OK)]
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
    [HttpDelete("api/student/milestones/files/{fileId}")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultModel<bool>>> DeleteFile([FromRoute] int fileId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { isSuccess = false, message = "User not authenticated" });
        }

        var result = await _submissionService.DeleteFileAsync(fileId, userId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Download a file from submission
    /// </summary>
    [HttpGet("api/student/milestones/files/{fileId}/download")]
    [Authorize(Roles = "Student,Instructor")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadFile([FromRoute] int fileId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { isSuccess = false, message = "User not authenticated" });
            }

            // Get file info from database
            var fileResult = await _submissionService.GetFileInfoAsync(fileId, userId);
            
            if (!fileResult.IsSuccess || fileResult.Data == null)
            {
                return StatusCode(fileResult.StatusCode, new { 
                    isSuccess = false, 
                    message = fileResult.Message 
                });
            }

            var fileInfo = fileResult.Data;
            
            // Download from Cloudinary
            var downloadResult = await _cloudinaryService.DownloadFileAsync(fileInfo.FileUrl);
            
            if (downloadResult == null)
            {
                return StatusCode(500, new { 
                    isSuccess = false, 
                    message = "Failed to download file from storage" 
                });
            }

            // Return file
            return File(downloadResult.Value.fileData, downloadResult.Value.contentType, downloadResult.Value.fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                isSuccess = false, 
                message = $"Error downloading file: {ex.Message}" 
            });
        }
    }
}
