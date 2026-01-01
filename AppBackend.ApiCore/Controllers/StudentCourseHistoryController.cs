using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.StudentCourseHistory;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Attributes;

namespace AppBackend.ApiCore.Controllers;

/// <summary>
/// Student Course History Management (Admin only)
/// Tracks student progress and completion status for IoT course
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class StudentCourseHistoryController : ControllerBase
{
    private readonly IStudentCourseHistoryService _service;

    public StudentCourseHistoryController(IStudentCourseHistoryService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get student course history by ID
    /// </summary>
    [HttpGet("{historyId}")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<StudentCourseHistoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<StudentCourseHistoryResponseDto>>> GetById([FromRoute] int historyId)
    {
        var result = await _service.GetByIdAsync(historyId);
        
        if (result.IsSuccess)
            return Ok(result);
            
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get current course history for a student
    /// </summary>
    [HttpGet("student/{studentId}/current")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<StudentCourseHistoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<StudentCourseHistoryResponseDto>>> GetCurrentByStudentId([FromRoute] int studentId)
    {
        var result = await _service.GetCurrentByStudentIdAsync(studentId);
        
        if (result.IsSuccess)
            return Ok(result);
            
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all course history records for a student
    /// </summary>
    [HttpGet("student/{studentId}/all")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<List<StudentCourseHistoryResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<List<StudentCourseHistoryResponseDto>>>> GetAllByStudentId([FromRoute] int studentId)
    {
        var result = await _service.GetAllByStudentIdAsync(studentId);
        
        if (result.IsSuccess)
            return Ok(result);
            
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get students grouped by course completion status
    /// </summary>
    /// <remarks>
    /// Returns students grouped by status:
    /// - Not Started
    /// - In Progress
    /// - Pass
    /// - Not Pass
    /// - Withdrawn
    /// </remarks>
    [HttpGet("by-status")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<List<StudentsByStatusResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<List<StudentsByStatusResponseDto>>>> GetStudentsByStatus()
    {
        var result = await _service.GetStudentsByStatusAsync();
        
        if (result.IsSuccess)
            return Ok(result);
            
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Create a new student course history record
    /// </summary>
    /// <remarks>
    /// Creates a new history record and marks any existing current record as not current.
    /// Only one record per student should have IsCurrent = true.
    /// 
    /// Valid statuses:
    /// - Not Started
    /// - In Progress
    /// - Pass
    /// - Not Pass
    /// - Withdrawn
    /// </remarks>
    [HttpPost]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<StudentCourseHistoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<StudentCourseHistoryResponseDto>>> Create([FromBody] StudentCourseHistoryCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Invalid request data"
            });
        }

        var result = await _service.CreateAsync(dto);
        
        if (result.IsSuccess)
            return Ok(result);
            
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update student course history
    /// </summary>
    [HttpPut("{historyId}")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<StudentCourseHistoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<StudentCourseHistoryResponseDto>>> Update(
        [FromRoute] int historyId,
        [FromBody] StudentCourseHistoryUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Invalid request data"
            });
        }

        var result = await _service.UpdateAsync(historyId, dto);
        
        if (result.IsSuccess)
            return Ok(result);
            
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update student course status
    /// </summary>
    /// <remarks>
    /// Updates the status of the current course history record for a student.
    /// Automatically sets CompletedAt when status changes to Pass or Not Pass.
    /// </remarks>
    [HttpPut("student/{studentId}/status")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<StudentCourseHistoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<StudentCourseHistoryResponseDto>>> UpdateStatus(
        [FromRoute] int studentId,
        [FromBody] UpdateStudentCourseStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Invalid request data"
            });
        }

        var result = await _service.UpdateStatusAsync(studentId, dto);
        
        if (result.IsSuccess)
            return Ok(result);
            
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete student course history record
    /// </summary>
    [HttpDelete("{historyId}")]
    [RateLimit(permitLimit: 10, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<bool>>> Delete([FromRoute] int historyId)
    {
        var result = await _service.DeleteAsync(historyId);
        
        if (result.IsSuccess)
            return Ok(result);
            
        return StatusCode(result.StatusCode, result);
    }
}
