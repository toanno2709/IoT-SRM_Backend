using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.ProjectMilestoneRepo;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.BusinessObjects.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.ProjectMilestone;

public class ProjectMilestoneService : IProjectMilestoneService
{
    private readonly IProjectMilestoneRepository _repository;
    private readonly IotShowroomContext _context;
    private readonly ILogger<ProjectMilestoneService> _logger;

    public ProjectMilestoneService(
        IProjectMilestoneRepository repository, 
        IotShowroomContext context,
        ILogger<ProjectMilestoneService> logger)
    {
        _repository = repository;
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<List<ProjectMilestoneResponseDto>>> GetByProjectAsync(int projectId)
    {
        var items = await _repository.GetByProjectAsync(projectId);
        var dtos = items.Select(MapToResponse).ToList();
        return new ResultModel<List<ProjectMilestoneResponseDto>>
        {
            IsSuccess = true,
            Message = "Milestones retrieved successfully",
            Data = dtos
        };
    }

    public async Task<ResultModel<ProjectMilestoneResponseDto>> CreateAsync(ProjectMilestoneCreateRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return new ResultModel<ProjectMilestoneResponseDto> { IsSuccess = false, Message = "Title is required" };
        }

        if (request.Weight is < 0 or > 100)
        {
            return new ResultModel<ProjectMilestoneResponseDto> { IsSuccess = false, Message = "Weight must be between 0 and 100" };
        }

        var currentTotal = await _repository.GetTotalWeightByProjectAsync(request.ProjectId);
        var newTotal = currentTotal + (request.Weight ?? 0);
        if (newTotal > 100)
        {
            return new ResultModel<ProjectMilestoneResponseDto> { IsSuccess = false, Message = $"Total weight would be {newTotal}, exceeds 100" };
        }

        var entity = new BusinessObjects.Models.ProjectMilestone
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Status = "Planned",
            Weight = request.Weight,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return new ResultModel<ProjectMilestoneResponseDto>
        {
            IsSuccess = true,
            Message = "Milestone created",
            Data = MapToResponse(entity)
        };
    }

    public async Task<ResultModel<ProjectMilestoneResponseDto>> UpdateAsync(ProjectMilestoneUpdateRequestDto request)
    {
        var entity = await _repository.GetByIdAsync(request.MilestoneId);
        if (entity == null)
        {
            return new ResultModel<ProjectMilestoneResponseDto> { IsSuccess = false, Message = "Milestone not found" };
        }

        if (request.Weight is < 0 or > 100)
        {
            return new ResultModel<ProjectMilestoneResponseDto> { IsSuccess = false, Message = "Weight must be between 0 and 100" };
        }

        if (request.Weight.HasValue)
        {
            var currentTotal = await _repository.GetTotalWeightByProjectAsync(entity.ProjectId, excludeMilestoneId: entity.MilestoneId);
            var newTotal = currentTotal + request.Weight.Value;
            if (newTotal > 100)
            {
                return new ResultModel<ProjectMilestoneResponseDto> { IsSuccess = false, Message = $"Total weight would be {newTotal}, exceeds 100" };
            }
        }

        if (request.Title != null) entity.Title = request.Title;
        if (request.Description != null) entity.Description = request.Description;
        if (request.DueDate != null) entity.DueDate = request.DueDate;
        if (request.Status != null) entity.Status = request.Status;
        if (request.Weight.HasValue) entity.Weight = request.Weight.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        return new ResultModel<ProjectMilestoneResponseDto>
        {
            IsSuccess = true,
            Message = "Milestone updated",
            Data = MapToResponse((BusinessObjects.Models.ProjectMilestone)entity)
        };
    }

    public async Task<ResultModel<bool>> DeleteAsync(int milestoneId)
    {
        var entity = await _repository.GetByIdAsync(milestoneId);
        if (entity == null)
        {
            return new ResultModel<bool> { IsSuccess = false, Message = "Milestone not found", Data = false };
        }
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
        return new ResultModel<bool> { IsSuccess = true, Message = "Milestone deleted", Data = true };
    }

    public async Task<ResultModel<BulkCreateMilestoneResponseDto>> BulkCreateMilestoneForClassAsync(BulkCreateMilestoneRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting bulk milestone creation for class {ClassId}", request.ClassId);

            // Validate class exists
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.ClassId == request.ClassId);

            if (classEntity == null)
            {
                return new ResultModel<BulkCreateMilestoneResponseDto>
                {
                    IsSuccess = false,
                    Message = "Class not found",
                    StatusCode = 404
                };
            }

            // Get all groups in the class with their projects
            var groups = await _context.Groups
                .Where(g => g.ClassId == request.ClassId)
                .Include(g => g.Projects)
                .ToListAsync();

            var response = new BulkCreateMilestoneResponseDto
            {
                ClassId = request.ClassId,
                ClassName = classEntity.ClassName,
                CreatedMilestones = new List<BulkMilestoneCreationDetailDto>(),
                Warnings = new List<string>(),
                Errors = new List<string>()
            };

            if (groups.Count == 0)
            {
                response.Warnings.Add("No groups found in this class");
                return new ResultModel<BulkCreateMilestoneResponseDto>
                {
                    IsSuccess = true,
                    Message = "No groups found in class",
                    Data = response,
                    StatusCode = 200
                };
            }

            // Process each group's projects
            foreach (var group in groups)
            {
                // Get approved projects only
                var approvedProjects = group.Projects
                    .Where(p => p.Status != null && p.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (approvedProjects.Count == 0)
                {
                    response.Warnings.Add($"Group '{group.GroupName}' has no approved projects");
                    continue;
                }

                response.TotalProjectsProcessed += approvedProjects.Count;

                // Create milestone for each approved project
                foreach (var project in approvedProjects)
                {
                    try
                    {
                        // Check current total weight
                        var currentTotalWeight = await _repository.GetTotalWeightByProjectAsync(project.ProjectId);
                        var newTotal = currentTotalWeight + request.Weight;

                        var detail = new BulkMilestoneCreationDetailDto
                        {
                            ProjectId = project.ProjectId,
                            ProjectTitle = project.Title,
                            GroupId = group.GroupId,
                            GroupName = group.GroupName,
                            MilestoneTitle = request.Title,
                            Weight = request.Weight
                        };

                        // Warning if weight exceeds 100%
                        if (newTotal > 100)
                        {
                            detail.IsSuccess = false;
                            detail.Message = $"Cannot add milestone. Current weight: {currentTotalWeight}%, New total would be: {newTotal}% (exceeds 100%)";
                            response.Warnings.Add($"Project '{project.Title}' (ID: {project.ProjectId}): {detail.Message}");
                            response.CreatedMilestones.Add(detail);
                            continue;
                        }

                        // Create milestone
                        var milestone = new BusinessObjects.Models.ProjectMilestone
                        {
                            ProjectId = project.ProjectId,
                            Title = request.Title,
                            Description = request.Description,
                            DueDate = request.DueDate,
                            Status = "Planned",
                            Weight = request.Weight,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _repository.AddAsync(milestone);
                        await _repository.SaveChangesAsync();

                        detail.MilestoneId = milestone.MilestoneId;
                        detail.IsSuccess = true;
                        detail.Message = "Milestone created successfully";
                        response.CreatedMilestones.Add(detail);
                        response.TotalMilestonesCreated++;

                        _logger.LogInformation(
                            "Created milestone {MilestoneId} for project {ProjectId} in group {GroupName}",
                            milestone.MilestoneId, project.ProjectId, group.GroupName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating milestone for project {ProjectId}", project.ProjectId);
                        response.Errors.Add($"Project '{project.Title}' (ID: {project.ProjectId}): {ex.Message}");
                        
                        response.CreatedMilestones.Add(new BulkMilestoneCreationDetailDto
                        {
                            ProjectId = project.ProjectId,
                            ProjectTitle = project.Title,
                            GroupId = group.GroupId,
                            GroupName = group.GroupName,
                            MilestoneTitle = request.Title,
                            Weight = request.Weight,
                            IsSuccess = false,
                            Message = $"Error: {ex.Message}"
                        });
                    }
                }
            }

            var successMessage = response.TotalMilestonesCreated > 0
                ? $"Successfully created {response.TotalMilestonesCreated} milestone(s) for {response.TotalProjectsProcessed} approved project(s) in class '{classEntity.ClassName}'"
                : $"No milestones were created. Processed {response.TotalProjectsProcessed} approved project(s).";

            if (response.Warnings.Count > 0)
            {
                successMessage += $" {response.Warnings.Count} warning(s) occurred.";
            }

            if (response.Errors.Count > 0)
            {
                successMessage += $" {response.Errors.Count} error(s) occurred.";
            }

            _logger.LogInformation(
                "Bulk milestone creation completed. Class: {ClassId}, Projects: {ProjectCount}, Created: {MilestoneCount}, Warnings: {WarningCount}, Errors: {ErrorCount}",
                request.ClassId, response.TotalProjectsProcessed, response.TotalMilestonesCreated, response.Warnings.Count, response.Errors.Count);

            return new ResultModel<BulkCreateMilestoneResponseDto>
            {
                IsSuccess = true,
                Message = successMessage,
                Data = response,
                StatusCode = 200
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk milestone creation for class {ClassId}", request.ClassId);
            return new ResultModel<BulkCreateMilestoneResponseDto>
            {
                IsSuccess = false,
                Message = $"Error creating milestones: {ex.Message}",
                StatusCode = 500
            };
        }
    }

    private static ProjectMilestoneResponseDto MapToResponse(BusinessObjects.Models.ProjectMilestone m)
    {
        return new ProjectMilestoneResponseDto
        {
            MilestoneId = m.MilestoneId,
            ProjectId = m.ProjectId,
            Title = m.Title,
            Description = m.Description,
            DueDate = m.DueDate,
            Status = m.Status,
            Weight = m.Weight,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };
    }
}

