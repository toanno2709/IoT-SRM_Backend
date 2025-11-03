using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.NotificationRepo;

public interface INotificationRepository : IGenericRepository<Notification>
{
    /// <summary>
    /// Lấy tất cả notifications của một user
    /// </summary>
    Task<List<Notification>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Lấy unread notifications của user
    /// </summary>
    Task<List<Notification>> GetUnreadByUserIdAsync(int userId);

    /// <summary>
    /// Đánh dấu notification đã đọc
    /// </summary>
    Task<bool> MarkAsReadAsync(int notificationId);

    /// <summary>
    /// Đánh dấu tất cả notifications của user đã đọc
    /// </summary>
    Task<int> MarkAllAsReadAsync(int userId);

    /// <summary>
    /// Đếm số notifications chưa đọc của user
    /// </summary>
    Task<int> GetUnreadCountAsync(int userId);

    /// <summary>
    /// Xóa notification
    /// </summary>
    Task<bool> DeleteNotificationAsync(int notificationId);

    /// <summary>
    /// Xóa tất cả notifications đã đọc của user
    /// </summary>
    Task<int> DeleteReadNotificationsAsync(int userId);

    /// <summary>
    /// Lấy notifications với phân trang
    /// </summary>
    Task<(List<Notification> notifications, int totalCount)> GetPagedByUserIdAsync(int userId, int pageNumber, int pageSize);
}

