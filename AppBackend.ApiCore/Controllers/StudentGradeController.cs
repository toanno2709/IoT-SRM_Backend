using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.StudentGrade;
using AppBackend.Services.ApiModels.Commons;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

[ApiController]
[Route("api/student")]
[Authorize(Roles = "Student")]
public class StudentGradeController : ControllerBase
{
    private readonly IStudentGradeService _gradeService;

    public StudentGradeController(IStudentGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    /// <summary>
    /// Get all grades for the current student
    /// </summary>
    /// <returns>All project grades with milestone breakdown</returns>
    /// <remarks>
    /// Returns grades for all projects the student is involved in,
    /// including milestone-by-milestone breakdown and weighted averages.
    /// </remarks>
    [HttpGet("my-grades")]
    [ProducesResponseType(typeof(ResultModel<StudentGradesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<StudentGradesResponseDto>>> GetMyGrades()
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

        var result = await _gradeService.GetMyGradesAsync(userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get grades for a specific project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Project grades with milestone details</returns>
    [HttpGet("projects/{projectId}/grades")]
    [ProducesResponseType(typeof(ResultModel<StudentProjectGradeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<StudentProjectGradeDto>>> GetProjectGrades([FromRoute] int projectId)
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

        var result = await _gradeService.GetProjectGradesAsync(projectId, userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all feedback for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Proposal feedback and milestone feedback</returns>
    /// <remarks>
    /// Includes:
    /// - Proposal approval/rejection feedback
    /// - Milestone-by-milestone feedback from instructor
    /// - Final project feedback (if available)
    /// </remarks>
    [HttpGet("projects/{projectId}/feedback")]
    [ProducesResponseType(typeof(ResultModel<ProjectFeedbackResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<ProjectFeedbackResponseDto>>> GetProjectFeedback([FromRoute] int projectId)
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

        var result = await _gradeService.GetProjectFeedbackAsync(projectId, userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get calculated overall grade for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Overall weighted grade with breakdown</returns>
    /// <remarks>
    /// Calculates overall grade using weighted average of milestone grades.
    /// Shows contribution of each milestone to final grade.
    /// </remarks>
    [HttpGet("projects/{projectId}/overall-grade")]
    [ProducesResponseType(typeof(ResultModel<ProjectOverallGradeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<ProjectOverallGradeDto>>> GetProjectOverallGrade([FromRoute] int projectId)
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

        var result = await _gradeService.GetProjectOverallGradeAsync(projectId, userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }
}
