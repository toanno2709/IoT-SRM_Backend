using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Data;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.StudentGrade;

public class StudentGradeService : IStudentGradeService
{
    private readonly IotShowroomContext _context;
    private readonly ILogger<StudentGradeService> _logger;

    public StudentGradeService(IotShowroomContext context, ILogger<StudentGradeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<StudentGradesResponseDto>> GetMyGradesAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "User not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Get all groups student belongs to
            var groupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            // Get all projects from these groups
            var projects = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                        .ThenInclude(c => c!.Semester)
                .Where(p => groupIds.Contains(p.GroupId ?? 0))
                .ToListAsync();

            var projectGrades = new List<StudentProjectGradeDto>();

            foreach (var project in projects)
            {
                var projectGrade = await BuildProjectGradeDto(project);
                projectGrades.Add(projectGrade);
            }

            var response = new StudentGradesResponseDto
            {
                StudentId = userId,
                StudentName = user.FullName,
                Email = user.Email,
                Projects = projectGrades
            };

            return new ResultModel<StudentGradesResponseDto>
            {
                IsSuccess = true,
                Message = "Grades retrieved successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student grades");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting grades: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<StudentProjectGradeDto>> GetProjectGradesAsync(int projectId, int userId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                        .ThenInclude(c => c!.Semester)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Verify user is in project group
            var isMember = project.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not a member of this project",
                    StatusCodes.Status403Forbidden
                );
            }

            var projectGrade = await BuildProjectGradeDto(project);

            return new ResultModel<StudentProjectGradeDto>
            {
                IsSuccess = true,
                Message = "Project grades retrieved successfully",
                Data = projectGrade,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project grades");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting project grades: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ProjectFeedbackResponseDto>> GetProjectFeedbackAsync(int projectId, int userId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Verify user is in project group
            var isMember = project.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not a member of this project",
                    StatusCodes.Status403Forbidden
                );
            }

            // Get proposal feedback
            ProposalFeedbackDto? proposalFeedback = null;
            var latestApproval = await _context.ProjectApprovalHistories
                .Include(h => h.Reviewer)
                .Where(h => h.Submission!.ProjectId == projectId)
                .OrderByDescending(h => h.ActedAt)
                .FirstOrDefaultAsync();

            if (latestApproval != null)
            {
                proposalFeedback = new ProposalFeedbackDto
                {
                    Status = latestApproval.Action ?? "Pending",
                    ReviewedBy = latestApproval.Reviewer?.FullName,
                    ReviewedAt = latestApproval.ActedAt,
                    Comment = latestApproval.Comment
                };
            }

            // Get milestone feedback
            var evaluations = await _context.MilestoneEvaluations
                .Include(e => e.Instructor)
                .Include(e => e.MilestoneDef)
                .Where(e => e.ProjectId == projectId)
                .ToListAsync();

            var milestoneFeedback = evaluations.Select(e => new MilestoneFeedbackDto
            {
                MilestoneId = e.MilestoneDefId,
                MilestoneTitle = e.MilestoneDef?.Title,
                Grade = e.Score,
                Feedback = e.Feedback,
                GradedBy = e.Instructor?.FullName,
                GradedAt = e.EvaluatedAt
            }).ToList();

            var response = new ProjectFeedbackResponseDto
            {
                ProjectId = projectId,
                ProjectTitle = project.Title,
                ProjectStatus = project.Status,
                ProposalFeedback = proposalFeedback,
                MilestoneFeedback = milestoneFeedback
            };

            return new ResultModel<ProjectFeedbackResponseDto>
            {
                IsSuccess = true,
                Message = "Feedback retrieved successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project feedback");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting feedback: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ProjectOverallGradeDto>> GetProjectOverallGradeAsync(int projectId, int userId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Verify user is in project group
            var isMember = project.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not a member of this project",
                    StatusCodes.Status403Forbidden
                );
            }

            // Get all milestones
            var milestones = await _context.ProjectMilestones
                .Where(m => m.ProjectId == projectId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Get all evaluations
            var evaluations = await _context.MilestoneEvaluations
                .Include(e => e.MilestoneDef)
                .Where(e => e.ProjectId == projectId)
                .ToListAsync();

            var contributions = new List<MilestoneGradeContributionDto>();
            decimal totalWeightedScore = 0;
            decimal totalWeight = 0;
            int gradedCount = 0;

            foreach (var milestone in milestones)
            {
                var evaluation = evaluations.FirstOrDefault(e => e.MilestoneDefId == milestone.MilestoneId);
                var weight = milestone.Weight ?? 0;
                var grade = evaluation?.Score ?? 0;
                var weightedScore = (grade * weight) / 100;

                if (evaluation != null)
                {
                    totalWeightedScore += weightedScore;
                    totalWeight += weight;
                    gradedCount++;
                }

                contributions.Add(new MilestoneGradeContributionDto
                {
                    MilestoneTitle = milestone.Title,
                    Weight = weight,
                    Grade = evaluation?.Score,
                    WeightedScore = weightedScore,
                    IsGraded = evaluation != null
                });
            }

            decimal? overallGrade = totalWeight > 0 ? (totalWeightedScore / totalWeight) * 100 : null;
            bool isComplete = gradedCount == milestones.Count && milestones.Count > 0;

            var response = new ProjectOverallGradeDto
            {
                ProjectId = projectId,
                ProjectTitle = project.Title,
                OverallGrade = overallGrade,
                CalculationMethod = "WeightedAverage",
                MilestoneContributions = contributions,
                IsComplete = isComplete,
                TotalMilestones = milestones.Count,
                GradedMilestones = gradedCount
            };

            return new ResultModel<ProjectOverallGradeDto>
            {
                IsSuccess = true,
                Message = "Overall grade calculated successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating overall grade");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error calculating grade: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    // Helper method to build project grade DTO
    private async Task<StudentProjectGradeDto> BuildProjectGradeDto(BusinessObjects.Models.Project project)
    {
        // Get all milestones for project
        var milestones = await _context.ProjectMilestones
            .Where(m => m.ProjectId == project.ProjectId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        // Get all evaluations
        var evaluations = await _context.MilestoneEvaluations
            .Include(e => e.Instructor)
            .Where(e => e.ProjectId == project.ProjectId)
            .ToListAsync();

        // Get submissions
        var submissions = await _context.MilestoneSubmissions
            .Where(s => s.ProjectId == project.ProjectId)
            .ToListAsync();

        var milestoneGrades = new List<StudentMilestoneGradeDto>();
        decimal totalWeightedScore = 0;
        decimal totalWeight = 0;
        var breakdownScores = new Dictionary<string, decimal>();

        foreach (var milestone in milestones)
        {
            var evaluation = evaluations.FirstOrDefault(e => e.MilestoneDefId == milestone.MilestoneId);
            var submission = submissions.FirstOrDefault(s => s.MilestoneDefId == milestone.MilestoneId);

            var status = evaluation != null ? "Graded" : 
                         submission != null ? "Submitted" : "NotSubmitted";

            var milestoneGrade = new StudentMilestoneGradeDto
            {
                MilestoneId = milestone.MilestoneId,
                MilestoneTitle = milestone.Title,
                Weight = milestone.Weight,
                Grade = evaluation?.Score,
                Feedback = evaluation?.Feedback,
                GradedAt = evaluation?.EvaluatedAt,
                GradedBy = evaluation?.Instructor?.FullName,
                Status = status
            };

            milestoneGrades.Add(milestoneGrade);

            if (evaluation != null && milestone.Weight.HasValue)
            {
                var weightedScore = (evaluation.Score * milestone.Weight.Value) / 100;
                totalWeightedScore += weightedScore;
                totalWeight += milestone.Weight.Value;
                breakdownScores[$"milestone{milestone.MilestoneId}"] = weightedScore;
            }
        }

        decimal? overallGrade = totalWeight > 0 ? (totalWeightedScore / totalWeight) * 100 : null;
        decimal? projectedGrade = totalWeight < 100 && totalWeight > 0
            ? (totalWeightedScore / totalWeight) * 100
            : overallGrade;

        return new StudentProjectGradeDto
        {
            ProjectId = project.ProjectId,
            ProjectTitle = project.Title,
            ClassId = project.Group?.ClassId ?? 0,
            ClassName = project.Group?.Class?.ClassName,
            SemesterName = project.Group?.Class?.Semester?.Name,
            GroupId = project.GroupId ?? 0,
            GroupName = project.Group?.GroupName,
            ProjectStatus = DetermineProjectStatus(milestoneGrades, project.Status),
            OverallGrade = overallGrade,
            Milestones = milestoneGrades,
            GradeBreakdown = new WeightedGradeBreakdownDto
            {
                MilestoneScores = breakdownScores,
                TotalWeightedScore = totalWeightedScore,
                TotalWeight = totalWeight,
                ProjectedFinalGrade = projectedGrade
            }
        };
    }

    private string DetermineProjectStatus(List<StudentMilestoneGradeDto> milestones, string? projectStatus)
    {
        if (projectStatus == "Rejected")
            return "Failed";

        var allGraded = milestones.All(m => m.Status == "Graded");
        if (allGraded && milestones.Any())
        {
            var avgGrade = milestones.Where(m => m.Grade.HasValue).Average(m => m.Grade);
            return avgGrade >= 50 ? "Completed" : "Failed";
        }

        return "InProgress";
    }
}
