using AppBackend.BusinessObjects.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.BackgroundServices;

/// <summary>
/// Background service to automatically update IsCurrent flag for StudentCourseHistory
/// based on semester start/end dates
/// </summary>
public class StudentCourseHistoryUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StudentCourseHistoryUpdateService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Run every 1 hour

    public StudentCourseHistoryUpdateService(
        IServiceProvider serviceProvider,
        ILogger<StudentCourseHistoryUpdateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StudentCourseHistoryUpdateService is starting.");

        // Wait 5 seconds before first run
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateStudentCourseHistoryCurrentFlagsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating StudentCourseHistory IsCurrent flags.");
            }

            // Wait for next interval
            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("StudentCourseHistoryUpdateService is stopping.");
    }

    private async Task UpdateStudentCourseHistoryCurrentFlagsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IotShowroomContext>();

        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            _logger.LogInformation("Starting StudentCourseHistory IsCurrent update check for date: {Date}", today);

            // Get all StudentCourseHistory records with semester info
            var allHistories = await context.StudentCourseHistories
                .Include(sch => sch.Semester)
                .Where(sch => sch.SemesterId != null)
                .ToListAsync(cancellationToken);

            var updatedCount = 0;
            var unchangedCount = 0;

            foreach (var history in allHistories)
            {
                if (history.Semester == null)
                    continue;

                var semester = history.Semester;
                
                // Determine if this history should be current
                // Current if today is between semester start and end dates
                bool shouldBeCurrent = false;

                if (semester.StartDate.HasValue && semester.EndDate.HasValue)
                {
                    shouldBeCurrent = today >= semester.StartDate.Value && today <= semester.EndDate.Value;
                }

                // Update if different from current state
                if (history.IsCurrent != shouldBeCurrent)
                {
                    history.IsCurrent = shouldBeCurrent;
                    history.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;

                    _logger.LogDebug(
                        "Updated StudentCourseHistory ID: {HistoryId}, Student: {StudentId}, Semester: {SemesterId}, IsCurrent: {IsCurrent}",
                        history.HistoryId, history.StudentId, history.SemesterId, shouldBeCurrent);
                }
                else
                {
                    unchangedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "StudentCourseHistory IsCurrent update completed. Updated: {Updated}, Unchanged: {Unchanged}",
                    updatedCount, unchangedCount);
            }
            else
            {
                _logger.LogInformation(
                    "No StudentCourseHistory records needed updating. Total checked: {Total}",
                    allHistories.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateStudentCourseHistoryCurrentFlagsAsync");
            throw;
        }
    }
}
