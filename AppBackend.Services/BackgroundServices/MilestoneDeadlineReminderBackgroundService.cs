using AppBackend.Services.Services.MilestoneDeadlineReminder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.BackgroundServices;

/// <summary>
/// Background service that runs daily to check milestone deadlines and send reminders to students
/// Runs at 1:00 AM Vietnam Time (18:00 UTC / 6:00 PM UTC) every day
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

        // Calculate time until next 6:00 PM UTC (1:00 AM Vietnam Time)
        var now = DateTime.UtcNow;
        var next6PM = CalculateNext6PM(now);
        var timeUntilNext6PM = next6PM - now;

        _logger.LogInformation(
            "Next milestone deadline check scheduled for: {Next6PM} UTC (1:00 AM Vietnam Time) (in {Hours} hours)",
            next6PM, timeUntilNext6PM.TotalHours);

        // Schedule the first execution
        _timer = new Timer(
            DoWork,
            null,
            timeUntilNext6PM,
            TimeSpan.FromDays(1)); // Run every 24 hours

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            _logger.LogInformation("=== Starting Daily Milestone Deadline Reminder Job at {Time} UTC (1:00 AM Vietnam Time) ===", DateTime.UtcNow);

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

        _logger.LogInformation("Next check will run in 24 hours at 6:00 PM UTC (1:00 AM Vietnam Time)");
    }

    private DateTime CalculateNext6PM(DateTime fromDate)
    {
        // Set to 6:00 PM UTC (1:00 AM Vietnam Time UTC+7)
        var targetTime = new TimeSpan(18, 0, 0);
        var next6PM = fromDate.Date.Add(targetTime);

        // If current time is past 6:00 PM today, schedule for tomorrow
        if (fromDate.TimeOfDay >= targetTime)
        {
            next6PM = next6PM.AddDays(1);
        }

        return next6PM;
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
