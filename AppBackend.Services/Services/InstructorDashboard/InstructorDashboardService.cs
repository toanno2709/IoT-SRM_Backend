using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.MilestoneSubmissionRepo;
using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Repositories.Repositories.AnnouncementRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Constants;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.InstructorDashboard;

public class InstructorDashboardService : IInstructorDashboardService
{
    private readonly IClassRepository _classRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMilestoneSubmissionRepository _submissionRepository;
    private readonly IMilestoneEvaluationRepository _evaluationRepository;
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly IotShowroomContext _context;
    private readonly ILogger<InstructorDashboardService> _logger;

    public InstructorDashboardService(
        IClassRepository classRepository,
        IGroupRepository groupRepository,
        IMilestoneSubmissionRepository submissionRepository,
        IMilestoneEvaluationRepository evaluationRepository,
        IAnnouncementRepository announcementRepository,
        IotShowroomContext context,
        ILogger<InstructorDashboardService> logger)
    {
        _classRepository = classRepository;
        _groupRepository = groupRepository;
        _submissionRepository = submissionRepository;
        _evaluationRepository = evaluationRepository;
        _announcementRepository = announcementRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<InstructorDashboardResponseDto>> GetDashboardAsync(int instructorId)
    {
        try
        {
            _logger.LogInformation("Getting dashboard for instructor {InstructorId}", instructorId);

            // Get all class IDs for this instructor first
            var classIds = await _context.Classes
                .Where(c => c.InstructorId == instructorId)
                .Select(c => c.ClassId)
                .ToListAsync();

            _logger.LogInformation("Instructor {InstructorId} has {Count} classes: {ClassIds}", 
                instructorId, classIds.Count, string.Join(", ", classIds));

            // 1. Get total classes count
            var totalClasses = classIds.Count;

            // 2. Get total groups count (groups in instructor's classes)
            var totalGroups = await _context.Groups
                .Where(g => classIds.Contains(g.ClassId))
                .CountAsync();

            _logger.LogInformation("Total groups in instructor's classes: {Count}", totalGroups);

            // 3. Get total projects count (projects in instructor's groups)
            var groupIds = await _context.Groups
                .Where(g => classIds.Contains(g.ClassId))
                .Select(g => g.GroupId)
                .ToListAsync();

            var totalProjects = await _context.Projects
                .Where(p => p.GroupId.HasValue).Where(p => groupIds.Contains((int)p.GroupId!))
                .CountAsync();

            _logger.LogInformation("Total projects: {Count}", totalProjects);

            // 4. Get total students count (unique students enrolled in instructor's classes)
            var totalStudents = await _context.ClassEnrollments
                .Where(ce => ce.ClassId.HasValue).Where(ce => classIds.Contains((int)ce.ClassId!))
                .Select(ce => ce.StudentId)
                .Distinct()
                .CountAsync();

            _logger.LogInformation("Total students: {Count}", totalStudents);

            // 5. Get pending proposals count
            var projectIds = await _context.Projects
                .Where(p => p.GroupId.HasValue).Where(p => groupIds.Contains((int)p.GroupId!))
                .Select(p => p.ProjectId)
                .ToListAsync();

            var pendingProposals = await _context.MilestoneSubmissions
                .Include(s => s.MilestoneDef)
                .Include(s => s.ProjectApprovalHistories)
                .Where(s => projectIds.Contains(s.ProjectId)
                    && s.MilestoneDef != null
                    && s.MilestoneDef.Title != null
                    && s.MilestoneDef.Title.Contains("Proposal")
                    && !s.ProjectApprovalHistories.Any(h => h.Action == "Approved" || h.Action == "Rejected"))
                .CountAsync();

            _logger.LogInformation("Pending proposals: {Count}", pendingProposals);

            // 6. Get submissions to grade
            var submissionsToGrade = await _context.MilestoneSubmissions
                .Include(s => s.ProjectApprovalHistories)
                .Where(s => projectIds.Contains(s.ProjectId)
                    && s.ProjectApprovalHistories.Any(h => h.Action == "Approved")
                    && !_context.MilestoneEvaluations.Any(e => 
                        e.ProjectId == s.ProjectId 
                        && e.MilestoneDefId == s.MilestoneDefId
                        && e.InstructorId == instructorId))
                .CountAsync();

            _logger.LogInformation("Submissions to grade: {Count}", submissionsToGrade);

            // 7. Get recent announcements count (last 7 days by this instructor)
            var recentDate = DateTime.UtcNow.AddDays(-7);
            var recentAnnouncements = await _context.Announcements
                .Where(a => a.AdminId == instructorId && a.CreatedAt >= recentDate)
                .CountAsync();

            _logger.LogInformation("Recent announcements: {Count}", recentAnnouncements);

            // 8. Get recent classes with details
            var recentClasses = new List<RecentClassDto>();
            
            if (classIds.Any())
            {
                var classesData = await _context.Classes
                    .Include(c => c.Semester)
                    .Include(c => c.ClassEnrollments)
                    .Include(c => c.Groups)
                        .ThenInclude(g => g.Projects)
                            .ThenInclude(p => p.MilestoneSubmissions)
                                .ThenInclude(ms => ms.MilestoneDef)
                    .Include(c => c.Groups)
                        .ThenInclude(g => g.Projects)
                            .ThenInclude(p => p.MilestoneSubmissions)
                                .ThenInclude(ms => ms.ProjectApprovalHistories)
                    .Where(c => classIds.Contains(c.ClassId))
                    .ToListAsync();

                recentClasses = classesData.Select(c => new RecentClassDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    SemesterName = c.Semester?.Name,
                    TotalStudents = c.ClassEnrollments?.Count ?? 0,
                    TotalGroups = c.Groups?.Count ?? 0,
                    TotalProjects = c.Groups?.SelectMany(g => g.Projects).Count() ?? 0,
                    PendingProposals = c.Groups?
                        .SelectMany(g => g.Projects)
                        .SelectMany(p => p.MilestoneSubmissions)
                        .Count(s => s.MilestoneDef != null 
                            && s.MilestoneDef.Title != null 
                            && s.MilestoneDef.Title.Contains("Proposal")
                            && !s.ProjectApprovalHistories.Any(h => h.Action == "Approved" || h.Action == "Rejected")) ?? 0,
                    LastActivity = c.Groups != null && c.Groups.SelectMany(g => g.Projects).Any()
                        ? c.Groups.SelectMany(g => g.Projects).Max(p => p.UpdatedAt)
                        : c.CreatedAt
                })
                .OrderByDescending(c => c.LastActivity)
                .Take(5)
                .ToList();
            }

            _logger.LogInformation("Recent classes count: {Count}", recentClasses.Count);

            // 9. Get recent activities
            var recentActivities = new List<RecentActivityDto>();

            // Get recent proposals
            if (projectIds.Any())
            {
                var recentProposalActivities = await _context.MilestoneSubmissions
                    .Include(s => s.Project)
                        .ThenInclude(p => p.Group)
                            .ThenInclude(g => g!.Class)
                    .Include(s => s.MilestoneDef)
                    .Include(s => s.ProjectApprovalHistories)
                    .Where(s => projectIds.Contains(s.ProjectId)
                        && s.MilestoneDef != null
                        && s.MilestoneDef.Title != null
                        && s.MilestoneDef.Title.Contains("Proposal")
                        && !s.ProjectApprovalHistories.Any(h => h.Action == "Approved" || h.Action == "Rejected"))
                    .OrderByDescending(s => s.LastSubmittedAt)
                    .Take(5)
                    .Select(s => new RecentActivityDto
                    {
                        ActivityType = "Proposal",
                        Description = $"New proposal: {s.Project.Title}",
                        RelatedClass = s.Project.Group!.Class!.ClassName,
                        RelatedGroup = s.Project.Group.GroupName,
                        ActivityDate = s.LastSubmittedAt
                    })
                    .ToListAsync();

                recentActivities.AddRange(recentProposalActivities);
            }

            // Get recent announcements
            var recentAnnouncementActivities = await _context.Announcements
                .Where(a => a.AdminId == instructorId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(3)
                .Select(a => new RecentActivityDto
                {
                    ActivityType = "Announcement",
                    Description = a.Title,
                    RelatedClass = "All Classes",
                    RelatedGroup = null,
                    ActivityDate = a.CreatedAt
                })
                .ToListAsync();

            recentActivities.AddRange(recentAnnouncementActivities);

            // Sort and take top 10
            recentActivities = recentActivities
                .OrderByDescending(a => a.ActivityDate)
                .Take(10)
                .ToList();

            _logger.LogInformation("Recent activities count: {Count}", recentActivities.Count);

            // 10. Create response
            var dashboard = new InstructorDashboardResponseDto
            {
                TotalClasses = totalClasses,
                TotalGroups = totalGroups,
                TotalProjects = totalProjects,
                TotalStudents = totalStudents,
                PendingProposals = pendingProposals,
                SubmissionsToGrade = submissionsToGrade,
                RecentAnnouncements = recentAnnouncements,
                RecentClasses = recentClasses,
                RecentActivities = recentActivities
            };

            _logger.LogInformation("Dashboard created successfully for instructor {InstructorId}. Classes: {Classes}, Groups: {Groups}, Projects: {Projects}, Students: {Students}", 
                instructorId, totalClasses, totalGroups, totalProjects, totalStudents);

            return new ResultModel<InstructorDashboardResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Dashboard data retrieved successfully",
                Data = dashboard,
                StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard for instructor {InstructorId}", instructorId);
            return new ResultModel<InstructorDashboardResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error retrieving dashboard: {ex.Message}",
                Data = null,
                StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError
            };
        }
    }
}


