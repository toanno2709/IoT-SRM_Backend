using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.ProjectMilestoneRepo;

public interface IProjectMilestoneRepository : IGenericRepository<ProjectMilestone>
{
    Task<List<ProjectMilestone>> GetByProjectAsync(int projectId);
    Task<ProjectMilestone?> GetWithSubmissionsAsync(int milestoneId);
}

public class ProjectMilestoneRepository : GenericRepository<ProjectMilestone>, IProjectMilestoneRepository
{
    public ProjectMilestoneRepository(IOTShowroomContext context) : base(context)
    {
    }

    public async Task<List<ProjectMilestone>> GetByProjectAsync(int projectId)
    {
        return await _context.ProjectMilestones
            .Include(m => m.MilestoneSubmissions)
            .Include(m => m.MilestoneEvaluations)
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.DueDate)
            .ToListAsync();
    }

    public async Task<ProjectMilestone?> GetWithSubmissionsAsync(int milestoneId)
    {
        return await _context.ProjectMilestones
            .Include(m => m.Project)
            .Include(m => m.MilestoneSubmissions)
                .ThenInclude(ms => ms.SubmissionFiles)
            .Include(m => m.MilestoneEvaluations)
            .FirstOrDefaultAsync(m => m.MilestoneId == milestoneId);
    }
}


