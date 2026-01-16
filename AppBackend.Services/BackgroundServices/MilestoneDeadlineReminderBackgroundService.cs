using AppBackend.Services.Services.MilestoneDeadlineReminder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.BackgroundServices;

/// <summary>
/// Background service that runs daily to check milestone deadlines and send reminders to students
/// Runs at 9:00 AM UTC every day
/// </summary>
public class MilestoneDeadlineReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MilestoneDeadlineReminderBackgroundService> _logger;
    private Timer? _timer;

    public MilestoneDeadlineReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MilestoneDeadlineReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Milestone Deadline Reminder Background Service is starting");

        // Calculate time until next 9:00 AM UTC
        var now = DateTime.UtcNow;
        var next9AM = CalculateNext9AM(now);
        var timeUntilNext9AM = next9AM - now;

        _logger.LogInformation(
            "Next milestone deadline check scheduled for: {Next9AM} (in {Hours} hours)",
            next9AM, timeUntilNext9AM.TotalHours);

        // Schedule the first execution
        _timer = new Timer(
            DoWork,
            null,
            timeUntilNext9AM,
            TimeSpan.FromDays(1)); // Run every 24 hours

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            _logger.LogInformation("=== Starting Daily Milestone Deadline Reminder Job at {Time} ===", DateTime.UtcNow);

            using (var scope = _serviceProvider.CreateScope())
            {
                var reminderService = scope.ServiceProvider
                    .GetRequiredService<IMilestoneDeadlineReminderService>();

                var result = await reminderService.CheckAndSendMilestoneRemindersAsync();

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation(
                        "Daily check completed successfully. " +
                        "Milestones checked: {Milestones}, Students notified: {Students}, " +
                        "7-day reminders: {Reminders7}, 3-day reminders: {Reminders3}, " +
                        "1-day reminders: {Reminders1}, Overdue reminders: {RemindersOverdue}",
                        result.Data.TotalMilestonesChecked,
                        result.Data.TotalStudentsNotified,
                        result.Data.TotalReminders7Days,
                        result.Data.TotalReminders3Days,
                        result.Data.TotalReminders1Day,
                        result.Data.TotalOverdueReminders);

                    // Log details of reminders sent
                    foreach (var detail in result.Data.ReminderDetails)
                    {
                        _logger.LogDebug(
                            "Reminder: {Type} for milestone '{Title}' (ID: {Id}) - {Count} students notified",
                            detail.ReminderType,
                            detail.MilestoneTitle,
                            detail.MilestoneId,
                            detail.StudentsNotified);
                    }
                }
                else
                {
                    _logger.LogWarning("Daily check completed with warnings: {Message}", result.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during daily milestone deadline reminder check");
        }

        _logger.LogInformation("Next check will run in 24 hours at 9:00 AM UTC");
    }

    private DateTime CalculateNext9AM(DateTime fromDate)
    {
        // Set to 9:00 AM UTC
        var targetTime = new TimeSpan(9, 0, 0);
        var next9AM = fromDate.Date.Add(targetTime);

        // If current time is past 9:00 AM today, schedule for tomorrow
        if (fromDate.TimeOfDay >= targetTime)
        {
            next9AM = next9AM.AddDays(1);
        }

        return next9AM;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Milestone Deadline Reminder Background Service is stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
