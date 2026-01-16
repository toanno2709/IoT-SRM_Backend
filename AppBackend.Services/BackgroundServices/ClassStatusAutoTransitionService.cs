using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;

namespace AppBackend.Services.BackgroundServices;

/// <summary>
/// Background service to automatically transition class status from "Not Started" to "In Progress"
/// when start_time is reached and all students have groups.
/// Also sends notifications to instructors and admins if students don't have groups after start_time.
/// </summary>
public class ClassStatusAutoTransitionService : BackgroundService
{
    private readonly ILogger<ClassStatusAutoTransitionService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

    public ClassStatusAutoTransitionService(
        ILogger<ClassStatusAutoTransitionService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ClassStatusAutoTransitionService started at {Time}", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndTransitionClassesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking class status transitions");
            }

            // Wait for next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("ClassStatusAutoTransitionService stopped at {Time}", DateTime.UtcNow);
    }

    private async Task CheckAndTransitionClassesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IotShowroomContext>();

        var now = DateTime.UtcNow;

        // Find classes that:
        // 1. Status is "Not Started"
        // 2. StartTime is set and has passed
        // 3. StartTime is within the last 24 hours (to avoid processing very old classes)
        var classesToCheck = await context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.ClassEnrollments)
                .ThenInclude(ce => ce.Student)
            .Include(c => c.Groups)
                .ThenInclude(g => g.GroupMembers)
            .Where(c => c.Status == "Not Started" 
                && c.StartTime != null 
                && c.StartTime <= now
                && c.StartTime >= now.AddDays(-1)) // Only check classes within last 24 hours
            .ToListAsync();

        _logger.LogInformation("Found {Count} classes to check for status transition", classesToCheck.Count);

        foreach (var classEntity in classesToCheck)
        {
            await ProcessClassTransitionAsync(classEntity, context);
        }

        if (classesToCheck.Any())
        {
            await context.SaveChangesAsync();
        }
    }

    private async Task ProcessClassTransitionAsync(Class classEntity, IotShowroomContext context)
    {
        var totalStudents = classEntity.ClassEnrollments?.Count ?? 0;
        
        if (totalStudents == 0)
        {
            _logger.LogWarning("Class {ClassId} ({ClassName}) has no enrolled students, skipping transition", 
                classEntity.ClassId, classEntity.ClassName);
            return;
        }

        // Get students who are in groups
        var studentIdsInGroups = classEntity.Groups?
            .SelectMany(g => g.GroupMembers ?? new List<GroupMember>())
            .Select(gm => gm.UserId)
            .Distinct()
            .ToHashSet() ?? new HashSet<int>();

        var enrolledStudentIds = classEntity.ClassEnrollments?
            .Select(ce => ce.StudentId ?? 0)
            .Where(id => id > 0)
            .ToHashSet() ?? new HashSet<int>();

        var studentsWithoutGroup = enrolledStudentIds.Except(studentIdsInGroups).ToList();

        if (studentsWithoutGroup.Any())
        {
            // Students don't have groups - send notification
            await SendNotificationForStudentsWithoutGroupAsync(classEntity, studentsWithoutGroup, context);
            
            _logger.LogWarning(
                "Class {ClassId} ({ClassName}) reached start time but {Count} students don't have groups. Notifications sent.",
                classEntity.ClassId, classEntity.ClassName, studentsWithoutGroup.Count);
        }
        else
        {
            // All students have groups - transition to "In Progress"
            classEntity.Status = "In Progress";
            
            _logger.LogInformation(
                "Class {ClassId} ({ClassName}) automatically transitioned to 'In Progress'. All {Count} students have groups.",
                classEntity.ClassId, classEntity.ClassName, totalStudents);

            // Send success notification to instructor
            await SendSuccessTransitionNotificationAsync(classEntity, context);
        }
    }

    private async Task SendNotificationForStudentsWithoutGroupAsync(
        Class classEntity, 
        List<int> studentsWithoutGroup, 
        IotShowroomContext context)
    {
        var studentsWithoutGroupDetails = classEntity.ClassEnrollments?
            .Where(ce => studentsWithoutGroup.Contains(ce.StudentId ?? 0))
            .Select(ce => ce.Student?.FullName ?? ce.Student?.Email ?? $"Student ID: {ce.StudentId}")
            .ToList() ?? new List<string>();

        var message = $"Class '{classEntity.ClassName}' has reached its start time but {studentsWithoutGroup.Count} student(s) do not have a group yet: {string.Join(", ", studentsWithoutGroupDetails.Take(10))}{(studentsWithoutGroupDetails.Count > 10 ? "..." : "")}. Please assign these students to groups.";

        var notifications = new List<Notification>();

        // Send to instructor
        if (classEntity.InstructorId.HasValue)
        {
            notifications.Add(new Notification
            {
                UserId = classEntity.InstructorId.Value,
                Title = $"Action Required: Students Without Groups in {classEntity.ClassName}",
                Message = message,
                Type = "class_status_warning",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Send to all admins (RoleId = 1)
        var adminIds = await context.Users
            .Where(u => u.RoleId == 1)
            .Select(u => u.UserId)
            .ToListAsync();

        foreach (var adminId in adminIds)
        {
            notifications.Add(new Notification
            {
                UserId = adminId,
                Title = $"Action Required: Students Without Groups in {classEntity.ClassName}",
                Message = message,
                Type = "class_status_warning",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        context.Notifications.AddRange(notifications);
        
        _logger.LogInformation(
            "Sent {Count} notifications about students without groups in class {ClassId}",
            notifications.Count, classEntity.ClassId);
    }

    private async Task SendSuccessTransitionNotificationAsync(Class classEntity, IotShowroomContext context)
    {
        var message = $"Class '{classEntity.ClassName}' has automatically started. All students are in groups. Status changed to 'In Progress'.";

        var notifications = new List<Notification>();

        // Send to instructor
        if (classEntity.InstructorId.HasValue)
        {
            notifications.Add(new Notification
            {
                UserId = classEntity.InstructorId.Value,
                Title = $"Class {classEntity.ClassName} Started",
                Message = message,
                Type = "class_status_change",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Send to all admins (RoleId = 1)
        var adminIds = await context.Users
            .Where(u => u.RoleId == 1)
            .Select(u => u.UserId)
            .ToListAsync();

        foreach (var adminId in adminIds)
        {
            notifications.Add(new Notification
            {
                UserId = adminId,
                Title = $"Class {classEntity.ClassName} Started",
                Message = message,
                Type = "class_status_change",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        context.Notifications.AddRange(notifications);
        
        _logger.LogInformation(
            "Sent {Count} notifications about successful class start for class {ClassId}",
            notifications.Count, classEntity.ClassId);
    }
}
