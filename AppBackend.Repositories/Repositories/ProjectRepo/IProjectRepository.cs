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
            .Include(p => p.Group)
                .ThenInclude(g => g.Leader)
            .Include(p => p.Group.GroupMembers)
                .ThenInclude(gm => gm.User)
            .Where(p => p.Group.ClassId == classId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}





