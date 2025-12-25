using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.ProjectTemplate;
using AppBackend.Services.Services.ClassConfig;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

/// <summary>
/// Student-specific endpoints for project template management
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
    /// <param name="classId">Class ID</param>
    /// <returns>Class configuration (read-only)</returns>
    /// <remarks>
    /// Allows students to view class configuration such as:
    /// - Max groups allowed in class
    /// - Min/max members per group
    /// - Group formation deadline
    /// - Whether students can create groups
    /// 
    /// This is read-only for students. Only instructors can modify configuration.
    /// </remarks>
    [HttpGet("classes/{classId}/config")]
    [ProducesResponseType(typeof(ResultModel<ClassConfigResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    #endregion

    #region Project Templates

    /// <summary>
    /// Get all available project templates for a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of available templates with registration status</returns>
    /// <remarks>
    /// Returns all active project templates in a class that students can view and register to.
    /// 
    /// Features:
    /// - Shows which templates have available slots
    /// - Indicates if your group is already registered
    /// - Shows how many groups have registered
    /// - Displays all milestone details for each template
    /// - Shows whether you can register (requires being group leader)
    /// 
    /// CanRegister is true when:
    /// - Template is active
    /// - Has available slots (or unlimited)
    /// - Student is in a group in this class
    /// - Student's group hasn't registered yet
    /// - Student is the group leader
    /// 
    /// Use this endpoint to browse available project templates before registration.
    /// </remarks>
    [HttpGet("classes/{classId}/templates")]
    [ProducesResponseType(typeof(ResultModel<List<AvailableTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// <param name="request">Registration data with template and group IDs</param>
    /// <returns>Registration confirmation with created project and milestones</returns>
    /// <remarks>
    /// Allows group leader to register their group to a project template.
    /// 
    /// **Important:** Only the group leader can register the group to a template.
    /// 
    /// What happens when you register:
    /// 1. System validates:
    ///    - You are the group leader
    ///    - Template has available slots
    ///    - Your group doesn't have an existing project
    ///    - Your group hasn't registered this template before
    /// 
    /// 2. System automatically creates:
    ///    - A new project from the template (title, description, component)
    ///    - All milestones defined in the template
    ///    - Due dates calculated based on milestone durations
    ///    - Registration record linking group to template
    /// 
    /// 3. Template's registered_count is incremented
    /// 
    /// Example response:
    /// ```json
    /// {
    ///   "isSuccess": true,
    ///   "message": "Registration successful! Project and milestones created automatically.",
    ///   "data": {
    ///     "registrationId": 1,
    ///     "templateTitle": "IoT Smart Home System",
    ///     "groupName": "Team Alpha",
    ///     "projectId": 15,
    ///     "projectTitle": "IoT Smart Home System",
    ///     "milestonesCreated": 5,
    ///     "message": "Project created with 5 milestones"
    ///   }
    /// }
    /// ```
    /// 
    /// After registration, your group can start working on the project and submit milestones.
    /// </remarks>
    [HttpPost("templates/register")]
    [ProducesResponseType(typeof(ResultModel<TemplateRegistrationResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// <param name="registrationId">Registration ID to cancel</param>
    /// <returns>Cancellation status</returns>
    /// <remarks>
    /// Allows group leader to cancel their group's template registration.
    /// 
    /// **Important:** Only the group leader can cancel registration.
    /// 
    /// Cancellation rules:
    /// - Can only cancel if no submissions have been made yet
    /// - Can only cancel if you are the group leader
    /// - Registration status will be marked as "Cancelled"
    /// - Template's registered_count is decremented
    /// - Slot becomes available for other groups
    /// 
    /// **Note:** The project and milestones are NOT deleted when cancelling.
    /// This is to preserve any work that might have been done.
    /// If you want to completely remove the project, ask your instructor.
    /// 
    /// Example scenario:
    /// - Group leader accidentally registered wrong template
    /// - No submissions made yet
    /// - Leader can cancel and register to correct template
    /// </remarks>
    [HttpDelete("templates/registrations/{registrationId}")]
    [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// <returns>List of all registrations for groups I'm in</returns>
    /// <remarks>
    /// Returns all template registrations for groups that the current student is a member of.
    /// 
    /// Use cases:
    /// - View all templates your group(s) have registered
    /// - Get registration IDs to cancel registrations
    /// - Check registration status and project details
    /// - See which registrations can be cancelled
    /// 
    /// **CanCancel** is true when:
    /// - Registration status is "Active"
    /// - No milestone submissions have been made yet
    /// - You are the group leader (checked in DELETE endpoint)
    /// 
    /// Example response:
    /// ```json
    /// {
    ///   "isSuccess": true,
    ///   "data": [
    ///     {
    ///       "registrationId": 15,
    ///       "templateTitle": "IoT Smart Home",
    ///       "groupName": "Team Alpha",
    ///       "projectId": 42,
    ///       "status": "Active",
    ///       "canCancel": true
    ///     }
    ///   ]
    /// }
    /// ```
    /// 
    /// To cancel a registration, use the `registrationId` with:
    /// DELETE /api/Student/templates/registrations/{registrationId}
    /// </remarks>
    [HttpGet("templates/registrations/my-group")]
    [ProducesResponseType(typeof(ResultModel<List<MyGroupRegistrationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
