using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AppBackend.Services.Services.MilestoneWarning;

public class MilestoneWarningService : IMilestoneWarningService
{
    private readonly IotShowroomContext _context;
    private readonly ILogger<MilestoneWarningService> _logger;
    private const decimal COMPLETE_WEIGHT_PERCENTAGE = 100m;

    public MilestoneWarningService(
        IotShowroomContext context,
        ILogger<MilestoneWarningService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<MilestoneWarningResultDto>> CheckAndSendMilestoneWarningsAsync()
    {
        try
        {
            _logger.LogInformation("=== Starting Weekly Milestone Weight Check ===");

            var result = new MilestoneWarningResultDto
            {
                CheckedAt = DateTime.UtcNow,
                ClassWarnings = new List<ClassWarningDto>()
            };

            // Get all active classes with their projects and milestones
            var classes = await _context.Classes
                .Include(c => c.Instructor)
                .Include(c => c.Groups)
                    .ThenInclude(g => g.Projects)
                        .ThenInclude(p => p.ProjectMilestones)
                .Where(c => c.InstructorId != null)
                .ToListAsync();

            result.TotalClassesChecked = classes.Count;
            var instructorsNotified = new HashSet<int>();

            foreach (var classEntity in classes)
            {
                var projectsWithIncompleteWeights = new List<ProjectMilestoneWarningDto>();

                // Check all projects in this class
                foreach (var group in classEntity.Groups)
                {
                    foreach (var project in group.Projects)
                    {
                        var milestones = project.ProjectMilestones.ToList();
                        
                        if (milestones.Count == 0)
                            continue;

                        var totalWeight = milestones.Sum(m => m.Weight ?? 0);

                        // Check if weight is not 100%
                        if (Math.Abs(totalWeight - COMPLETE_WEIGHT_PERCENTAGE) > 0.01m)
                        {
                            projectsWithIncompleteWeights.Add(new ProjectMilestoneWarningDto
                            {
                                ProjectId = project.ProjectId,
                                ProjectTitle = project.Title,
                                GroupId = group.GroupId,
                                GroupName = group.GroupName,
                                TotalWeightPercentage = totalWeight,
                                MissingWeightPercentage = COMPLETE_WEIGHT_PERCENTAGE - totalWeight,
                                TotalMilestones = milestones.Count,
                                Milestones = milestones.Select(m => new MilestoneWeightDto
                                {
                                    MilestoneId = m.MilestoneId,
                                    Title = m.Title,
                                    MilestoneOrder = m.MilestoneId,
                                    WeightPercentage = m.Weight ?? 0,
                                    DueDate = m.DueDate,
                                    IsCompleted = false // TODO: Check if milestone is completed based on submissions
                                }).OrderBy(m => m.MilestoneId).ToList()
                            });
                        }
                    }
                }

                // If there are projects with incomplete weights, send notification to instructor
                if (projectsWithIncompleteWeights.Count > 0 && classEntity.InstructorId.HasValue)
                {
                    var instructorId = classEntity.InstructorId.Value;
                    
                    // Prepare data payload: include class id and affected projects with projectId and groupId
                    var dataPayload = new
                    {
                        classId = classEntity.ClassId,
                        projects = projectsWithIncompleteWeights.Select(p => new { projectId = p.ProjectId, groupId = p.GroupId }).ToList()
                    };

                    // Create notification
                    var notification = new BusinessObjects.Models.Notification
                    {
                        UserId = instructorId,
                        Title = $"?? Milestone Weight Warning - {classEntity.ClassName}",
                        Message = $"Class '{classEntity.ClassName}' has {projectsWithIncompleteWeights.Count} project(s) with incomplete milestone weights (not 100%). " +
                                 $"Projects: {string.Join(", ", projectsWithIncompleteWeights.Take(5).Select(p => p.ProjectTitle))}",
                        Type = "milestone_weight_warning",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        Data = JsonSerializer.Serialize(dataPayload)
                    };
                    _context.Notifications.Add(notification);

                    instructorsNotified.Add(instructorId);

                    // Add to result
                    result.ClassWarnings.Add(new ClassWarningDto
                    {
                        ClassId = classEntity.ClassId,
                        ClassName = classEntity.ClassName,
                        InstructorId = instructorId,
                        InstructorName = classEntity.Instructor?.FullName,
                        ProjectsWithIncompleteWeights = projectsWithIncompleteWeights.Count,
                        ProjectNames = projectsWithIncompleteWeights.Select(p => p.ProjectTitle ?? "Unknown").ToList()
                    });

                    result.TotalProjectsWithIncompleteWeights += projectsWithIncompleteWeights.Count;

                    _logger.LogInformation(
                        "Sent warning to instructor {InstructorId} for class {ClassId}: {ProjectCount} projects with incomplete weights",
                        instructorId, classEntity.ClassId, projectsWithIncompleteWeights.Count);
                }
            }

            await _context.SaveChangesAsync();

            result.TotalInstructorsNotified = instructorsNotified.Count;

            _logger.LogInformation(
                "=== Milestone Weight Check Complete === Classes: {Classes}, Instructors Notified: {Instructors}, Projects with Issues: {Projects}",
                result.TotalClassesChecked, result.TotalInstructorsNotified, result.TotalProjectsWithIncompleteWeights);

            return new ResultModel<MilestoneWarningResultDto>
            {
                IsSuccess = true,
                Message = $"Checked {result.TotalClassesChecked} classes. Notified {result.TotalInstructorsNotified} instructors about {result.TotalProjectsWithIncompleteWeights} projects with incomplete milestone weights.",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking milestone warnings");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error checking milestone warnings: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<List<ProjectMilestoneWarningDto>>> GetProjectsWithIncompleteMilestonesAsync(int classId)
    {
        try
        {
            _logger.LogInformation("Getting projects with incomplete milestones for class {ClassId}", classId);

            var projects = await _context.Projects
                .Where(p => p.Group!.ClassId == classId)
                .Include(p => p.Group)
                .Include(p => p.ProjectMilestones)
                .ToListAsync();

            var result = new List<ProjectMilestoneWarningDto>();

            foreach (var project in projects)
            {
                var milestones = project.ProjectMilestones.ToList();
                
                if (milestones.Count == 0)
                    continue;

                var totalWeight = milestones.Sum(m => m.Weight ?? 0);

                // Check if weight is not 100%
                if (Math.Abs(totalWeight - COMPLETE_WEIGHT_PERCENTAGE) > 0.01m)
                {
                    result.Add(new ProjectMilestoneWarningDto
                    {
                        ProjectId = project.ProjectId,
                        ProjectTitle = project.Title,
                        GroupId = project.Group!.GroupId,
                        GroupName = project.Group.GroupName,
                        TotalWeightPercentage = totalWeight,
                        MissingWeightPercentage = COMPLETE_WEIGHT_PERCENTAGE - totalWeight,
                        TotalMilestones = milestones.Count,
                        Milestones = milestones.Select(m => new MilestoneWeightDto
                        {
                            MilestoneId = m.MilestoneId,
                            Title = m.Title,
                            MilestoneOrder = m.MilestoneId,
                            WeightPercentage = m.Weight ?? 0,
                            DueDate = m.DueDate,
                            IsCompleted = false
                        }).OrderBy(m => m.MilestoneId).ToList()
                    });
                }
            }

            return new ResultModel<List<ProjectMilestoneWarningDto>>
            {
                IsSuccess = true,
                Message = $"Found {result.Count} project(s) with incomplete milestone weights",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects with incomplete milestones");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting projects: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ProjectMilestoneWeightDto>> GetProjectMilestoneWeightsAsync(int projectId)
    {
        try
        {
            _logger.LogInformation("Getting milestone weights for project {ProjectId}", projectId);

            var project = await _context.Projects
                .Where(p => p.ProjectId == projectId)
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                .Include(p => p.ProjectMilestones)
                .FirstOrDefaultAsync();

            if (project == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );
            }

            var milestones = project.ProjectMilestones.OrderBy(m => m.MilestoneId).ToList();
            var totalWeight = milestones.Sum(m => m.Weight ?? 0);
            var isComplete = Math.Abs(totalWeight - COMPLETE_WEIGHT_PERCENTAGE) < 0.01m;
            var missingWeight = COMPLETE_WEIGHT_PERCENTAGE - totalWeight;

            var result = new ProjectMilestoneWeightDto
            {
                ProjectId = project.ProjectId,
                ProjectTitle = project.Title,
                ClassId = project.Group!.ClassId,
                ClassName = project.Group.Class?.ClassName,
                TotalWeightPercentage = totalWeight,
                IsComplete = isComplete,
                MissingWeightPercentage = missingWeight,
                TotalMilestones = milestones.Count,
                Milestones = milestones.Select(m => new MilestoneWeightDto
                {
                    MilestoneId = m.MilestoneId,
                    Title = m.Title,
                    MilestoneOrder = m.MilestoneId,
                    WeightPercentage = m.Weight ?? 0,
                    DueDate = m.DueDate,
                    IsCompleted = false
                }).ToList()
            };

            if (!isComplete)
            {
                result.WarningMessage = missingWeight > 0 
                    ? $"?? Milestone weights total {totalWeight:F1}%. Missing {missingWeight:F1}% to reach 100%."
                    : $"?? Milestone weights total {totalWeight:F1}%. Exceeds 100% by {Math.Abs(missingWeight):F1}%.";
            }

            return new ResultModel<ProjectMilestoneWeightDto>
            {
                IsSuccess = true,
                Message = isComplete ? "Milestone weights are complete (100%)" : result.WarningMessage!,
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
            _logger.LogError(ex, "Error getting project milestone weights");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting milestone weights: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }
}
