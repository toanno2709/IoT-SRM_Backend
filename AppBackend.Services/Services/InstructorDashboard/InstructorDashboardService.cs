using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.MilestoneSubmissionRepo;
using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Repositories.Repositories.AnnouncementRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
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

            // 1. Get total classes count
            var totalClasses = await _context.Classes
                .Where(c => c.InstructorId == instructorId)
                .CountAsync();

            _logger.LogInformation("Total classes: {Count}", totalClasses);

            // 2. Get total groups count
            var totalGroups = await _context.Groups
                .Where(g => g.Class!.InstructorId == instructorId)
                .CountAsync();

            _logger.LogInformation("Total groups: {Count}", totalGroups);

            // 3. Get total projects count
            var totalProjects = await _context.Projects
                .Where(p => p.Group!.Class!.InstructorId == instructorId)
                .CountAsync();

            _logger.LogInformation("Total projects: {Count}", totalProjects);

            // 4. Get total students count (unique students across all classes)
            var totalStudents = await _context.ClassEnrollments
                .Where(ce => ce.Class!.InstructorId == instructorId)
                .Select(ce => ce.StudentId)
                .Distinct()
                .CountAsync();

            _logger.LogInformation("Total students: {Count}", totalStudents);

            // 5. Get pending proposals count (submissions with Proposal milestone and no approval or pending approval)
            var pendingProposals = await _context.MilestoneSubmissions
                .Include(s => s.MilestoneDef)
                .Include(s => s.ProjectApprovalHistories)
                .Where(s => s.Project.Group!.Class!.InstructorId == instructorId
                    && s.MilestoneDef.Title!.Contains("Proposal")
                    && !s.ProjectApprovalHistories.Any(h => h.Action == "Approved" || h.Action == "Rejected"))
                .CountAsync();

            _logger.LogInformation("Pending proposals: {Count}", pendingProposals);

            // 6. Get submissions to grade (approved submissions without evaluations)
            var submissionsToGrade = await _context.MilestoneSubmissions
                .Where(s => s.Project.Group!.Class!.InstructorId == instructorId
                    && s.ProjectApprovalHistories.Any(h => h.Action == "Approved")
                    && !_context.MilestoneEvaluations.Any(e => 
                        e.ProjectId == s.ProjectId 
                        && e.MilestoneDefId == s.MilestoneDefId
                        && e.InstructorId == instructorId))
                .CountAsync();

            _logger.LogInformation("Submissions to grade: {Count}", submissionsToGrade);

            // 7. Get recent announcements count (last 7 days)
            var recentDate = DateTime.UtcNow.AddDays(-7);
            var recentAnnouncements = await _context.Announcements
                .Where(a => a.AdminId == instructorId && a.CreatedAt >= recentDate)
                .CountAsync();

            _logger.LogInformation("Recent announcements: {Count}", recentAnnouncements);

            // 8. Get recent classes (top 5 with latest activity)
            var recentClasses = await _context.Classes
                .Where(c => c.InstructorId == instructorId)
                .Select(c => new RecentClassDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    SemesterName = c.Semester!.Name,
                    TotalStudents = c.ClassEnrollments!.Count,
                    TotalGroups = c.Groups!.Count,
                    TotalProjects = c.Groups!.SelectMany(g => g.Projects).Count(),
                    PendingProposals = c.Groups!
                        .SelectMany(g => g.Projects)
                        .SelectMany(p => p.MilestoneSubmissions)
                        .Count(s => s.MilestoneDef.Title!.Contains("Proposal")
                            && !s.ProjectApprovalHistories.Any(h => h.Action == "Approved" || h.Action == "Rejected")),
                    LastActivity = c.Groups!
                        .SelectMany(g => g.Projects)
                        .Any() 
                        ? c.Groups!.SelectMany(g => g.Projects).Max(p => p.UpdatedAt) 
                        : c.CreatedAt
                })
                .OrderByDescending(c => c.LastActivity)
                .Take(5)
                .ToListAsync();

            _logger.LogInformation("Recent classes count: {Count}", recentClasses.Count);

            // 9. Get recent activities (10 most recent)
            var recentActivities = new List<RecentActivityDto>();

            // Get recent proposals
            var recentProposalActivities = await _context.MilestoneSubmissions
                .Include(s => s.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.Class)
                .Include(s => s.MilestoneDef)
                .Include(s => s.ProjectApprovalHistories)
                .Where(s => s.Project.Group!.Class!.InstructorId == instructorId
                    && s.MilestoneDef.Title!.Contains("Proposal")
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

            _logger.LogInformation("Dashboard created successfully for instructor {InstructorId}", instructorId);

            return new ResultModel<InstructorDashboardResponseDto>
            {
                IsSuccess = true,
                Message = "Dashboard data retrieved successfully",
                Data = dashboard
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard for instructor {InstructorId}", instructorId);
            return new ResultModel<InstructorDashboardResponseDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving dashboard: {ex.Message}",
                Data = null
            };
        }
    }
}
