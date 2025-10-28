using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
namespace AppBackend.Repositories.Repositories.GroupMemberRepo;

public interface IGroupMemberRepository : IGenericRepository<GroupMember>
{
    Task<GroupMember?> GetMemberAsync(int groupId, int userId);
    Task<List<GroupMember>> GetGroupMembersAsync(int groupId);
    Task<bool> IsMemberInGroupAsync(int groupId, int userId);
    Task<int> GetGroupMemberCountAsync(int groupId);
}

public class GroupMemberRepository : GenericRepository<GroupMember>, IGroupMemberRepository
{
    public GroupMemberRepository(IotShowroomContext context) : base(context)
    {
    }

    public async Task<GroupMember?> GetMemberAsync(int groupId, int userId)
    {
        return await _context.GroupMembers
            .Include(gm => gm.User)
            .Include(gm => gm.Group)
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
    }

    public async Task<List<GroupMember>> GetGroupMembersAsync(int groupId)
    {
        return await _context.GroupMembers
            .Include(gm => gm.User)
            .Where(gm => gm.GroupId == groupId)
            .OrderBy(gm => gm.JoinedAt)
            .ToListAsync();
    }

    public async Task<bool> IsMemberInGroupAsync(int groupId, int userId)
    {
        return await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
    }

    public async Task<int> GetGroupMemberCountAsync(int groupId)
    {
        return await _context.GroupMembers
            .CountAsync(gm => gm.GroupId == groupId);
    }
}


