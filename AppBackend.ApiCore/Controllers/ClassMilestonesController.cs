using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.Services.ProjectMilestone;
using AppBackend.Services.ApiModels.Commons;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

/// <summary>
/// Controller for managing milestones at class level
/// </summary>
[ApiController]
[Route("api/classes/{classId}/milestones")]
[Authorize]
public class ClassMilestonesController : ControllerBase
{
    private readonly IProjectMilestoneService _milestoneService;
    private readonly ILogger<ClassMilestonesController> _logger;

    public ClassMilestonesController(
        IProjectMilestoneService milestoneService,
        ILogger<ClassMilestonesController> logger)
    {
        _milestoneService = milestoneService;
        _logger = logger;
    }

    /// <summary>
    /// Create milestone for all approved projects in a class
    /// Only instructors can use this endpoint
    /// </summary>
    /// <param name="classId">Class ID from route</param>
    /// <param name="request">Milestone details</param>
    /// <returns>Details of created milestones</returns>
    [HttpPost("bulk-create")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<ResultModel<BulkCreateMilestoneResponseDto>>> BulkCreateMilestone(
        [FromRoute] int classId,
        [FromBody] BulkCreateMilestoneRequestDto request)
    {
        try
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<BulkCreateMilestoneResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid request data",
                    StatusCode = 400
                });
            }

            // Get instructor ID from claims
            var instructorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorIdClaim))
            {
                return Unauthorized(new ResultModel<BulkCreateMilestoneResponseDto>
                {
                    IsSuccess = false,
                    Message = "User not authenticated",
                    StatusCode = 401
                });
            }

            // Set classId from route
            request.ClassId = classId;

            _logger.LogInformation(
                "Instructor {InstructorId} is creating milestone '{Title}' for all approved projects in class {ClassId}",
                instructorIdClaim, request.Title, classId);

            // Call service
            var result = await _milestoneService.BulkCreateMilestoneForClassAsync(request);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BulkCreateMilestone for class {ClassId}", classId);
            return StatusCode(500, new ResultModel<BulkCreateMilestoneResponseDto>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}",
                StatusCode = 500
            });
        }
    }
}
