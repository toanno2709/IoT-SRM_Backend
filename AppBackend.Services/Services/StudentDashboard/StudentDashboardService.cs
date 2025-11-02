using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.StudentDashboard;

/// <summary>
/// Student Dashboard Service Implementation
/// </summary>
public class StudentDashboardService : IStudentDashboardService
{
    private readonly IotShowroomContext _context;
    private readonly ILogger<StudentDashboardService> _logger;

    public StudentDashboardService(
        IotShowroomContext context,
        ILogger<StudentDashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ResultModel<StudentDashboardResponseDto>> GetDashboardAsync(int userId)
    {
        try
        {
            // Verify user exists and is a student
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return new ResultModel<StudentDashboardResponseDto>
                {
                    IsSuccess = false,
                    Message = "User not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (user.Role?.RoleName != "Student")
            {
                return new ResultModel<StudentDashboardResponseDto>
                {
                    IsSuccess = false,
                    Message = "User is not a student",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Get student's groups
            var groupMembers = await _context.GroupMembers
                .Include(gm => gm.Group)
                .Where(gm => gm.UserId == userId)
                .ToListAsync();

            var groupIds = groupMembers.Select(gm => gm.GroupId).ToList();

            // Get student's projects
            var projects = await _context.Projects
                .Where(p => groupIds.Contains(p.GroupId ?? 0))
                .ToListAsync();

            var projectIds = projects.Select(p => p.ProjectId).ToList();

            // Calculate statistics
            var statistics = await CalculateStatisticsAsync(userId, projectIds);

            // Get upcoming deadlines
            var upcomingDeadlines = await GetUpcomingDeadlinesAsync(projectIds);

            // Get recent grades
            var recentGrades = await GetRecentGradesAsync(projectIds);

            // Get recent notifications
            var recentNotifications = await GetRecentNotificationsAsync(userId);

            var dashboard = new StudentDashboardResponseDto
            {
                StudentId = userId,
                StudentName = user.FullName ?? "Unknown",
                Statistics = statistics,
                UpcomingDeadlines = upcomingDeadlines,
                RecentGrades = recentGrades,
                RecentNotifications = recentNotifications
            };

            return new ResultModel<StudentDashboardResponseDto>
            {
                IsSuccess = true,
                Message = "Dashboard data retrieved successfully",
                Data = dashboard,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard for user {UserId}", userId);
            return new ResultModel<StudentDashboardResponseDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving dashboard: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ResultModel<List<StudentClassDto>>> GetMyClassesAsync(int userId)
    {
        try
        {
            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
            {
                return new ResultModel<List<StudentClassDto>>
                {
                    IsSuccess = false,
                    Message = "User not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Get enrollments with related data
            var enrollments = await _context.ClassEnrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Semester)
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Instructor)
                .Where(e => e.StudentId == userId)
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();

            var classes = new List<StudentClassDto>();

            foreach (var enrollment in enrollments)
            {
                if (enrollment.Class == null || !enrollment.ClassId.HasValue) continue;

                // Find student's group in this class
                var groupMember = await _context.GroupMembers
                    .Include(gm => gm.Group)
                    .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.Group.ClassId == enrollment.ClassId.Value);

                StudentClassGroupDto? myGroup = null;
                if (groupMember?.Group != null)
                {
                    var memberCount = await _context.GroupMembers
                        .CountAsync(gm => gm.GroupId == groupMember.GroupId);

                    myGroup = new StudentClassGroupDto
                    {
                        GroupId = groupMember.GroupId,
                        GroupName = groupMember.Group.GroupName ?? "Unknown",
                        IsLeader = groupMember.Group.LeaderId.HasValue && 
                                   groupMember.Group.LeaderId.Value == userId,
                        MemberCount = memberCount
                    };
                }

                classes.Add(new StudentClassDto
                {
                    ClassId = enrollment.ClassId.Value,
                    ClassName = enrollment.Class.ClassName ?? "Unknown",
                    SemesterName = enrollment.Class.Semester?.Name,
                    InstructorName = enrollment.Class.Instructor?.FullName,
                    EnrolledAt = enrollment.EnrolledAt ?? DateTime.UtcNow,
                    MyGroup = myGroup
                });
            }

            return new ResultModel<List<StudentClassDto>>
            {
                IsSuccess = true,
                Message = "Classes retrieved successfully",
                Data = classes,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting classes for user {UserId}", userId);
            return new ResultModel<List<StudentClassDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving classes: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ResultModel<StudentGroupDetailDto>> GetMyGroupAsync(int userId, int classId)
    {
        try
        {
            // Find group member record
            var groupMember = await _context.GroupMembers
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.Class)
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.GroupMembers)
                    .ThenInclude(gm => gm.User)
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.Projects)
                .FirstOrDefaultAsync(gm => gm.UserId == userId && gm.Group.ClassId == classId);

            if (groupMember?.Group == null)
            {
                return new ResultModel<StudentGroupDetailDto>
                {
                    IsSuccess = false,
                    Message = "No group found in this class",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var group = groupMember.Group;
            var isLeader = group.LeaderId.HasValue && group.LeaderId.Value == userId;

            // Map members
            var members = group.GroupMembers
                .Where(gm => gm.User != null)
                .Select(gm => new GroupMemberSimpleDto
                {
                    UserId = gm.UserId,
                    FullName = gm.User!.FullName ?? "Unknown",
                    Email = gm.User.Email ?? "Unknown",
                    Role = group.LeaderId.HasValue && gm.UserId == group.LeaderId.Value ? "Leader" : "Member",
                    JoinedAt = gm.JoinedAt ?? DateTime.UtcNow
                })
                .OrderByDescending(m => m.Role == "Leader")
                .ThenBy(m => m.JoinedAt)
                .ToList();

            // Map project
            var project = group.Projects.FirstOrDefault();
            StudentGroupProjectDto? projectDto = null;
            if (project != null)
            {
                projectDto = new StudentGroupProjectDto
                {
                    ProjectId = project.ProjectId,
                    Title = project.Title ?? "Untitled",
                    Description = project.Description,
                    Status = project.Status ?? "Unknown",
                    CreatedAt = project.CreatedAt
                };
            }

            var groupDetail = new StudentGroupDetailDto
            {
                GroupId = group.GroupId,
                GroupName = group.GroupName ?? "Unknown",
                ClassId = group.ClassId,
                ClassName = group.Class?.ClassName ?? "Unknown",
                Role = isLeader ? "Leader" : "Member",
                Members = members,
                Project = projectDto
            };

            return new ResultModel<StudentGroupDetailDto>
            {
                IsSuccess = true,
                Message = "Group details retrieved successfully",
                Data = groupDetail,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group for user {UserId} in class {ClassId}", userId, classId);
            return new ResultModel<StudentGroupDetailDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving group: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ResultModel<GroupInvitationsResponseDto>> GetGroupInvitationsAsync(int userId)
    {
        try
        {
            // Find unread notifications with type "group_invitation"
            var invitationNotifications = await _context.Notifications
                .Where(n => n.UserId == userId &&
                           n.Type == "group_invitation" &&
                           (n.IsRead == null || n.IsRead == false))
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var invitations = new List<GroupInvitationDto>();

            foreach (var notification in invitationNotifications)
            {
                // Parse notification message to extract group info
                var groupId = ExtractGroupIdFromMessage(notification.Message ?? "");
                if (groupId == null) continue;

                var group = await _context.Groups
                    .Include(g => g.Class)
                    .Include(g => g.Leader)
                    .FirstOrDefaultAsync(g => g.GroupId == groupId);

                if (group == null) continue;

                invitations.Add(new GroupInvitationDto
                {
                    GroupId = group.GroupId,
                    GroupName = group.GroupName ?? "Unknown",
                    ClassId = group.ClassId,
                    ClassName = group.Class?.ClassName ?? "Unknown",
                    InvitedBy = group.Leader?.FullName ?? "Unknown",
                    InvitedAt = notification.CreatedAt ?? DateTime.UtcNow,
                    NotificationId = notification.NotificationId
                });
            }

            var response = new GroupInvitationsResponseDto
            {
                PendingInvitations = invitations,
                TotalCount = invitations.Count
            };

            return new ResultModel<GroupInvitationsResponseDto>
            {
                IsSuccess = true,
                Message = "Invitations retrieved successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invitations for user {UserId}", userId);
            return new ResultModel<GroupInvitationsResponseDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving invitations: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ResultModel<RejectInvitationResponseDto>> RejectGroupInvitationAsync(
        int userId, int groupId, string? reason)
    {
        try
        {
            // Verify group exists
            var group = await _context.Groups
                .Include(g => g.Leader)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
            {
                return new ResultModel<RejectInvitationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Group not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Find invitation notification
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.UserId == userId &&
                    n.Type == "group_invitation" &&
                    (n.Message ?? "").Contains($"groupId:{groupId}"));

            if (notification != null)
            {
                // Mark notification as read
                notification.IsRead = true;
            }

            // Create rejection notification for group leader
            if (group.LeaderId != null)
            {
                var user = await _context.Users.FindAsync(userId);
                var rejectionMessage = string.IsNullOrEmpty(reason)
                    ? $"{user?.FullName ?? "A student"} has declined your invitation to join {group.GroupName}."
                    : $"{user?.FullName ?? "A student"} has declined your invitation to join {group.GroupName}. Reason: {reason}";

                var rejectionNotification = new BusinessObjects.Models.Notification
                {
                    UserId = group.LeaderId.Value,
                    Title = "Group Invitation Declined",
                    Message = rejectionMessage,
                    Type = "group_invitation_rejected",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(rejectionNotification);
            }

            await _context.SaveChangesAsync();

            var response = new RejectInvitationResponseDto
            {
                Success = true,
                Message = "Invitation rejected successfully",
                RejectedAt = DateTime.UtcNow
            };

            return new ResultModel<RejectInvitationResponseDto>
            {
                IsSuccess = true,
                Message = "Invitation rejected successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting invitation for user {UserId} and group {GroupId}", userId, groupId);
            return new ResultModel<RejectInvitationResponseDto>
            {
                IsSuccess = false,
                Message = $"Error rejecting invitation: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    // Helper Methods

    private async Task<StudentStatisticsDto> CalculateStatisticsAsync(int userId, List<int> projectIds)
    {
        var totalClasses = await _context.ClassEnrollments
            .Where(e => e.StudentId == userId)
            .CountAsync();

        var totalGroups = await _context.GroupMembers
            .Where(gm => gm.UserId == userId)
            .CountAsync();

        var totalProjects = projectIds.Count;

        var totalSubmissions = await _context.MilestoneSubmissions
            .Where(s => projectIds.Contains(s.ProjectId))
            .CountAsync();

        // Calculate average grade from milestone evaluations
        // Score is decimal, not decimal?
        var scores = await _context.MilestoneEvaluations
            .Where(e => projectIds.Contains(e.ProjectId))
            .Select(e => e.Score)
            .ToListAsync();

        decimal? averageGrade = scores.Any() ? (decimal)scores.Average() : null;

        // Count pending submissions
        var allMilestones = await _context.ProjectMilestones
            .Where(m => projectIds.Contains(m.ProjectId))
            .CountAsync();

        var submittedMilestones = await _context.MilestoneSubmissions
            .Where(s => projectIds.Contains(s.ProjectId))
            .Select(s => s.MilestoneDefId)
            .Distinct()
            .CountAsync();

        var pendingSubmissions = allMilestones - submittedMilestones;

        return new StudentStatisticsDto
        {
            TotalClasses = totalClasses,
            TotalGroups = totalGroups,
            TotalProjects = totalProjects,
            AverageGrade = averageGrade,
            TotalSubmissions = totalSubmissions,
            PendingSubmissions = Math.Max(0, pendingSubmissions)
        };
    }

    private async Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(List<int> projectIds)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var deadlines = new List<UpcomingDeadlineDto>();

        var milestones = await _context.ProjectMilestones
            .Include(m => m.Project)
            .Where(m => projectIds.Contains(m.ProjectId) && 
                       m.DueDate != null &&
                       m.DueDate > today)
            .OrderBy(m => m.DueDate)
            .Take(10)
            .Select(m => new
            {
                m.MilestoneId,
                m.ProjectId,
                m.Title,
                m.DueDate,
                ProjectTitle = m.Project.Title,
                Weight = m.Weight
            })
            .ToListAsync();

        foreach (var milestone in milestones)
        {
            if (milestone.ProjectTitle == null || !milestone.DueDate.HasValue) continue;

            // Check submission status
            var submission = await _context.MilestoneSubmissions
                .FirstOrDefaultAsync(s => s.ProjectId == milestone.ProjectId &&
                                         s.MilestoneDefId == milestone.MilestoneId);

            var evaluation = await _context.MilestoneEvaluations
                .FirstOrDefaultAsync(e => e.ProjectId == milestone.ProjectId &&
                                         e.MilestoneDefId == milestone.MilestoneId);

            var status = evaluation != null ? "Graded" :
                        submission != null ? "Submitted" : "NotSubmitted";

            var daysRemaining = milestone.DueDate.Value.DayNumber - today.DayNumber;

            deadlines.Add(new UpcomingDeadlineDto
            {
                ProjectId = milestone.ProjectId,
                ProjectTitle = milestone.ProjectTitle,
                MilestoneId = milestone.MilestoneId,
                MilestoneTitle = milestone.Title ?? "Untitled Milestone",
                Deadline = milestone.DueDate.Value.ToDateTime(TimeOnly.MinValue),
                DaysRemaining = daysRemaining,
                Status = status,
                Weight = milestone.Weight
            });
        }

        return deadlines;
    }

    private async Task<List<RecentGradeDto>> GetRecentGradesAsync(List<int> projectIds)
    {
        // Score is decimal, not decimal?
        var recentEvaluations = await _context.MilestoneEvaluations
            .Include(e => e.Project)
            .Include(e => e.MilestoneDef)
            .Where(e => projectIds.Contains(e.ProjectId))
            .OrderByDescending(e => e.EvaluatedAt)
            .Take(5)
            .ToListAsync();

        return recentEvaluations
            .Where(e => e.Project != null && e.MilestoneDef != null)
            .Select(e => new RecentGradeDto
            {
                ProjectId = e.ProjectId,
                ProjectTitle = e.Project!.Title ?? "Unknown",
                MilestoneId = e.MilestoneDefId,
                MilestoneTitle = e.MilestoneDef!.Title ?? "Untitled Milestone",
                Grade = e.Score,  // Score is decimal, not nullable
                GradedAt = e.EvaluatedAt,
                Feedback = e.Feedback
            })
            .ToList();
    }

    private async Task<List<RecentNotificationDto>> GetRecentNotificationsAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync();

        return notifications.Select(n => new RecentNotificationDto
        {
            NotificationId = n.NotificationId,
            Title = n.Title ?? "Notification",
            Message = n.Message ?? "",
            Type = n.Type ?? "general",
            IsRead = n.IsRead ?? false,
            CreatedAt = n.CreatedAt ?? DateTime.UtcNow
        }).ToList();
    }

    private int? ExtractGroupIdFromMessage(string message)
    {
        try
        {
            if (message.Contains("groupId:"))
            {
                var startIndex = message.IndexOf("groupId:") + 8;
                var endIndex = message.IndexOf(")", startIndex);
                if (endIndex == -1) endIndex = message.Length;
                
                var idString = message.Substring(startIndex, endIndex - startIndex).Trim();
                if (int.TryParse(idString, out var groupId))
                {
                    return groupId;
                }
            }
            else if (message.Contains("(ID: "))
            {
                var startIndex = message.IndexOf("(ID: ") + 5;
                var endIndex = message.IndexOf(")", startIndex);
                
                if (endIndex > startIndex)
                {
                    var idString = message.Substring(startIndex, endIndex - startIndex).Trim();
                    if (int.TryParse(idString, out var groupId))
                    {
                        return groupId;
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
