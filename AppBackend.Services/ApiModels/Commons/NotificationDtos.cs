using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO để tạo notification mới
/// </summary>
public class NotificationCreateRequestDto
{
    [Required(ErrorMessage = "User ID is required")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Message is required")]
    public string Message { get; set; } = null!;

    [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
    public string? Type { get; set; }  // "info", "success", "warning", "error", "announcement", "grade", "proposal_review"

    /// <summary>
    /// JSON string containing notification-specific data
    /// </summary>
    public string? Data { get; set; }
}

/// <summary>
/// DTO để gửi notification hàng loạt
/// </summary>
public class BulkNotificationRequestDto
{
    [Required(ErrorMessage = "User IDs are required")]
    [MinLength(1, ErrorMessage = "At least one user ID is required")]
    public List<int> UserIds { get; set; } = new();

    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Message is required")]
    public string Message { get; set; } = null!;

    [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
    public string? Type { get; set; }

    /// <summary>
    /// JSON string containing notification-specific data
    /// </summary>
    public string? Data { get; set; }
}

/// <summary>
/// DTO để gửi notification cho cả class
/// </summary>
public class ClassNotificationRequestDto
{
    [Required(ErrorMessage = "Class ID is required")]
    public int ClassId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Message is required")]
    public string Message { get; set; } = null!;

    [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
    public string? Type { get; set; }

    public bool IncludeInstructor { get; set; } = false;  // Gửi cho cả instructor không

    /// <summary>
    /// JSON string containing notification-specific data
    /// </summary>
    public string? Data { get; set; }
}

/// <summary>
/// Response notification
/// </summary>
public class NotificationResponseDto
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? Type { get; set; }
    public bool? IsRead { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? Data { get; set; }
}

/// <summary>
/// Summary thống kê notifications
/// </summary>
public class NotificationSummaryDto
{
    public int TotalNotifications { get; set; }
    public int UnreadCount { get; set; }
    public int ReadCount { get; set; }
    public DateTime? LatestNotificationAt { get; set; }
}

/// <summary>
/// Response sau khi gửi bulk notification
/// </summary>
public class BulkNotificationResponseDto
{
    public int TotalSent { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<int> FailedUserIds { get; set; } = new();
    public string Message { get; set; } = null!;
}

