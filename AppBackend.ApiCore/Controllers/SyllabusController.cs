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
    /// Upload file to syllabus (Instructor only - must be owner)
    /// </summary>
    [HttpPost("files")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<ResultModel<SyllabusFileDto>>> UploadFile([FromBody] SyllabusFileUploadRequestDto request)
    {
        var instructorId = GetUserId();
        var result = await _syllabusService.UploadFileAsync(request, instructorId);
        
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetFilesBySyllabus), new { syllabusId = request.SyllabusId }, result);
        
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
