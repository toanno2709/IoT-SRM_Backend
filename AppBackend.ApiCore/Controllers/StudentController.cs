using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.ProjectTemplate;
using AppBackend.Services.Services.ClassConfig;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

/// <summary>
/// Student-specific endpoints for project template management and class configuration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
public class StudentController : ControllerBase
{
    private readonly IProjectTemplateService _templateService;
    private readonly IClassConfigService _classConfigService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(
        IProjectTemplateService templateService,
        IClassConfigService classConfigService,
        ILogger<StudentController> logger)
    {
        _templateService = templateService;
        _classConfigService = classConfigService;
        _logger = logger;
    }

    #region Class Configuration

    /// <summary>
    /// Get class configuration (Student read-only access)
    /// </summary>
    [HttpGet("classes/{classId}/config")]
    [ProducesResponseType(typeof(ResultModel<ClassConfigResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultModel<ClassConfigResponseDto>>> GetClassConfig([FromRoute] int classId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new ResultModel<ClassConfigResponseDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Student {StudentId} requesting configuration for class {ClassId}", 
            studentId, classId);

        var result = await _classConfigService.GetConfigAsync(classId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Validate submission deadline for a class (Student view)
    /// </summary>
    [HttpGet("classes/{classId}/validate-submission")]
    [ProducesResponseType(typeof(ResultModel<SubmissionDeadlineValidationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultModel<SubmissionDeadlineValidationDto>>> ValidateSubmissionDeadline(
        [FromRoute] int classId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new ResultModel<SubmissionDeadlineValidationDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Student {StudentId} validating submission deadline for class {ClassId}", 
            studentId, classId);

        var result = await _classConfigService.ValidateSubmissionDeadlineAsync(classId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Validate edit window for a class (Student view)
    /// </summary>
    [HttpGet("classes/{classId}/validate-edit-window")]
    [ProducesResponseType(typeof(ResultModel<EditWindowValidationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultModel<EditWindowValidationDto>>> ValidateEditWindow(
        [FromRoute] int classId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new ResultModel<EditWindowValidationDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Student {StudentId} validating edit window for class {ClassId}", 
            studentId, classId);

        var result = await _classConfigService.ValidateEditWindowAsync(classId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region Project Templates

    /// <summary>
    /// Get all available project templates for a class
    /// </summary>
    [HttpGet("classes/{classId}/templates")]
    [ProducesResponseType(typeof(ResultModel<List<AvailableTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultModel<List<AvailableTemplateDto>>>> GetAvailableTemplates(
        [FromRoute] int classId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new ResultModel<List<AvailableTemplateDto>>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Student {StudentId} requesting available templates for class {ClassId}", 
            studentId, classId);

        var result = await _templateService.GetAvailableTemplatesAsync(classId, studentId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Register group to a project template (Group Leader only)
    /// </summary>
    [HttpPost("templates/register")]
    [ProducesResponseType(typeof(ResultModel<TemplateRegistrationResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ResultModel<TemplateRegistrationResponseDto>>> RegisterToTemplate(
        [FromBody] RegisterTemplateDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<TemplateRegistrationResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request data",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new ResultModel<TemplateRegistrationResponseDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation(
            "Student {StudentId} registering group {GroupId} to template {TemplateId}", 
            studentId, request.GroupId, request.TemplateId);

        var result = await _templateService.RegisterToTemplateAsync(request, studentId);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Registration successful. Project {ProjectId} created for group {GroupId}", 
                result.Data?.ProjectId, request.GroupId);
            
            return CreatedAtAction(
                nameof(GetAvailableTemplates), 
                new { classId = result.Data?.GroupId }, 
                result);
        }

        _logger.LogWarning(
            "Registration failed for student {StudentId}, group {GroupId}, template {TemplateId}: {Message}", 
            studentId, request.GroupId, request.TemplateId, result.Message);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Cancel template registration (Group Leader only)
    /// </summary>
    [HttpDelete("templates/registrations/{registrationId}")]
    [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultModel<bool>>> CancelRegistration(
        [FromRoute] int registrationId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new ResultModel<bool>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation(
            "Student {StudentId} attempting to cancel registration {RegistrationId}", 
            studentId, registrationId);

        var result = await _templateService.CancelRegistrationAsync(registrationId, studentId);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Registration {RegistrationId} cancelled successfully by student {StudentId}", 
                registrationId, studentId);
            return Ok(result);
        }

        _logger.LogWarning(
            "Registration cancellation failed for student {StudentId}, registration {RegistrationId}: {Message}", 
            studentId, registrationId, result.Message);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get my group's template registrations
    /// </summary>
    [HttpGet("templates/registrations/my-group")]
    [ProducesResponseType(typeof(ResultModel<List<MyGroupRegistrationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultModel<List<MyGroupRegistrationDto>>>> GetMyGroupRegistrations()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var studentId))
        {
            return Unauthorized(new ResultModel<List<MyGroupRegistrationDto>>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Student {StudentId} requesting their group registrations", studentId);

        var result = await _templateService.GetMyGroupRegistrationsAsync(studentId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion
}
