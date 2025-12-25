using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.StudentDashboard;
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
    private readonly ILogger<StudentDashboardController> _logger;

    public StudentDashboardController(
        IStudentDashboardService dashboardService,
        ILogger<StudentDashboardController> logger)
    {
        _dashboardService = dashboardService;
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
}
