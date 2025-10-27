using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.ClassStats;

public interface IClassStatsService
{
    Task<ResultModel<ClassStatsResponseDto>> GetClassStatsAsync(int classId);
}

public class ClassStatsService : IClassStatsService
{
    private readonly IClassRepository _classRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IMilestoneEvaluationRepository _milestoneEvaluationRepository;

    public ClassStatsService(
        IClassRepository classRepository,
        IGroupRepository groupRepository,
        IProjectRepository projectRepository,
        IMilestoneEvaluationRepository milestoneEvaluationRepository)
    {
        _classRepository = classRepository;
        _groupRepository = groupRepository;
        _projectRepository = projectRepository;
        _milestoneEvaluationRepository = milestoneEvaluationRepository;
    }

    public async Task<ResultModel<ClassStatsResponseDto>> GetClassStatsAsync(int classId)
    {
        try
        {
            // Get class info
            var classEntity = await _classRepository.GetClassWithDetailsAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<ClassStatsResponseDto>
                {
                    IsSuccess = false,
                    Message = "Class not found",
                    Data = null
                };
            }

            // Get all groups in this class
            var groups = await _groupRepository.GetGroupsByClassAsync(classId);
            
            // Get all projects from these groups
            var allProjects = new List<AppBackend.BusinessObjects.Models.Project>();
            foreach (var group in groups)
            {
                var projects = await _projectRepository.FindAsync(p => p.GroupId == group.GroupId);
                allProjects.AddRange(projects);
            }

            // Get all milestone evaluations for these projects
            var allEvaluations = new List<AppBackend.BusinessObjects.Models.MilestoneEvaluation>();
            foreach (var project in allProjects)
            {
                var evaluations = await _milestoneEvaluationRepository.GetByProjectAsync(project.ProjectId);
                allEvaluations.AddRange(evaluations);
            }

            // Calculate statistics
            var totalStudents = classEntity.ClassEnrollments?.Count ?? 0;
            var totalGroups = groups.Count;
            var totalProjects = allProjects.Count;
            var evaluatedProjects = allProjects.Count(p => allEvaluations.Any(e => e.ProjectId == p.ProjectId));
            
            // Calculate average score weighted by milestone weights
            var projectScores = new List<decimal>();
            foreach (var project in allProjects)
            {
                var projectEvaluations = allEvaluations.Where(e => e.ProjectId == project.ProjectId).ToList();
                if (projectEvaluations.Any())
                {
                    // Calculate weighted average: Σ(score × weight)
                    decimal weightedScore = 0;
                    foreach (var eval in projectEvaluations)
                    {
                        weightedScore += eval.Score * (eval.WeightRatioSnapshot / 100);
                    }
                    projectScores.Add(weightedScore);
                }
            }

            var averageScore = projectScores.Any() ? projectScores.Average() : 0;

            // Create project details
            var projectDetails = allProjects.Select(p =>
            {
                var group = groups.FirstOrDefault(g => g.GroupId == p.GroupId);
                var projectEvaluations = allEvaluations.Where(e => e.ProjectId == p.ProjectId).ToList();
                
                // Calculate weighted score for this project
                decimal? totalScore = null;
                if (projectEvaluations.Any())
                {
                    decimal weightedScore = 0;
                    foreach (var eval in projectEvaluations)
                    {
                        weightedScore += eval.Score * (eval.WeightRatioSnapshot / 100);
                    }
                    totalScore = weightedScore;
                }
                
                return new ProjectStatsDto
                {
                    ProjectId = p.ProjectId,
                    ProjectTitle = p.Title,
                    LeaderName = group?.Leader?.FullName,
                    Status = p.Status ?? "Active",
                    TotalScore = totalScore,
                    LastSubmissionDate = p.UpdatedAt,
                    MemberCount = group?.GroupMembers?.Count ?? 0
                };
            }).ToList();

            var stats = new ClassStatsResponseDto
            {
                ClassId = classEntity.ClassId,
                ClassName = classEntity.ClassName,
                TotalStudents = totalStudents,
                TotalProjects = totalProjects,
                SubmittedProjects = allProjects.Count(p => p.Status != null),
                PendingProjects = allProjects.Count(p => p.Status == "Pending"),
                ApprovedProjects = allProjects.Count(p => p.Status == "Approved"),
                RejectedProjects = allProjects.Count(p => p.Status == "Rejected"),
                SubmissionRate = totalProjects > 0 ? Math.Round((double)allProjects.Count(p => p.Status != null) / totalProjects * 100, 2) : 0,
                AverageScore = Math.Round((double)averageScore, 2),
                EvaluatedProjects = evaluatedProjects,
                LastSubmissionDate = allProjects.Any() ? allProjects.Max(p => p.UpdatedAt) : null,
                ProjectDetails = projectDetails
            };

            return new ResultModel<ClassStatsResponseDto>
            {
                IsSuccess = true,
                Message = "Class statistics retrieved successfully",
                Data = stats
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassStatsResponseDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving class statistics: {ex.Message}",
                Data = null
            };
        }
    }
}
