using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AppBackend.ApiCore.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications
/// Clients can connect using their email as the connection identifier
/// </summary>
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// Automatically adds user to their email-based group
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userEmail))
        {
            // Add user to their email-based group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"notifications_email_{userEmail}");
            _logger.LogInformation($"User {userEmail} (ID: {userId}) connected to NotificationHub with ConnectionId: {Context.ConnectionId}");
        }
        else
        {
            _logger.LogWarning($"User connected without email claim. ConnectionId: {Context.ConnectionId}");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userEmail))
        {
            _logger.LogInformation($"User {userEmail} (ID: {userId}) disconnected from NotificationHub. ConnectionId: {Context.ConnectionId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client can manually join an email-based group (optional, as it's done automatically in OnConnectedAsync)
    /// </summary>
    /// <param name="email">User's email address</param>
    public async Task JoinEmailGroup(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Attempted to join email group with null or empty email");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"notifications_email_{email}");
        _logger.LogInformation($"User manually joined email group: notifications_email_{email}");
    }

    /// <summary>
    /// Client can manually leave an email-based group
    /// </summary>
    /// <param name="email">User's email address</param>
    public async Task LeaveEmailGroup(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Attempted to leave email group with null or empty email");
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"notifications_email_{email}");
        _logger.LogInformation($"User left email group: notifications_email_{email}");
    }

    /// <summary>
    /// Client can mark a notification as read (optional helper method)
    /// </summary>
    /// <param name="notificationId">Notification ID to mark as read</param>
    public async Task MarkNotificationAsRead(int notificationId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation($"User {userId} marked notification {notificationId} as read via SignalR");
        
        // The actual marking should be done via the API endpoint
        // This is just a client-initiated trigger
        await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
    }
}
