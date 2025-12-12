using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.Syllabus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyllabusController : ControllerBase
{
    private readonly ISyllabusService _syllabusService;

    public SyllabusController(ISyllabusService syllabusService)
    {
        _syllabusService = syllabusService;
    }

    /// <summary>
    /// Create a new syllabus (Instructor only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<ResultModel<SyllabusResponseDto>>> CreateSyllabus([FromBody] SyllabusCreateRequestDto request)
    {
        var instructorId = GetUserId();
        var result = await _syllabusService.CreateSyllabusAsync(request, instructorId);
        
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetSyllabusById), new { id = result.Data!.SyllabusId }, result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get syllabus by ID (All authenticated users)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResultModel<SyllabusResponseDto>>> GetSyllabusById(int id)
    {
        var result = await _syllabusService.GetSyllabusByIdAsync(id);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all syllabuses for a class (All authenticated users)
    /// </summary>
    [HttpGet("class/{classId}")]
    public async Task<ActionResult<ResultModel<List<SyllabusListItemDto>>>> GetSyllabusesByClass(int classId)
    {
        var result = await _syllabusService.GetSyllabusesByClassIdAsync(classId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all syllabuses created by current instructor
    /// </summary>
    [HttpGet("my-syllabuses")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<ResultModel<List<SyllabusListItemDto>>>> GetMySyllabuses()
    {
        var instructorId = GetUserId();
        var result = await _syllabusService.GetSyllabusesByInstructorIdAsync(instructorId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update syllabus (Instructor only - must be owner)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<ResultModel<SyllabusResponseDto>>> UpdateSyllabus(int id, [FromBody] SyllabusUpdateRequestDto request)
    {
        var instructorId = GetUserId();
        var result = await _syllabusService.UpdateSyllabusAsync(id, request, instructorId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete syllabus (Instructor only - must be owner)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<ResultModel<bool>>> DeleteSyllabus(int id)
    {
        var instructorId = GetUserId();
        var result = await _syllabusService.DeleteSyllabusAsync(id, instructorId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Upload file to syllabus from device (Instructor only - must be owner)
    /// </summary>
    /// <param name="syllabusId">Syllabus ID</param>
    /// <param name="file">File to upload from device</param>
    /// <param name="description">Optional file description</param>
    /// <param name="displayOrder">Optional display order</param>
    /// <returns>Uploaded file information with Cloudinary URL</returns>
    /// <remarks>
    /// Upload a file to a syllabus from any device using multipart/form-data.
    /// Files are automatically uploaded to Cloudinary cloud storage.
    /// 
    /// **Supported file types:**
    /// - Documents: PDF, DOCX, DOC, XLSX, XLS, PPTX, PPT, TXT
    /// - Images: JPG, PNG, GIF, SVG
    /// - Archives: ZIP, RAR
    /// - Videos: MP4, AVI, MOV
    /// - And more...
    /// 
    /// **Maximum file size:** 100 MB
    /// 
    /// **Sample request (multipart/form-data):**
    /// ```
    /// syllabusId: 1
    /// file: [select file from device]
    /// description: "Course materials for Chapter 1" (optional)
    /// displayOrder: 1 (optional)
    /// ```
    /// 
    /// **Sample response:**
    /// ```json
    /// {
    ///   "isSuccess": true,
    ///   "message": "File uploaded successfully",
    ///   "data": {
    ///     "fileId": 123,
    ///     "fileName": "chapter1.pdf",
    ///     "fileUrl": "https://res.cloudinary.com/.../syllabuses/chapter1.pdf",
    ///     "fileSize": 1024000,
    ///     "uploadedAt": "2024-01-15T10:30:00Z"
    ///   }
    /// }
    /// ```
    /// 
    /// **Error responses:**
    /// - 400: Invalid file or file too large
    /// - 403: Not the syllabus owner
    /// - 404: Syllabus not found
    /// - 500: Upload failed
    /// </remarks>
    [HttpPost("files")]
    [Authorize(Roles = "Instructor")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResultModel<SyllabusFileDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(104857600)] // 100 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 104857600)] // 100 MB
    public async Task<ActionResult<ResultModel<SyllabusFileDto>>> UploadFile(
        [FromForm] int syllabusId,
        [FromForm] IFormFile file,
        [FromForm] string? description = null,
        [FromForm] int? displayOrder = null)
    {
        if (syllabusId <= 0)
        {
            return BadRequest(new ResultModel<SyllabusFileDto>
            {
                IsSuccess = false,
                Message = "Invalid syllabus ID",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new ResultModel<SyllabusFileDto>
            {
                IsSuccess = false,
                Message = "File is required",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var instructorId = GetUserId();
        if (instructorId == 0)
        {
            return Unauthorized(new ResultModel<SyllabusFileDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        var result = await _syllabusService.UploadFileAsync(syllabusId, file, description, displayOrder, instructorId);
        
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetFilesBySyllabus), new { syllabusId }, result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all files for a syllabus (All authenticated users)
    /// </summary>
    [HttpGet("{syllabusId}/files")]
    public async Task<ActionResult<ResultModel<List<SyllabusFileDto>>>> GetFilesBySyllabus(int syllabusId)
    {
        var result = await _syllabusService.GetFilesBySyllabusIdAsync(syllabusId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update file information (Instructor only - must be owner)
    /// </summary>
    [HttpPut("files/{fileId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<ResultModel<SyllabusFileDto>>> UpdateFile(int fileId, [FromBody] SyllabusFileUpdateRequestDto request)
    {
        var instructorId = GetUserId();
        var result = await _syllabusService.UpdateFileAsync(fileId, request, instructorId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete file (Instructor only - must be owner)
    /// </summary>
    [HttpDelete("files/{fileId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<ResultModel<bool>>> DeleteFile(int fileId)
    {
        var instructorId = GetUserId();
        var result = await _syllabusService.DeleteFileAsync(fileId, instructorId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    #region Private Helpers

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    #endregion
}
