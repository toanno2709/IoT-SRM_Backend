using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.ProjectMilestoneRepo;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.ProjectMilestone;

public interface IProjectMilestoneService
{
    Task<ResultModel<List<ProjectMilestoneResponseDto>>> GetByProjectAsync(int projectId);
    Task<ResultModel<ProjectMilestoneResponseDto>> CreateAsync(ProjectMilestoneCreateRequestDto request);
    Task<ResultModel<ProjectMilestoneResponseDto>> UpdateAsync(ProjectMilestoneUpdateRequestDto request);
    Task<ResultModel<bool>> DeleteAsync(int milestoneId);
}

public class ProjectMilestoneService : IProjectMilestoneService
{
    private readonly IProjectMilestoneRepository _repository;

    public ProjectMilestoneService(IProjectMilestoneRepository repository)
    {
        _repository = repository;
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
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        };
    }
}


