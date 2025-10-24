using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.GroupRepo;

public interface IGroupRepository : IGenericRepository<Group>
{
    Task<List<Group>> GetGroupsByClassAsync(int classId);
    Task<Group?> GetGroupWithDetailsAsync(int groupId);
}

public class GroupRepository : GenericRepository<Group>, IGroupRepository
{
    public GroupRepository(IOTShowroomContext context) : base(context)
    {
    }

    public async Task<List<Group>> GetGroupsByClassAsync(int classId)
    {
        return await _context.Groups
            .Include(g => g.Leader)
            .Include(g => g.Class)
            .Include(g => g.GroupMembers).ThenInclude(gm => gm.User)
            .Include(g => g.Projects)
            .Where(g => g.ClassId == classId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<Group?> GetGroupWithDetailsAsync(int groupId)
    {
        return await _context.Groups
            .Include(g => g.Leader)
            .Include(g => g.Class)
            .Include(g => g.GroupMembers).ThenInclude(gm => gm.User)
            .Include(g => g.Projects)
            .FirstOrDefaultAsync(g => g.GroupId == groupId);
    }
}



