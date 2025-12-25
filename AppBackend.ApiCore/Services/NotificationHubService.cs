using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using AppBackend.ApiCore.Hubs;

namespace AppBackend.ApiCore.Services;

/// <summary>
/// Implementation of SignalR notification service
/// Handles real-time notification delivery to users via email-based groups
/// </summary>
public class NotificationHubService : AppBackend.Services.Services.Notification.INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationHubService> _logger;

    public NotificationHubService(
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationHubService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Send notification to a specific user by email using SignalR groups
    /// Group name format: "notifications_email_{email}"
    /// </summary>
    public async Task SendNotificationToUserAsync(string email, object notification)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Attempted to send notification to null or empty email");
                return;
            }

            var groupName = $"notifications_email_{email}";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", notification);
            
            _logger.LogInformation($"Notification sent to user: {email} via group: {groupName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to user: {email}");
        }
    }

    /// <summary>
    /// Send notification to multiple users by their emails
    /// </summary>
    public async Task SendNotificationToUsersAsync(List<string> emails, object notification)
    {
        try
        {
            if (emails == null || !emails.Any())
            {
                _logger.LogWarning("Attempted to send notification to empty email list");
                return;
            }

            var tasks = emails.Select(email => SendNotificationToUserAsync(email, notification));
            await Task.WhenAll(tasks);
            
            _logger.LogInformation($"Notification sent to {emails.Count} users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notifications");
        }
    }

    /// <summary>
    /// Broadcast notification to all connected clients
    /// </summary>
    public async Task SendNotificationToAllAsync(object notification)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
            _logger.LogInformation("Notification broadcast to all connected clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification to all clients");
        }
    }

    /// <summary>
    /// Send group invitation notification via SignalR
    /// Uses specific event name for group invitations
    /// </summary>
    public async Task SendGroupInvitationNotificationAsync(string email, object notification)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Attempted to send group invitation to null or empty email");
                return;
            }

            var groupName = $"notifications_email_{email}";
            
            // Send with specific event name for group invitations
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveGroupInvitation", notification);
            
            _logger.LogInformation($"Group invitation notification sent to user: {email} via group: {groupName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending group invitation notification to user: {email}");
        }
    }
}
