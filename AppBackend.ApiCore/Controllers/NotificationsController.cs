using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.Notification;
using AppBackend.Services.ApiModels.Commons;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get all notifications for current user (with pagination)
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    [HttpGet]
    public async Task<ActionResult<ResultModel<List<NotificationResponseDto>>>> GetMyNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new ResultModel<List<NotificationResponseDto>>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _notificationService.GetUserNotificationsAsync(userId, pageNumber, pageSize);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultModel<List<NotificationResponseDto>>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get unread notifications for current user
    /// </summary>
    [HttpGet("unread")]
    public async Task<ActionResult<ResultModel<List<NotificationResponseDto>>>> GetUnreadNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new ResultModel<List<NotificationResponseDto>>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _notificationService.GetUnreadNotificationsAsync(userId);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultModel<List<NotificationResponseDto>>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get notification summary (count, unread count, etc.)
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ResultModel<NotificationSummaryDto>>> GetNotificationSummary()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new ResultModel<NotificationSummaryDto>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _notificationService.GetNotificationSummaryAsync(userId);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultModel<NotificationSummaryDto>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    [HttpPut("{notificationId}/mark-read")]
    public async Task<ActionResult<ResultModel<bool>>> MarkAsRead(int notificationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _notificationService.MarkAsReadAsync(notificationId, userId);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultModel<bool>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("mark-all-read")]
    public async Task<ActionResult<ResultModel<int>>> MarkAllAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new ResultModel<int>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _notificationService.MarkAllAsReadAsync(userId);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultModel<int>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    [HttpDelete("{notificationId}")]
    public async Task<ActionResult<ResultModel<bool>>> DeleteNotification(int notificationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _notificationService.DeleteNotificationAsync(notificationId, userId);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultModel<bool>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Delete all read notifications
    /// </summary>
    [HttpDelete("read")]
    public async Task<ActionResult<ResultModel<int>>> DeleteReadNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new ResultModel<int>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _notificationService.DeleteReadNotificationsAsync(userId);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultModel<int>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Send notification to a user (Admin/Instructor only)
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<ActionResult<ResultModel<NotificationResponseDto>>> SendNotification(
        [FromBody] NotificationCreateRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<NotificationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid request"
                });
            }

            var result = await _notificationService.SendNotificationAsync(request);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultModel<NotificationResponseDto>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Send notification to multiple users (Admin/Instructor only)
    /// </summary>
    [HttpPost("send-bulk")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<ActionResult<ResultModel<BulkNotificationResponseDto>>> SendBulkNotification(
        [FromBody] BulkNotificationRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<BulkNotificationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid request"
                });
            }

            var result = await _notificationService.SendBulkNotificationAsync(request);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultModel<BulkNotificationResponseDto>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Send notification to all students in a class (Instructor only)
    /// </summary>
    [HttpPost("send-to-class")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<ResultModel<BulkNotificationResponseDto>>> SendClassNotification(
        [FromBody] ClassNotificationRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<BulkNotificationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid request"
                });
            }

            var result = await _notificationService.SendClassNotificationAsync(request);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultModel<BulkNotificationResponseDto>
            {
                IsSuccess = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }
}

