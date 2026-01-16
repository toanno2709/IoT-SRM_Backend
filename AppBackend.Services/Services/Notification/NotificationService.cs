using AppBackend.Repositories.Repositories.NotificationRepo;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.Notification;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IClassRepository _classRepository;
    private readonly INotificationHubService _notificationHubService;
    private readonly AppBackend.BusinessObjects.Data.IotShowroomContext _context;

    public NotificationService(
        INotificationRepository notificationRepository,
        IClassRepository classRepository,
        INotificationHubService notificationHubService,
        AppBackend.BusinessObjects.Data.IotShowroomContext context)
    {
        _notificationRepository = notificationRepository;
        _classRepository = classRepository;
        _notificationHubService = notificationHubService;
        _context = context;
    }

    public async Task<ResultModel<NotificationResponseDto>> SendNotificationAsync(NotificationCreateRequestDto request)
    {
        try
        {
            var notification = new BusinessObjects.Models.Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type ?? "info",
                Data = request.Data, // Add Data field
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            var dto = MapToDto(notification);

            // Send real-time notification via SignalR
            var user = await _context.Users.FindAsync(request.UserId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                await _notificationHubService.SendNotificationToUserAsync(user.Email, dto);
            }

            return new ResultModel<NotificationResponseDto>
            {
                IsSuccess = true,
                Message = "Notification sent successfully",
                Data = dto
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<NotificationResponseDto>
            {
                IsSuccess = false,
                Message = $"Error sending notification: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<BulkNotificationResponseDto>> SendBulkNotificationAsync(BulkNotificationRequestDto request)
    {
        try
        {
            var successCount = 0;
            var failedCount = 0;
            var failedUserIds = new List<int>();
            var userEmails = new List<string>();

            foreach (var userId in request.UserIds)
            {
                try
                {
                    var notification = new BusinessObjects.Models.Notification
                    {
                        UserId = userId,
                        Title = request.Title,
                        Message = request.Message,
                        Type = request.Type ?? "info",
                        Data = request.Data, // Add Data field
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _notificationRepository.AddAsync(notification);
                    
                    // Get user email for SignalR
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        userEmails.Add(user.Email);
                    }
                    
                    successCount++;
                }
                catch
                {
                    failedCount++;
                    failedUserIds.Add(userId);
                }
            }

            await _notificationRepository.SaveChangesAsync();

            // Send real-time notifications via SignalR
            if (userEmails.Any())
            {
                var notificationDto = new NotificationResponseDto
                {
                    Title = request.Title,
                    Message = request.Message,
                    Type = request.Type ?? "info",
                    Data = request.Data, // Add Data field
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationHubService.SendNotificationToUsersAsync(userEmails, notificationDto);
            }

            return new ResultModel<BulkNotificationResponseDto>
            {
                IsSuccess = true,
                Message = $"Sent {successCount} notifications successfully, {failedCount} failed",
                Data = new BulkNotificationResponseDto
                {
                    TotalSent = request.UserIds.Count,
                    SuccessCount = successCount,
                    FailedCount = failedCount,
                    FailedUserIds = failedUserIds,
                    Message = $"Bulk notification sent to {successCount} users"
                }
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<BulkNotificationResponseDto>
            {
                IsSuccess = false,
                Message = $"Error sending bulk notification: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<BulkNotificationResponseDto>> SendClassNotificationAsync(ClassNotificationRequestDto request)
    {
        try
        {
            // Lấy tất cả students trong class
            var classEntity = await _classRepository.GetClassWithDetailsAsync(request.ClassId);
            if (classEntity == null)
            {
                return new ResultModel<BulkNotificationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Class not found",
                    Data = null
                };
            }

            var userIds = classEntity.ClassEnrollments
                .Where(e => e.StudentId.HasValue)
                .Select(e => e.StudentId!.Value)
                .ToList();

            // Thêm instructor nếu cần
            if (request.IncludeInstructor && classEntity.InstructorId.HasValue)
            {
                userIds.Add(classEntity.InstructorId.Value);
            }

            if (!userIds.Any())
            {
                return new ResultModel<BulkNotificationResponseDto>
                {
                    IsSuccess = false,
                    Message = "No users found in class",
                    Data = null
                };
            }

            // Gửi bulk notification
            var bulkRequest = new BulkNotificationRequestDto
            {
                UserIds = userIds,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Data = request.Data // Add Data field
            };

            return await SendBulkNotificationAsync(bulkRequest);
        }
        catch (Exception ex)
        {
            return new ResultModel<BulkNotificationResponseDto>
            {
                IsSuccess = false,
                Message = $"Error sending class notification: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<List<NotificationResponseDto>>> GetUserNotificationsAsync(int userId, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            var (notifications, totalCount) = await _notificationRepository.GetPagedByUserIdAsync(userId, pageNumber, pageSize);

            var dtos = notifications.Select(MapToDto).ToList();

            return new ResultModel<List<NotificationResponseDto>>
            {
                IsSuccess = true,
                Message = $"Retrieved {dtos.Count} notifications (Total: {totalCount})",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<NotificationResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving notifications: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<List<NotificationResponseDto>>> GetUnreadNotificationsAsync(int userId)
    {
        try
        {
            var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
            var dtos = notifications.Select(MapToDto).ToList();

            return new ResultModel<List<NotificationResponseDto>>
            {
                IsSuccess = true,
                Message = $"Retrieved {dtos.Count} unread notifications",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<NotificationResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving unread notifications: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<NotificationSummaryDto>> GetNotificationSummaryAsync(int userId)
    {
        try
        {
            var allNotifications = await _notificationRepository.GetByUserIdAsync(userId);
            var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);

            var summary = new NotificationSummaryDto
            {
                TotalNotifications = allNotifications.Count,
                UnreadCount = unreadCount,
                ReadCount = allNotifications.Count - unreadCount,
                LatestNotificationAt = allNotifications.FirstOrDefault()?.CreatedAt
            };

            return new ResultModel<NotificationSummaryDto>
            {
                IsSuccess = true,
                Message = "Notification summary retrieved successfully",
                Data = summary
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<NotificationSummaryDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving notification summary: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<bool>> MarkAsReadAsync(int notificationId, int userId)
    {
        try
        {
            // Kiểm tra notification thuộc về user
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "Notification not found",
                    Data = false
                };
            }

            if (notification.UserId != userId)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "Unauthorized to mark this notification",
                    Data = false
                };
            }

            var result = await _notificationRepository.MarkAsReadAsync(notificationId);

            return new ResultModel<bool>
            {
                IsSuccess = result,
                Message = result ? "Notification marked as read" : "Failed to mark notification as read",
                Data = result
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                Message = $"Error marking notification as read: {ex.Message}",
                Data = false
            };
        }
    }

    public async Task<ResultModel<int>> MarkAllAsReadAsync(int userId)
    {
        try
        {
            var count = await _notificationRepository.MarkAllAsReadAsync(userId);

            return new ResultModel<int>
            {
                IsSuccess = true,
                Message = $"Marked {count} notifications as read",
                Data = count
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<int>
            {
                IsSuccess = false,
                Message = $"Error marking all notifications as read: {ex.Message}",
                Data = 0
            };
        }
    }

    public async Task<ResultModel<bool>> DeleteNotificationAsync(int notificationId, int userId)
    {
        try
        {
            // Kiểm tra notification thuộc về user
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "Notification not found",
                    Data = false
                };
            }

            if (notification.UserId != userId)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "Unauthorized to delete this notification",
                    Data = false
                };
            }

            var result = await _notificationRepository.DeleteNotificationAsync(notificationId);

            return new ResultModel<bool>
            {
                IsSuccess = result,
                Message = result ? "Notification deleted successfully" : "Failed to delete notification",
                Data = result
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                Message = $"Error deleting notification: {ex.Message}",
                Data = false
            };
        }
    }

    public async Task<ResultModel<int>> DeleteReadNotificationsAsync(int userId)
    {
        try
        {
            var count = await _notificationRepository.DeleteReadNotificationsAsync(userId);

            return new ResultModel<int>
            {
                IsSuccess = true,
                Message = $"Deleted {count} read notifications",
                Data = count
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<int>
            {
                IsSuccess = false,
                Message = $"Error deleting read notifications: {ex.Message}",
                Data = 0
            };
        }
    }

    private static NotificationResponseDto MapToDto(BusinessObjects.Models.Notification notification)
    {
        return new NotificationResponseDto
        {
            NotificationId = notification.NotificationId,
            UserId = notification.UserId ?? 0,
            UserName = notification.User?.FullName,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Data = notification.Data, // Add Data field
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}

