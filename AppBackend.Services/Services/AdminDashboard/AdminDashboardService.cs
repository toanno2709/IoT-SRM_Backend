using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Repositories.Repositories.SemesterRepo;
using AppBackend.Repositories.Repositories.MilestoneSubmissionRepo;
using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Repositories.Repositories.AnnouncementRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.AdminDashboard;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IUserRepository _userRepository;
    private readonly IClassRepository _classRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IMilestoneSubmissionRepository _submissionRepository;
    private readonly IMilestoneEvaluationRepository _evaluationRepository;
    private readonly IAnnouncementRepository _announcementRepository;

    public AdminDashboardService(
        IUserRepository userRepository,
        IClassRepository classRepository,
        IGroupRepository groupRepository,
        IProjectRepository projectRepository,
        ISemesterRepository semesterRepository,
        IMilestoneSubmissionRepository submissionRepository,
        IMilestoneEvaluationRepository evaluationRepository,
        IAnnouncementRepository announcementRepository)
    {
        _userRepository = userRepository;
        _classRepository = classRepository;
        _groupRepository = groupRepository;
        _projectRepository = projectRepository;
        _semesterRepository = semesterRepository;
        _submissionRepository = submissionRepository;
        _evaluationRepository = evaluationRepository;
        _announcementRepository = announcementRepository;
    }

    public async Task<ResultModel<AdminDashboardOverviewDto>> GetDashboardOverviewAsync()
    {
        try
        {
            // Get all entities and convert to lists
            var allClasses = (await _classRepository.GetAllAsync()).ToList();
            var allUsers = (await _userRepository.GetAllAsync()).ToList();
            var allGroups = (await _groupRepository.GetAllAsync()).ToList();
            var allProjects = (await _projectRepository.GetAllAsync()).ToList();
            var allSemesters = (await _semesterRepository.GetAllAsync()).ToList();
            var allAnnouncements = (await _announcementRepository.GetAllAsync()).ToList();

            // Count by roles
            var instructors = allUsers.Where(u => u.RoleId == 2).ToList();
            var students = allUsers.Where(u => u.RoleId == 3).ToList();

            // Active semester
            var activeSemesters = allSemesters.Where(s => s.IsActive == true).ToList();

            // Pending approvals (projects with Pending status)
            var pendingApprovals = allProjects.Count(p => p.Status == "Pending");

            // Recent announcements (last 7 days)
            var recentDate = DateTime.UtcNow.AddDays(-7);
            var recentAnnouncements = allAnnouncements.Count(a => a.CreatedAt >= recentDate);

            // Completed projects
            var completedProjects = allProjects.Count(p => p.Status == "Completed");

            // Recent activities (Last 10)
            var recentActivities = new List<AdminActivityDto>();

            // Recent classes
            foreach (var cls in allClasses.OrderByDescending(c => c.CreatedAt).Take(3))
            {
                recentActivities.Add(new AdminActivityDto
                {
                    ActivityType = "ClassCreated",
                    Description = $"Class '{cls.ClassName}' was created",
                    PerformedBy = "Admin",
                    ActivityDate = cls.CreatedAt,
                    RelatedEntity = cls.ClassName
                });
            }

            // Recent users
            foreach (var user in allUsers.OrderByDescending(u => u.CreatedAt).Take(3))
            {
                var roleName = user.RoleId == 1 ? "Admin" : user.RoleId == 2 ? "Instructor" : "Student";
                recentActivities.Add(new AdminActivityDto
                {
                    ActivityType = "UserAdded",
                    Description = $"{roleName} '{user.FullName}' was added",
                    PerformedBy = "Admin",
                    ActivityDate = user.CreatedAt,
                    RelatedEntity = user.Email
                });
            }

            // Recent projects
            foreach (var project in allProjects.OrderByDescending(p => p.CreatedAt).Take(3))
            {
                recentActivities.Add(new AdminActivityDto
                {
                    ActivityType = "ProjectCreated",
                    Description = $"Project '{project.Title}' was created",
                    PerformedBy = "Group",
                    ActivityDate = project.CreatedAt,
                    RelatedEntity = project.Title
                });
            }

            // Sort activities by date
            recentActivities = recentActivities
                .OrderByDescending(a => a.ActivityDate)
                .Take(10)
                .ToList();

            // System alerts
            var systemAlerts = new List<SystemAlertDto>();

            // Check for classes without instructors
            var classesWithoutInstructor = allClasses.Count(c => c.InstructorId == null);
            if (classesWithoutInstructor > 0)
            {
                systemAlerts.Add(new SystemAlertDto
                {
                    AlertType = "Warning",
                    Message = $"{classesWithoutInstructor} class(es) do not have an assigned instructor",
                    CreatedAt = DateTime.UtcNow,
                    IsResolved = false
                });
            }

            // Check for pending approvals
            if (pendingApprovals > 10)
            {
                systemAlerts.Add(new SystemAlertDto
                {
                    AlertType = "Info",
                    Message = $"{pendingApprovals} project proposals are pending approval",
                    CreatedAt = DateTime.UtcNow,
                    IsResolved = false
                });
            }

            var overview = new AdminDashboardOverviewDto
            {
                TotalClasses = allClasses.Count,
                TotalInstructors = instructors.Count,
                TotalStudents = students.Count,
                TotalGroups = allGroups.Count,
                TotalProjects = allProjects.Count,
                ActiveSemesterCount = activeSemesters.Count,
                PendingApprovals = pendingApprovals,
                RecentAnnouncements = recentAnnouncements,
                CompletedProjects = completedProjects,
                RecentActivities = recentActivities,
                SystemAlerts = systemAlerts
            };

            return new ResultModel<AdminDashboardOverviewDto>
            {
                IsSuccess = true,
                Message = "Admin dashboard overview retrieved successfully",
                Data = overview,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<AdminDashboardOverviewDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving admin dashboard: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<AdminStatisticsDto>> GetSystemStatisticsAsync()
    {
        try
        {
            // Get all data and convert to lists
            var allUsers = (await _userRepository.GetAllAsync()).ToList();
            var allClasses = (await _classRepository.GetAllAsync()).ToList();
            var allGroups = (await _groupRepository.GetAllAsync()).ToList();
            var allProjects = (await _projectRepository.GetAllAsync()).ToList();
            var allSemesters = (await _semesterRepository.GetAllAsync()).ToList();
            var allSubmissions = (await _submissionRepository.GetAllAsync()).ToList();
            var allEvaluations = (await _evaluationRepository.GetAllAsync()).ToList();
            var allAnnouncements = (await _announcementRepository.GetAllAsync()).ToList();

            // User Statistics
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var userStats = new UserStatisticsDto
            {
                TotalUsers = allUsers.Count,
                TotalAdmins = allUsers.Count(u => u.RoleId == 1),
                TotalInstructors = allUsers.Count(u => u.RoleId == 2),
                TotalStudents = allUsers.Count(u => u.RoleId == 3),
                ActiveUsersLast30Days = allUsers.Count(u => u.UpdatedAt.HasValue && u.UpdatedAt.Value >= thirtyDaysAgo),
                NewUsersThisMonth = allUsers.Count(u => u.CreatedAt.HasValue && 
                                                        u.CreatedAt.Value.Month == DateTime.UtcNow.Month && 
                                                        u.CreatedAt.Value.Year == DateTime.UtcNow.Year)
            };

            // Class Statistics
            var classStats = new ClassStatisticsDto
            {
                TotalClasses = allClasses.Count,
                ActiveClasses = allClasses.Count, // All classes are considered active
                TotalSemesters = allSemesters.Count,
                ActiveSemesters = allSemesters.Count(s => s.IsActive == true),
                AverageClassSize = allClasses.Any() 
                    ? (decimal)allClasses.Average(c => c.ClassEnrollments?.Count ?? 0) 
                    : 0,
                ClassesWithoutInstructor = allClasses.Count(c => c.InstructorId == null)
            };

            // Project Statistics
            var totalGroupMembers = 0;
            foreach (var group in allGroups)
            {
                totalGroupMembers += group.GroupMembers?.Count ?? 0;
            }

            var projectStats = new ProjectStatisticsDto
            {
                TotalProjects = allProjects.Count,
                PendingProjects = allProjects.Count(p => p.Status == "Pending"),
                ApprovedProjects = allProjects.Count(p => p.Status == "Approved"),
                CompletedProjects = allProjects.Count(p => p.Status == "Completed"),
                RejectedProjects = allProjects.Count(p => p.Status == "Rejected"),
                TotalGroups = allGroups.Count,
                AverageGroupSize = allGroups.Any() && totalGroupMembers > 0
                    ? (decimal)totalGroupMembers / allGroups.Count 
                    : 0,
                ProjectCompletionRate = allProjects.Any() 
                    ? (decimal)allProjects.Count(p => p.Status == "Completed") / allProjects.Count * 100 
                    : 0
            };

            // Submission Statistics
            var gradedSubmissions = allSubmissions.Count(s => 
                allEvaluations.Any(e => e.ProjectId == s.ProjectId && e.MilestoneDefId == s.MilestoneDefId));

            var lateSubmissions = allSubmissions.Count(s => 
                s.MilestoneDef != null && 
                s.MilestoneDef.DueDate.HasValue && 
                s.LastSubmittedAt.HasValue &&
                s.LastSubmittedAt.Value > s.MilestoneDef.DueDate.Value.ToDateTime(TimeOnly.MinValue));

            var submissionStats = new AdminSubmissionStatisticsDto
            {
                TotalSubmissions = allSubmissions.Count,
                GradedSubmissions = gradedSubmissions,
                PendingGrading = allSubmissions.Count - gradedSubmissions,
                LateSubmissions = lateSubmissions,
                AverageScore = allEvaluations.Any()
                    ? allEvaluations.Average(e => e.Score) 
                    : 0,
                SubmissionRate = allProjects.Any() 
                    ? (decimal)allSubmissions.Count / (allProjects.Count * 5) * 100 // Assuming 5 milestones average
                    : 0,
                OnTimeSubmissionRate = allSubmissions.Any() 
                    ? (decimal)(allSubmissions.Count - lateSubmissions) / allSubmissions.Count * 100 
                    : 0
            };

            // System Health
            var systemHealth = new SystemHealthDto
            {
                TotalAnnouncements = allAnnouncements.Count,
                ActiveAnnouncements = allAnnouncements.Count(a => 
                    a.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
                LastBackupDate = null, // TODO: Implement backup tracking
                DatabaseSizeInMB = 0, // TODO: Implement database size calculation
                SystemStatus = "Healthy"
            };

            var statistics = new AdminStatisticsDto
            {
                UserStats = userStats,
                ClassStats = classStats,
                ProjectStats = projectStats,
                SubmissionStats = submissionStats,
                SystemHealth = systemHealth
            };

            return new ResultModel<AdminStatisticsDto>
            {
                IsSuccess = true,
                Message = "System statistics retrieved successfully",
                Data = statistics,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<AdminStatisticsDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving statistics: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<ClassesBySemesterChartDto>> GetClassesBySemesterChartAsync()
    {
        try
        {
            var allClasses = (await _classRepository.GetAllAsync()).ToList();
            var allSemesters = (await _semesterRepository.GetAllAsync()).ToList();

            var chartData = new List<ChartDataPointDto>();

            foreach (var semester in allSemesters.OrderBy(s => s.StartDate))
            {
                var classCount = allClasses.Count(c => c.SemesterId == semester.SemesterId);
                
                chartData.Add(new ChartDataPointDto
                {
                    Label = semester.Name,
                    Value = classCount,
                    Color = GetChartColor(semester.SemesterId),
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "SemesterId", semester.SemesterId },
                        { "StartDate", semester.StartDate.HasValue ? semester.StartDate.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue },
                        { "EndDate", semester.EndDate.HasValue ? semester.EndDate.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue }
                    }
                });
            }

            var chart = new ClassesBySemesterChartDto
            {
                ChartData = chartData,
                ChartType = "Bar",
                Title = "Classes by Semester"
            };

            return new ResultModel<ClassesBySemesterChartDto>
            {
                IsSuccess = true,
                Message = "Chart data retrieved successfully",
                Data = chart,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassesBySemesterChartDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving chart data: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<ProjectDistributionChartDto>> GetProjectDistributionChartAsync()
    {
        try
        {
            var allProjects = (await _projectRepository.GetAllAsync()).ToList();

            var statusGroups = allProjects
                .GroupBy(p => p.Status ?? "Unknown")
                .Select(g => new ChartDataPointDto
                {
                    Label = g.Key,
                    Value = g.Count(),
                    Color = GetStatusColor(g.Key)
                })
                .ToList();

            var chart = new ProjectDistributionChartDto
            {
                ChartData = statusGroups,
                ChartType = "Pie",
                Title = "Project Status Distribution"
            };

            return new ResultModel<ProjectDistributionChartDto>
            {
                IsSuccess = true,
                Message = "Chart data retrieved successfully",
                Data = chart,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ProjectDistributionChartDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving chart data: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<MilestoneCompletionChartDto>> GetMilestoneCompletionChartAsync()
    {
        try
        {
            var allSubmissions = (await _submissionRepository.GetAllAsync()).ToList();
            var allEvaluations = (await _evaluationRepository.GetAllAsync()).ToList();

            // Group by milestone and calculate completion rate
            var milestoneGroups = allSubmissions
                .GroupBy(s => s.MilestoneDef?.Title ?? "Unknown")
                .Select(g => new ChartDataPointDto
                {
                    Label = g.Key,
                    Value = g.Count(s => allEvaluations.Any(e => 
                        e.ProjectId == s.ProjectId && 
                        e.MilestoneDefId == s.MilestoneDefId)),
                    Color = GetChartColor(g.GetHashCode()),
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "TotalSubmissions", g.Count() },
                        { "CompletionRate", g.Any() 
                            ? (decimal)g.Count(s => allEvaluations.Any(e => 
                                e.ProjectId == s.ProjectId && 
                                e.MilestoneDefId == s.MilestoneDefId)) / g.Count() * 100 
                            : 0 }
                    }
                })
                .ToList();

            var chart = new MilestoneCompletionChartDto
            {
                ChartData = milestoneGroups,
                ChartType = "Line",
                Title = "Milestone Completion Progress"
            };

            return new ResultModel<MilestoneCompletionChartDto>
            {
                IsSuccess = true,
                Message = "Chart data retrieved successfully",
                Data = chart,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<MilestoneCompletionChartDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving chart data: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    #region Helper Methods

    private string GetChartColor(int index)
    {
        var colors = new[]
        {
            "#4CAF50", "#2196F3", "#FFC107", "#FF5722", "#9C27B0",
            "#00BCD4", "#FF9800", "#8BC34A", "#E91E63", "#3F51B5"
        };
        
        return colors[Math.Abs(index) % colors.Length];
    }

    private string GetStatusColor(string status)
    {
        return status switch
        {
            "Pending" => "#FFC107",      // Yellow
            "Approved" => "#4CAF50",     // Green
            "Completed" => "#2196F3",    // Blue
            "Rejected" => "#FF5722",     // Red
            "In Progress" => "#00BCD4",  // Cyan
            _ => "#9E9E9E"               // Gray
        };
    }

    #endregion
}
