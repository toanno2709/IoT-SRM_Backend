using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Notification;

public interface INotificationService
{
    /// <summary>
    /// Gửi notification đến một user
    /// </summary>
    Task<ResultModel<NotificationResponseDto>> SendNotificationAsync(NotificationCreateRequestDto request);

    /// <summary>
    /// Gửi notification đến nhiều users
    /// </summary>
    Task<ResultModel<BulkNotificationResponseDto>> SendBulkNotificationAsync(BulkNotificationRequestDto request);

    /// <summary>
    /// Gửi notification đến tất cả students trong class
    /// </summary>
    Task<ResultModel<BulkNotificationResponseDto>> SendClassNotificationAsync(ClassNotificationRequestDto request);

    /// <summary>
    /// Lấy tất cả notifications của user
    /// </summary>
    Task<ResultModel<List<NotificationResponseDto>>> GetUserNotificationsAsync(int userId, int pageNumber = 1, int pageSize = 20);

    /// <summary>
    /// Lấy unread notifications của user
    /// </summary>
    Task<ResultModel<List<NotificationResponseDto>>> GetUnreadNotificationsAsync(int userId);

    /// <summary>
    /// Lấy summary thống kê notifications
    /// </summary>
    Task<ResultModel<NotificationSummaryDto>> GetNotificationSummaryAsync(int userId);

    /// <summary>
    /// Đánh dấu notification đã đọc
    /// </summary>
    Task<ResultModel<bool>> MarkAsReadAsync(int notificationId, int userId);

    /// <summary>
    /// Đánh dấu tất cả notifications đã đọc
    /// </summary>
    Task<ResultModel<int>> MarkAllAsReadAsync(int userId);

    /// <summary>
    /// Xóa notification
    /// </summary>
    Task<ResultModel<bool>> DeleteNotificationAsync(int notificationId, int userId);

    /// <summary>
    /// Xóa tất cả notifications đã đọc
    /// </summary>
    Task<ResultModel<int>> DeleteReadNotificationsAsync(int userId);
}

