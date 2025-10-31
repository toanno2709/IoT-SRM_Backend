using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppBackend.BusinessObjects.Data;
namespace AppBackend.Repositories.Repositories.GroupRepo
{
    public class GroupRepository : IGroupRepository
    {
        private readonly IotShowroomContext _context;

        public GroupRepository(IotShowroomContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Group>> GetAllAsync()
        {
            return await _context.Groups
                .Include(g => g.Leader)
                .Include(g => g.Class)
                .Include(g => g.GroupMembers).ThenInclude(m => m.User)
                .ToListAsync();
        }

        public async Task<Group?> GetByIdAsync(int groupId)
        {
            return await _context.Groups
                .Include(g => g.Leader)
                .Include(g => g.Class)
                .Include(g => g.GroupMembers).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);
        }

        public async Task<Group> CreateGroupAsync(Group group, int leaderId)
        {
            // Ki?m tra leader dã có nhóm trong class này chua
            var alreadyInGroup = await CheckStudentInClassGroupAsync(group.ClassId, leaderId);
            if (alreadyInGroup)
                throw new InvalidOperationException("Leader dã thu?c m?t nhóm khác trong cùng l?p.");

            group.LeaderId = leaderId;
            group.CreatedAt = DateTime.UtcNow;

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // Thêm leader vào GroupMembers
            var leaderMember = new GroupMember
            {
                GroupId = group.GroupId,
                UserId = leaderId,
                RoleInGroup = "Leader",
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(leaderMember);
            await _context.SaveChangesAsync();

            return group;
        }

        public async Task<bool> AddMemberAsync(int groupId, int userId, string role)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
                throw new KeyNotFoundException("Group not found.");

            // Ki?m tra sinh viên dã thu?c nhóm khác trong cùng class chua
            var alreadyInGroup = await CheckStudentInClassGroupAsync(group.ClassId, userId);
            if (alreadyInGroup)
                throw new InvalidOperationException("Sinh viên dã thu?c m?t nhóm khác trong cùng l?p.");

            var member = new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                RoleInGroup = role,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(member);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CheckStudentInClassGroupAsync(int classId, int userId)
        {
            return await _context.GroupMembers
                .Include(gm => gm.Group)
                .AnyAsync(gm => gm.UserId == userId && gm.Group.ClassId == classId);
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
}
