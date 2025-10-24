using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.MilestoneSubmissionRepo;
using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Repositories.Repositories.AnnouncementRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.InstructorDashboard;

public interface IInstructorDashboardService
{
    Task<ResultModel<InstructorDashboardResponseDto>> GetDashboardAsync(int instructorId);
}

public class InstructorDashboardService : IInstructorDashboardService
{
    private readonly IClassRepository _classRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMilestoneSubmissionRepository _submissionRepository;
    private readonly IMilestoneEvaluationRepository _evaluationRepository;
    private readonly IAnnouncementRepository _announcementRepository;

    public InstructorDashboardService(
        IClassRepository classRepository,
        IGroupRepository groupRepository,
        IMilestoneSubmissionRepository submissionRepository,
        IMilestoneEvaluationRepository evaluationRepository,
        IAnnouncementRepository announcementRepository)
    {
        _classRepository = classRepository;
        _groupRepository = groupRepository;
        _submissionRepository = submissionRepository;
        _evaluationRepository = evaluationRepository;
        _announcementRepository = announcementRepository;
    }

    public async Task<ResultModel<InstructorDashboardResponseDto>> GetDashboardAsync(int instructorId)
    {
        try
        {
            // 1. Lấy tất cả classes của instructor
            var classes = await _classRepository.GetAssignedClassesAsync(instructorId);
            var totalClasses = classes.Count;

            // 2. Lấy tất cả groups trong các classes
            var allGroups = new List<AppBackend.BusinessObjects.Models.Group>();
            var allProjects = new List<AppBackend.BusinessObjects.Models.Project>();
            var totalStudents = 0;

            foreach (var classEntity in classes)
            {
                var groups = await _groupRepository.GetGroupsByClassAsync(classEntity.ClassId);
                allGroups.AddRange(groups);
                
                totalStudents += classEntity.ClassEnrollments?.Count ?? 0;

                // Get projects from groups
                foreach (var group in groups)
                {
                    if (group.Projects != null)
                    {
                        allProjects.AddRange(group.Projects);
                    }
                }
            }

            // 3. Đếm pending proposals (submissions có status Pending và milestone chứa "Proposal")
            var pendingProposals = await _submissionRepository.GetPendingProposalsByInstructorAsync(instructorId);

            // 4. Đếm submissions cần chấm (submissions approved nhưng chưa có evaluation)
            var allSubmissions = new List<AppBackend.BusinessObjects.Models.MilestoneSubmission>();
            foreach (var project in allProjects)
            {
                var submissions = await _submissionRepository.FindAsync(s => 
                    s.ProjectId == project.ProjectId);
                // Filter approved submissions by checking ProjectApprovalHistory
                var approvedSubmissions = submissions.Where(s => 
                    s.ProjectApprovalHistories.Any(h => h.Action == "Approved")).ToList();
                allSubmissions.AddRange(approvedSubmissions);
            }

            // Check which submissions don't have evaluations yet
            var submissionsToGrade = 0;
            foreach (var submission in allSubmissions)
            {
                var evaluation = await _evaluationRepository.GetByProjectMilestoneInstructorAsync(
                    submission.ProjectId, 
                    submission.MilestoneDefId, 
                    instructorId);
                if (evaluation == null)
                {
                    submissionsToGrade++;
                }
            }

            // 5. Đếm announcements gần đây (7 ngày gần nhất)
            var recentDate = DateTime.UtcNow.AddDays(-7);
            var announcements = await _announcementRepository.GetAnnouncementsByAdminAsync(instructorId);
            var recentAnnouncements = announcements.Count(a => a.CreatedAt >= recentDate);

            // 6. Tạo recent classes (top 5 classes có hoạt động gần nhất)
            var recentClasses = new List<RecentClassDto>();
            foreach (var classEntity in classes.Take(5))
            {
                var groups = allGroups.Where(g => g.ClassId == classEntity.ClassId).ToList();
                var projects = allProjects.Where(p => groups.Any(g => g.GroupId == p.GroupId)).ToList();
                var classPendingProposals = pendingProposals.Count(p => 
                    p.Project?.Group?.ClassId == classEntity.ClassId);

                recentClasses.Add(new RecentClassDto
                {
                    ClassId = classEntity.ClassId,
                    ClassName = classEntity.ClassName,
                    SemesterName = classEntity.Semester?.Name,
                    TotalStudents = classEntity.ClassEnrollments?.Count ?? 0,
                    TotalGroups = groups.Count,
                    TotalProjects = projects.Count,
                    PendingProposals = classPendingProposals,
                    LastActivity = projects.Any() ? projects.Max(p => p.UpdatedAt) : classEntity.CreatedAt
                });
            }

            // 7. Tạo recent activities (10 hoạt động gần nhất)
            var recentActivities = new List<RecentActivityDto>();

            // Recent proposals
            foreach (var proposal in pendingProposals.Take(5))
            {
                recentActivities.Add(new RecentActivityDto
                {
                    ActivityType = "Proposal",
                    Description = $"New proposal: {proposal.Project?.Title}",
                    RelatedClass = proposal.Project?.Group?.Class?.ClassName,
                    RelatedGroup = proposal.Project?.Group?.GroupName,
                    ActivityDate = proposal.LastSubmittedAt
                });
            }

            // Recent announcements
            foreach (var announcement in announcements.Take(3))
            {
                recentActivities.Add(new RecentActivityDto
                {
                    ActivityType = "Announcement",
                    Description = announcement.Title,
                    RelatedClass = "All Classes",
                    ActivityDate = announcement.CreatedAt
                });
            }

            // Sort by date
            recentActivities = recentActivities
                .OrderByDescending(a => a.ActivityDate)
                .Take(10)
                .ToList();

            // 8. Tạo response
            var dashboard = new InstructorDashboardResponseDto
            {
                TotalClasses = totalClasses,
                TotalGroups = allGroups.Count,
                TotalProjects = allProjects.Count,
                TotalStudents = totalStudents,
                PendingProposals = pendingProposals.Count,
                SubmissionsToGrade = submissionsToGrade,
                RecentAnnouncements = recentAnnouncements,
                RecentClasses = recentClasses.OrderByDescending(c => c.LastActivity).ToList(),
                RecentActivities = recentActivities
            };

            return new ResultModel<InstructorDashboardResponseDto>
            {
                IsSuccess = true,
                Message = "Dashboard data retrieved successfully",
                Data = dashboard
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<InstructorDashboardResponseDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving dashboard: {ex.Message}",
                Data = null
            };
        }
    }
}


