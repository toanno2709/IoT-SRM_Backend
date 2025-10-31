using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
namespace AppBackend.Repositories.Repositories.ProjectMilestoneRepo;

public interface IProjectMilestoneRepository : IGenericRepository<ProjectMilestone>
{
    Task<List<ProjectMilestone>> GetByProjectAsync(int projectId);
    Task<decimal> GetTotalWeightByProjectAsync(int projectId, int? excludeMilestoneId = null);
}

public class ProjectMilestoneRepository : GenericRepository<ProjectMilestone>, IProjectMilestoneRepository
{
    public ProjectMilestoneRepository(IotShowroomContext context) : base(context)
    {
    }

    public async Task<List<ProjectMilestone>> GetByProjectAsync(int projectId)
    {
        return await _context.ProjectMilestones
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.DueDate)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalWeightByProjectAsync(int projectId, int? excludeMilestoneId = null)
    {
        var query = _context.ProjectMilestones
            .Where(m => m.ProjectId == projectId)
            .AsQueryable();

        if (excludeMilestoneId.HasValue)
        {
            query = query.Where(m => m.MilestoneId != excludeMilestoneId.Value);
        }

        var sum = await query
            .Select(m => m.Weight ?? 0)
            .SumAsync();
        return sum;
    }
}


