namespace AppBackend.Services.Services.Notification;

/// <summary>
/// Service interface for sending SignalR notifications
/// Provides methods to send real-time notifications to specific users via email
/// </summary>
public interface INotificationHubService
{
    /// <summary>
    /// Send notification to a specific user by email
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="notification">Notification data to send</param>
    Task SendNotificationToUserAsync(string email, object notification);

    /// <summary>
    /// Send notification to multiple users by email
    /// </summary>
    /// <param name="emails">List of user email addresses</param>
    /// <param name="notification">Notification data to send</param>
    Task SendNotificationToUsersAsync(List<string> emails, object notification);

    /// <summary>
    /// Send notification to all connected clients (broadcast)
    /// </summary>
    /// <param name="notification">Notification data to send</param>
    Task SendNotificationToAllAsync(object notification);

    /// <summary>
    /// Send notification about group invitation to specific user
    /// </summary>
    /// <param name="email">Invited user's email</param>
    /// <param name="notification">Invitation notification data</param>
    Task SendGroupInvitationNotificationAsync(string email, object notification);
}
