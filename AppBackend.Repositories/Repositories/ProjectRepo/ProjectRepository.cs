using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
namespace AppBackend.Repositories.Repositories.ProjectRepo;

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    public ProjectRepository(IotShowroomContext context) : base(context)
    {
    }

    public async Task<List<Project>> GetProjectsByClassAsync(int classId)
    {
        // Project gi? thu?c Group, Group thu?c Class
        return await _context.Projects
            .Include(p => p.Group)
                .ThenInclude(g => g!.Leader)
            .Include(p => p.Group)
                .ThenInclude(g => g!.GroupMembers)
                    .ThenInclude(gm => gm.User)
            .Include(p => p.Group)
                .ThenInclude(g => g!.Class)
            .Where(p => p.Group != null && p.Group.ClassId == classId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Project>> GetProjectsByGroupAsync(int groupId)
    {
        return await _context.Projects
            .Include(p => p.Group)
                .ThenInclude(g => g!.Leader)
            .Include(p => p.Group)
                .ThenInclude(g => g!.GroupMembers)
                    .ThenInclude(gm => gm.User)
            .Where(p => p.GroupId == groupId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectWithDetailsAsync(int projectId)
    {
        return await _context.Projects
            .Include(p => p.Group)
                .ThenInclude(g => g!.Leader)
            .Include(p => p.Group)
                .ThenInclude(g => g!.GroupMembers)
                    .ThenInclude(gm => gm.User)
            .Include(p => p.Group)
                .ThenInclude(g => g!.Class)
            .Include(p => p.ProjectMilestones)
            .Include(p => p.Sensors)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);
    }

    public async Task<Project?> GetByIdWithDetailsAsync(int projectId)
    {
        return await _context.Projects
            .Include(p => p.Group)
                .ThenInclude(g => g!.Leader)
            .Include(p => p.Group)
                .ThenInclude(g => g!.GroupMembers)
                    .ThenInclude(gm => gm.User)
            .Include(p => p.Group)
                .ThenInclude(g => g!.Class)
                    .ThenInclude(c => c!.Instructor)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);
    }

    public async Task<List<Project>> GetProjectsBySemesterAsync(int semesterId)
    {
        return await _context.Projects
            .Include(p => p.Group)
                .ThenInclude(g => g!.Class)
                    .ThenInclude(c => c!.Semester)
            .Include(p => p.Group)
                .ThenInclude(g => g!.Leader)
            .Include(p => p.FinalProjectSubmission)
            .Where(p => p.Group != null && p.Group.Class != null && p.Group.Class.SemesterId == semesterId)
            .OrderByDescending(p => p.FinalProjectSubmission != null ? p.FinalProjectSubmission.Grade : 0)
            .ToListAsync();
    }
}
