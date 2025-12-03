using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.StudentDashboard;
using AppBackend.Services.Services.ProjectTemplate;
using AppBackend.Services.ApiModels.Commons;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

/// <summary>
/// Student Dashboard Controller - Provides student-specific dashboard and convenience endpoints
/// </summary>
[ApiController]
[Route("api/student")]
[Authorize(Roles = "Student")]
public class StudentDashboardController : ControllerBase
{
    private readonly IStudentDashboardService _dashboardService;
    private readonly IProjectTemplateService _templateService;
    private readonly ILogger<StudentDashboardController> _logger;

    public StudentDashboardController(
        IStudentDashboardService dashboardService,
        IProjectTemplateService templateService,
        ILogger<StudentDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Get student dashboard with statistics, deadlines, grades, and notifications
    /// </summary>
    /// <returns>Dashboard overview data</returns>
    /// <remarks>
    /// Returns comprehensive dashboard data including:
    /// - Statistics (total classes, groups, projects, average grade)
    /// - Upcoming deadlines (next 10 deadlines)
    /// - Recent grades (last 5 grades)
    /// - Recent notifications (last 5 notifications)
    /// 
    /// This is a convenience endpoint that aggregates data from multiple sources.
    /// </remarks>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ResultModel<StudentDashboardResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<StudentDashboardResponseDto>>> GetDashboard()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ResultModel<StudentDashboardResponseDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Getting dashboard for student {UserId}", userId);

        var result = await _dashboardService.GetDashboardAsync(userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all classes enrolled by the student
    /// </summary>
    /// <returns>List of classes with group information</returns>
    /// <remarks>
    /// Returns all classes the student is enrolled in, including:
    /// - Class details (name, semester, instructor)
    /// - Student's group in each class (if exists)
    /// - Group role (Leader or Member)
    /// - Enrollment date
    /// </remarks>
    [HttpGet("my-classes")]
    [ProducesResponseType(typeof(ResultModel<List<StudentClassDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<List<StudentClassDto>>>> GetMyClasses()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ResultModel<List<StudentClassDto>>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Getting classes for student {UserId}", userId);

        var result = await _dashboardService.GetMyClassesAsync(userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get student's group in a specific class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>Group details with members and project</returns>
    /// <remarks>
    /// Quick access to student's group in a specific class.
    /// Returns:
    /// - Group details (name, class)
    /// - Student's role (Leader or Member)
    /// - All group members
    /// - Project information (if exists)
    /// 
    /// Returns 404 if student is not in a group for this class.
    /// </remarks>
    [HttpGet("my-group")]
    [ProducesResponseType(typeof(ResultModel<StudentGroupDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<StudentGroupDetailDto>>> GetMyGroup([FromQuery] int classId)
    {
        if (classId <= 0)
        {
            return BadRequest(new ResultModel<StudentGroupDetailDto>
            {
                IsSuccess = false,
                Message = "Invalid class ID",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ResultModel<StudentGroupDetailDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Getting group for student {UserId} in class {ClassId}", userId, classId);

        var result = await _dashboardService.GetMyGroupAsync(userId, classId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get pending group invitations for the student
    /// </summary>
    /// <returns>List of pending invitations</returns>
    /// <remarks>
    /// Returns all unread group invitations for the student, including:
    /// - Group name and class
    /// - Who invited the student
    /// - Invitation date
    /// - Notification ID (for accepting/rejecting)
    /// 
    /// Only shows invitations that haven't been read/actioned yet.
    /// </remarks>
    [HttpGet("group-invitations")]
    [ProducesResponseType(typeof(ResultModel<GroupInvitationsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResultModel<GroupInvitationsResponseDto>>> GetGroupInvitations()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ResultModel<GroupInvitationsResponseDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Getting group invitations for student {UserId}", userId);

        var result = await _dashboardService.GetGroupInvitationsAsync(userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Reject a group invitation
    /// </summary>
    /// <param name="request">Rejection details (groupId and optional reason)</param>
    /// <returns>Rejection confirmation</returns>
    /// <remarks>
    /// Allows student to explicitly reject a group invitation.
    /// 
    /// Actions performed:
    /// - Marks the invitation notification as read
    /// - Sends a rejection notification to the group leader
    /// - Includes optional reason in the notification
    /// </remarks>
    [HttpPost("group-invitations/reject")]
    [ProducesResponseType(typeof(ResultModel<RejectInvitationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<RejectInvitationResponseDto>>> RejectGroupInvitation(
        [FromBody] GroupRejectInviteDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<RejectInvitationResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ResultModel<RejectInvitationResponseDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Student {UserId} rejecting invitation to group {GroupId}", userId, request.GroupId);

        var result = await _dashboardService.RejectGroupInvitationAsync(userId, request.GroupId, request.Reason);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Accept a group invitation
    /// </summary>
    /// <param name="groupId">Group ID to accept invitation for</param>
    /// <returns>Acceptance confirmation with group details</returns>
    /// <remarks>
    /// Allows student to accept a group invitation.
    /// 
    /// Actions performed:
    /// - Adds the student to the group as a member
    /// - Marks the invitation notification as read
    /// - Sends an acceptance notification to the group leader
    /// 
    /// Prerequisites:
    /// - Student must have a pending invitation to the group
    /// - Student must not already be a member of the group
    /// </remarks>
    [HttpPost("group-invitations/{groupId}/accept")]
    [ProducesResponseType(typeof(ResultModel<AcceptInvitationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultModel<AcceptInvitationResponseDto>>> AcceptGroupInvitation(
        [FromRoute] int groupId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ResultModel<AcceptInvitationResponseDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Student {UserId} accepting invitation to group {GroupId}", userId, groupId);

        var result = await _dashboardService.AcceptGroupInvitationAsync(userId, groupId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #region Project Templates

    /// <summary>
    /// Get available project templates for a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of available templates with registration status</returns>
    /// <remarks>
    /// Shows templates that:
    /// - Are active (is_active = true)
    /// - Have available slots (registered_count &lt; max_groups OR max_groups IS NULL)
    /// 
    /// Response includes:
    /// - Template details (title, description, component)
    /// - Available slots (null = unlimited)
    /// - Whether current student's group already registered
    /// - Milestone preview
    /// 
    /// Students can only see templates from classes they're enrolled in.
    /// </remarks>
    [HttpGet("classes/{classId}/templates")]
    [ProducesResponseType(typeof(ResultModel<List<AvailableTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<List<AvailableTemplateDto>>>> GetAvailableTemplates(
        [FromRoute] int classId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ResultModel<List<AvailableTemplateDto>>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Student {UserId} getting available templates for class {ClassId}", userId, classId);

        var result = await _templateService.GetAvailableTemplatesAsync(classId, userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Register group to a project template
    /// </summary>
    /// <param name="dto">Registration request (templateId and groupId)</param>
    /// <returns>Registration confirmation with auto-created project details</returns>
    /// <remarks>
    /// Automatically creates a project from template when group registers.
    /// 
    /// Requirements:
    /// - Must be group leader
    /// - Group must not already have a project
    /// - Template must have available slots
    /// - Template must be active
    /// 
    /// Actions performed automatically:
    /// 1. Creates a new project from template
    /// 2. Copies milestones with calculated due dates
    /// 3. Creates registration record (status = 'Active')
    /// 4. Increments template's registered_count
    /// 5. Sends notification to group members
    /// 
    /// Example due date calculation:
    /// - Milestone 1: DaysDuration=7 ? Due: Today + 7 days
    /// - Milestone 2: DaysDuration=14 ? Due: Milestone1.Due + 14 days
    /// - Milestone 3: DaysDuration=7 ? Due: Milestone2.Due + 7 days
    /// </remarks>
    [HttpPost("templates/register")]
    [ProducesResponseType(typeof(ResultModel<TemplateRegistrationResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<TemplateRegistrationResponseDto>>> RegisterToTemplate(
        [FromBody] RegisterTemplateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<TemplateRegistrationResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ResultModel<TemplateRegistrationResponseDto>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Student {UserId} registering group {GroupId} to template {TemplateId}", 
            userId, dto.GroupId, dto.TemplateId);

        var result = await _templateService.RegisterToTemplateAsync(dto, userId);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetAvailableTemplates), 
                new { classId = result.Data!.GroupId }, result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Cancel template registration
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <returns>Cancellation confirmation</returns>
    /// <remarks>
    /// Only group leader can cancel registration.
    /// Cannot cancel if project has submissions.
    /// 
    /// Actions:
    /// - Sets registration status to 'Cancelled'
    /// - Decrements template's registered_count
    /// - Does NOT delete the project (keeps history)
    /// </remarks>
    [HttpDelete("templates/registrations/{registrationId}")]
    [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultModel<bool>>> CancelRegistration(
        [FromRoute] int registrationId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ResultModel<bool>
            {
                IsSuccess = false,
                Message = "User not authenticated",
                StatusCode = StatusCodes.Status401Unauthorized
            });
        }

        _logger.LogInformation("Student {UserId} cancelling registration {RegistrationId}", userId, registrationId);

        var result = await _templateService.CancelRegistrationAsync(registrationId, userId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion
}
