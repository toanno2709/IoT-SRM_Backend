using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.MilestoneDeadlineReminder;

public class MilestoneDeadlineReminderService : IMilestoneDeadlineReminderService
{
    private readonly IotShowroomContext _context;
    private readonly ILogger<MilestoneDeadlineReminderService> _logger;

    public MilestoneDeadlineReminderService(
        IotShowroomContext context,
        ILogger<MilestoneDeadlineReminderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<MilestoneDeadlineReminderResultDto>> CheckAndSendMilestoneRemindersAsync()
    {
        try
        {
            _logger.LogInformation("=== Starting Daily Milestone Deadline Check ===");

            var result = new MilestoneDeadlineReminderResultDto
            {
                CheckedAt = DateTime.UtcNow,
                ReminderDetails = new List<MilestoneReminderDetailDto>()
            };

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var studentsNotified = new HashSet<int>();

            // Get all active milestones with their projects and groups
            var milestones = await _context.ProjectMilestones
                .Include(m => m.Project)
                    .ThenInclude(p => p!.Group)
                        .ThenInclude(g => g!.GroupMembers)
                            .ThenInclude(gm => gm.User)
                .Include(m => m.Project)
                    .ThenInclude(p => p!.Group)
                        .ThenInclude(g => g!.Class)
                .Include(m => m.MilestoneSubmissions)
                .Where(m => m.DueDate != null && m.DueDate >= today.AddDays(-30)) // Check milestones from 30 days ago to future
                .OrderBy(m => m.DueDate)
                .ToListAsync();

            result.TotalMilestonesChecked = milestones.Count;

            foreach (var milestone in milestones)
            {
                if (milestone.DueDate == null || milestone.Project?.Group == null)
                    continue;

                var daysUntilDue = milestone.DueDate.Value.DayNumber - today.DayNumber;
                string reminderType = "";
                string notificationTitle = "";
                string notificationMessage = "";

                // Determine reminder type based on days until due
                if (daysUntilDue == 7)
                {
                    reminderType = "7days";
                    notificationTitle = $"?? Reminder: Milestone due in 7 days";
                    notificationMessage = $"Milestone '{milestone.Title}' for project '{milestone.Project.Title}' is due in 7 days ({milestone.DueDate:MMM dd, yyyy}). Please start working on it!";
                    result.TotalReminders7Days++;
                }
                else if (daysUntilDue == 3)
                {
                    reminderType = "3days";
                    notificationTitle = $"?? Reminder: Milestone due in 3 days";
                    notificationMessage = $"Milestone '{milestone.Title}' for project '{milestone.Project.Title}' is due in 3 days ({milestone.DueDate:MMM dd, yyyy}). Don't forget to submit!";
                    result.TotalReminders3Days++;
                }
                else if (daysUntilDue == 1)
                {
                    reminderType = "1day";
                    notificationTitle = $"?? Urgent: Milestone due tomorrow!";
                    notificationMessage = $"Milestone '{milestone.Title}' for project '{milestone.Project.Title}' is due tomorrow ({milestone.DueDate:MMM dd, yyyy}). Please submit as soon as possible!";
                    result.TotalReminders1Day++;
                }
                else if (daysUntilDue < 0)
                {
                    // Check if already submitted
                    var hasSubmission = milestone.MilestoneSubmissions.Any();
                    if (!hasSubmission)
                    {
                        reminderType = "overdue";
                        var daysOverdue = Math.Abs(daysUntilDue);
                        notificationTitle = $"? Overdue: Milestone not submitted";
                        notificationMessage = $"Milestone '{milestone.Title}' for project '{milestone.Project.Title}' was due {daysOverdue} day(s) ago ({milestone.DueDate:MMM dd, yyyy}). Please submit immediately!";
                        result.TotalOverdueReminders++;
                    }
                    else
                    {
                        continue; // Skip if already submitted
                    }
                }
                else
                {
                    continue; // Skip other days
                }

                // Get all group members
                var groupMembers = milestone.Project.Group.GroupMembers?.ToList() ?? new List<BusinessObjects.Models.GroupMember>();
                
                if (!groupMembers.Any())
                    continue;

                var studentNames = new List<string>();

                // Send notification to all group members
                foreach (var member in groupMembers)
                {
                    if (member.User == null)
                        continue;

                    var notification = new BusinessObjects.Models.Notification
                    {
                        UserId = member.UserId,
                        Title = notificationTitle,
                        Message = notificationMessage,
                        Type = "milestone_deadline_reminder",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);

                    studentsNotified.Add(member.UserId);
                    studentNames.Add(member.User.FullName ?? member.User.Email ?? $"User {member.UserId}");
                }

                // Add to result details
                result.ReminderDetails.Add(new MilestoneReminderDetailDto
                {
                    MilestoneId = milestone.MilestoneId,
                    MilestoneTitle = milestone.Title,
                    ProjectId = milestone.Project.ProjectId,
                    ProjectTitle = milestone.Project.Title,
                    GroupId = milestone.Project.Group.GroupId,
                    GroupName = milestone.Project.Group.GroupName,
                    ClassName = milestone.Project.Group.Class?.ClassName,
                    DueDate = milestone.DueDate,
                    DaysUntilDue = daysUntilDue,
                    ReminderType = reminderType,
                    StudentsNotified = groupMembers.Count,
                    StudentNames = studentNames
                });

                _logger.LogInformation(
                    "Sent {Type} reminder for milestone {MilestoneId} '{Title}' to {Count} students",
                    reminderType, milestone.MilestoneId, milestone.Title, groupMembers.Count);
            }

            await _context.SaveChangesAsync();

            result.TotalStudentsNotified = studentsNotified.Count;

            _logger.LogInformation(
                "=== Milestone Deadline Check Complete === Milestones: {Milestones}, Students Notified: {Students}, Total Reminders: {Reminders}",
                result.TotalMilestonesChecked, result.TotalStudentsNotified, 
                result.TotalReminders7Days + result.TotalReminders3Days + result.TotalReminders1Day + result.TotalOverdueReminders);

            return new ResultModel<MilestoneDeadlineReminderResultDto>
            {
                IsSuccess = true,
                Message = $"Checked {result.TotalMilestonesChecked} milestones. Sent reminders to {result.TotalStudentsNotified} students.",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking milestone deadline reminders");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error checking milestone reminders: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<List<StudentUpcomingMilestoneDto>>> GetStudentUpcomingDeadlinesAsync(int studentId)
    {
        try
        {
            _logger.LogInformation("Getting upcoming deadlines for student {StudentId}", studentId);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Get groups that student is a member of
            var studentGroups = await _context.GroupMembers
                .Where(gm => gm.UserId == studentId)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            if (!studentGroups.Any())
            {
                return new ResultModel<List<StudentUpcomingMilestoneDto>>
                {
                    IsSuccess = true,
                    Message = "Student is not in any group",
                    Data = new List<StudentUpcomingMilestoneDto>(),
                    StatusCode = StatusCodes.Status200OK
                };
            }

            // Get all milestones for student's groups that are due in the future or recently past
            var milestones = await _context.ProjectMilestones
                .Include(m => m.Project)
                    .ThenInclude(p => p!.Group)
                        .ThenInclude(g => g!.Class)
                .Include(m => m.MilestoneSubmissions)
                .Where(m => m.Project != null && 
                           studentGroups.Contains(m.Project.GroupId ?? 0) &&
                           m.DueDate != null &&
                           m.DueDate >= today.AddDays(-7)) // Show milestones from 7 days ago to future
                .OrderBy(m => m.DueDate)
                .ToListAsync();

            var result = milestones.Select(m =>
            {
                var daysUntilDue = m.DueDate!.Value.DayNumber - today.DayNumber;
                var hasSubmission = m.MilestoneSubmissions.Any();
                var lastSubmission = m.MilestoneSubmissions.OrderByDescending(s => s.LastSubmittedAt).FirstOrDefault();

                string urgency = "low";
                if (daysUntilDue < 0)
                    urgency = "critical"; // Overdue
                else if (daysUntilDue <= 1)
                    urgency = "critical";
                else if (daysUntilDue <= 3)
                    urgency = "high";
                else if (daysUntilDue <= 7)
                    urgency = "medium";

                return new StudentUpcomingMilestoneDto
                {
                    MilestoneId = m.MilestoneId,
                    MilestoneTitle = m.Title,
                    ProjectId = m.Project!.ProjectId,
                    ProjectTitle = m.Project.Title,
                    GroupId = m.Project.GroupId ?? 0,
                    GroupName = m.Project.Group?.GroupName,
                    ClassId = m.Project.Group?.ClassId ?? 0,
                    ClassName = m.Project.Group?.Class?.ClassName,
                    DueDate = m.DueDate,
                    DaysUntilDue = daysUntilDue,
                    WeightPercentage = m.Weight,
                    HasSubmitted = hasSubmission,
                    LastSubmittedAt = lastSubmission?.LastSubmittedAt,
                    UrgencyLevel = urgency
                };
            }).ToList();

            return new ResultModel<List<StudentUpcomingMilestoneDto>>
            {
                IsSuccess = true,
                Message = $"Found {result.Count} upcoming milestone(s)",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student upcoming deadlines");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting upcoming deadlines: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<List<StudentOverdueMilestoneDto>>> GetStudentOverdueMilestonesAsync(int studentId)
    {
        try
        {
            _logger.LogInformation("Getting overdue milestones for student {StudentId}", studentId);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Get groups that student is a member of
            var studentGroups = await _context.GroupMembers
                .Where(gm => gm.UserId == studentId)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            if (!studentGroups.Any())
            {
                return new ResultModel<List<StudentOverdueMilestoneDto>>
                {
                    IsSuccess = true,
                    Message = "Student is not in any group",
                    Data = new List<StudentOverdueMilestoneDto>(),
                    StatusCode = StatusCodes.Status200OK
                };
            }

            // Get overdue milestones that haven't been submitted
            var milestones = await _context.ProjectMilestones
                .Include(m => m.Project)
                    .ThenInclude(p => p!.Group)
                        .ThenInclude(g => g!.Class)
                .Include(m => m.MilestoneSubmissions)
                .Where(m => m.Project != null && 
                           studentGroups.Contains(m.Project.GroupId ?? 0) &&
                           m.DueDate != null &&
                           m.DueDate < today &&
                           !m.MilestoneSubmissions.Any()) // Only milestones without submissions
                .OrderBy(m => m.DueDate)
                .ToListAsync();

            var result = milestones.Select(m =>
            {
                var daysOverdue = today.DayNumber - m.DueDate!.Value.DayNumber;

                return new StudentOverdueMilestoneDto
                {
                    MilestoneId = m.MilestoneId,
                    MilestoneTitle = m.Title,
                    ProjectId = m.Project!.ProjectId,
                    ProjectTitle = m.Project.Title,
                    GroupId = m.Project.GroupId ?? 0,
                    GroupName = m.Project.Group?.GroupName,
                    ClassId = m.Project.Group?.ClassId ?? 0,
                    ClassName = m.Project.Group?.Class?.ClassName,
                    DueDate = m.DueDate,
                    DaysOverdue = daysOverdue,
                    WeightPercentage = m.Weight,
                    HasSubmitted = false,
                    LastSubmittedAt = null
                };
            }).ToList();

            return new ResultModel<List<StudentOverdueMilestoneDto>>
            {
                IsSuccess = true,
                Message = $"Found {result.Count} overdue milestone(s) without submission",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student overdue milestones");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting overdue milestones: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ClassDeadlineCheckResultDto>> CheckClassMilestoneDeadlinesAsync(int classId)
    {
        try
        {
            _logger.LogInformation("Checking milestone deadlines for class {ClassId}", classId);

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (classEntity == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Class not found",
                    StatusCodes.Status404NotFound
                );
            }

            var result = new ClassDeadlineCheckResultDto
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                CheckedAt = DateTime.UtcNow,
                ReminderDetails = new List<MilestoneReminderDetailDto>()
            };

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var studentsNotified = new HashSet<int>();

            // Get all milestones for this class
            var milestones = await _context.ProjectMilestones
                .Include(m => m.Project)
                    .ThenInclude(p => p!.Group)
                        .ThenInclude(g => g!.GroupMembers)
                            .ThenInclude(gm => gm.User)
                .Include(m => m.MilestoneSubmissions)
                .Where(m => m.Project != null &&
                           m.Project.Group != null &&
                           m.Project.Group.ClassId == classId &&
                           m.DueDate != null)
                .OrderBy(m => m.DueDate)
                .ToListAsync();

            result.TotalMilestonesChecked = milestones.Count;

            foreach (var milestone in milestones)
            {
                if (milestone.DueDate == null || milestone.Project?.Group == null)
                    continue;

                var daysUntilDue = milestone.DueDate.Value.DayNumber - today.DayNumber;

                // Only send reminders for specific days
                if (daysUntilDue != 7 && daysUntilDue != 3 && daysUntilDue != 1 && daysUntilDue >= 0)
                    continue;

                // Check for overdue without submission
                if (daysUntilDue < 0)
                {
                    var hasSubmission = milestone.MilestoneSubmissions.Any();
                    if (hasSubmission)
                        continue;
                }

                var groupMembers = milestone.Project.Group.GroupMembers?.ToList() ?? new List<BusinessObjects.Models.GroupMember>();
                
                foreach (var member in groupMembers)
                {
                    studentsNotified.Add(member.UserId);
                }

                result.ReminderDetails.Add(new MilestoneReminderDetailDto
                {
                    MilestoneId = milestone.MilestoneId,
                    MilestoneTitle = milestone.Title,
                    ProjectId = milestone.Project.ProjectId,
                    ProjectTitle = milestone.Project.Title,
                    GroupId = milestone.Project.Group.GroupId,
                    GroupName = milestone.Project.Group.GroupName,
                    ClassName = classEntity.ClassName,
                    DueDate = milestone.DueDate,
                    DaysUntilDue = daysUntilDue,
                    ReminderType = daysUntilDue < 0 ? "overdue" : $"{daysUntilDue}days",
                    StudentsNotified = groupMembers.Count,
                    StudentNames = groupMembers.Select(m => m.User?.FullName ?? "Unknown").ToList()
                });
            }

            result.TotalStudentsNotified = studentsNotified.Count;
            result.TotalReminders = result.ReminderDetails.Count;

            return new ResultModel<ClassDeadlineCheckResultDto>
            {
                IsSuccess = true,
                Message = $"Checked {result.TotalMilestonesChecked} milestones for class {classEntity.ClassName}",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking class milestone deadlines");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error checking deadlines: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }
}
