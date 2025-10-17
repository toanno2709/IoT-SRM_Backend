using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.ProjectRepo;

public interface IProjectRepository : IGenericRepository<Project>
{
    Task<List<Project>> GetProjectsByClassAsync(int classId);
}

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    public ProjectRepository(IOTShowroomContext context) : base(context)
    {
    }

    public async Task<List<Project>> GetProjectsByClassAsync(int classId)
    {
        return await _context.Projects
            .Include(p => p.Leader)
            .Include(p => p.ProjectMembers).ThenInclude(pm => pm.User)
            .Where(p => p.ClassId == classId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}





