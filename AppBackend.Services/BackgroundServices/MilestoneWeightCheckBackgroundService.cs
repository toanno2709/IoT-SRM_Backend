using AppBackend.Services.Services.MilestoneWarning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.BackgroundServices;

/// <summary>
/// Background service that runs weekly on Mondays to check for projects with incomplete milestone weights
/// </summary>
public class MilestoneWeightCheckBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MilestoneWeightCheckBackgroundService> _logger;
    private Timer? _timer;

    public MilestoneWeightCheckBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MilestoneWeightCheckBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Milestone Weight Check Background Service is starting");

        // Calculate time until next Monday at 9:00 AM
        var now = DateTime.UtcNow;
        var nextMonday = CalculateNextMonday(now);
        var timeUntilNextMonday = nextMonday - now;

        _logger.LogInformation(
            "Next milestone weight check scheduled for: {NextMonday} (in {Hours} hours)",
            nextMonday, timeUntilNextMonday.TotalHours);

        // Schedule the first execution
        _timer = new Timer(
            DoWork,
            null,
            timeUntilNextMonday,
            TimeSpan.FromDays(7)); // Run every 7 days

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            _logger.LogInformation("=== Starting Weekly Milestone Weight Check Job ===");

            using (var scope = _serviceProvider.CreateScope())
            {
                var milestoneWarningService = scope.ServiceProvider
                    .GetRequiredService<IMilestoneWarningService>();

                var result = await milestoneWarningService.CheckAndSendMilestoneWarningsAsync();

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation(
                        "Weekly check completed successfully. Classes checked: {Classes}, Instructors notified: {Instructors}, Projects with issues: {Projects}",
                        result.Data.TotalClassesChecked,
                        result.Data.TotalInstructorsNotified,
                        result.Data.TotalProjectsWithIncompleteWeights);
                }
                else
                {
                    _logger.LogWarning("Weekly check completed with warnings: {Message}", result.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during weekly milestone weight check");
        }

        _logger.LogInformation("Next check will run in 7 days (next Monday at 9:00 AM UTC)");
    }

    private DateTime CalculateNextMonday(DateTime fromDate)
    {
        // Set to 9:00 AM UTC
        var targetTime = new TimeSpan(9, 0, 0);
        
        // Calculate days until next Monday
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)fromDate.DayOfWeek + 7) % 7;
        
        // If today is Monday but it's past 9 AM, schedule for next week
        if (daysUntilMonday == 0 && fromDate.TimeOfDay > targetTime)
        {
            daysUntilMonday = 7;
        }

        var nextMonday = fromDate.Date.AddDays(daysUntilMonday).Add(targetTime);
        return nextMonday;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Milestone Weight Check Background Service is stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
