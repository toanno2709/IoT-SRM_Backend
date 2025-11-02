using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.FinalProjectRepo;

public class FinalProjectRepository : GenericRepository<FinalProjectSubmission>, IFinalProjectRepository
{
    public FinalProjectRepository(IotShowroomContext context) : base(context)
    {
    }

    public async Task<FinalProjectSubmission?> GetByProjectIdAsync(int projectId)
    {
        return await _context.FinalProjectSubmissions
            .FirstOrDefaultAsync(f => f.ProjectId == projectId);
    }

    public async Task<FinalProjectSubmission?> GetByProjectIdWithDetailsAsync(int projectId)
    {
        return await _context.FinalProjectSubmissions
            .Include(f => f.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
            .Include(f => f.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g!.Class)
            .Include(f => f.SubmittedByNavigation)
            .Include(f => f.GradedByNavigation)
            .FirstOrDefaultAsync(f => f.ProjectId == projectId);
    }

    public async Task<bool> HasSubmissionAsync(int projectId)
    {
        return await _context.FinalProjectSubmissions
            .AnyAsync(f => f.ProjectId == projectId);
    }

    public async Task<bool> CanUpdateAsync(int projectId)
    {
        // Check if there's a final milestone with deadline
        var project = await _context.Projects
            .Include(p => p.ProjectMilestones)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null) return false;

        // Find final milestone (usually the last one or titled "Final Submission")
        var finalMilestone = project.ProjectMilestones
            .OrderByDescending(m => m.MilestoneId)
            .FirstOrDefault(m => m.Title != null && 
                (m.Title.Contains("Final", StringComparison.OrdinalIgnoreCase) ||
                 m.Title.Contains("Submission", StringComparison.OrdinalIgnoreCase)));

        if (finalMilestone?.DueDate == null) return true; // No deadline set, can always update

        var deadline = finalMilestone.DueDate.Value.ToDateTime(TimeOnly.MaxValue);
        return DateTime.UtcNow <= deadline;
    }

    public async Task<List<FinalProjectSubmission>> GetByClassIdAsync(int classId)
    {
        return await _context.FinalProjectSubmissions
            .Include(f => f.Project)
                .ThenInclude(p => p.Group)
            .Include(f => f.SubmittedByNavigation)
            .Include(f => f.GradedByNavigation)
            .Where(f => f.Project.Group!.ClassId == classId)
            .OrderByDescending(f => f.SubmittedAt)
            .ToListAsync();
    }

    public async Task<List<FinalProjectSubmission>> GetGradedSubmissionsAsync(int classId)
    {
        return await _context.FinalProjectSubmissions
            .Include(f => f.Project)
                .ThenInclude(p => p.Group)
            .Include(f => f.SubmittedByNavigation)
            .Include(f => f.GradedByNavigation)
            .Where(f => f.Project.Group!.ClassId == classId && f.Status == "Graded")
            .OrderByDescending(f => f.GradedAt)
            .ToListAsync();
    }

    public async Task<List<FinalProjectSubmission>> GetUngradedSubmissionsAsync(int classId)
    {
        return await _context.FinalProjectSubmissions
            .Include(f => f.Project)
                .ThenInclude(p => p.Group)
            .Include(f => f.SubmittedByNavigation)
            .Where(f => f.Project.Group!.ClassId == classId && f.Status == "Submitted")
            .OrderBy(f => f.SubmittedAt)
            .ToListAsync();
    }
}
