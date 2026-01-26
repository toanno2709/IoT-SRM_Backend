using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Repositories.Repositories.FinalProjectRepo;

public class FinalProjectRepository : GenericRepository<FinalProjectSubmission>, IFinalProjectRepository
{
    private readonly ILogger<FinalProjectRepository> _logger;

    public FinalProjectRepository(IotShowroomContext context, ILogger<FinalProjectRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<FinalProjectSubmission?> GetByProjectIdAsync(int projectId)
    {
        return await _context.FinalProjectSubmissions
            .FirstOrDefaultAsync(f => f.ProjectId == projectId);
    }

    public async Task<FinalProjectSubmission?> GetByProjectIdWithDetailsAsync(int projectId)
    {
        _logger.LogInformation("=== GetByProjectIdWithDetailsAsync START === ProjectId: {ProjectId}", projectId);
        
        var submission = await _context.FinalProjectSubmissions
            .Include(f => f.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
            .Include(f => f.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g!.Class)
                        .ThenInclude(c => c!.ClassGraders) // ? Include ClassGraders for authorization check
            .Include(f => f.SubmittedByNavigation)
            .Include(f => f.GradedByNavigation)
            .FirstOrDefaultAsync(f => f.ProjectId == projectId);

        if (submission == null)
        {
            _logger.LogWarning("No submission found for ProjectId: {ProjectId}", projectId);
            return null;
        }

        _logger.LogInformation("Found submission {SubmissionId}", submission.FinalSubmissionId);
        
        // Log navigation properties
        _logger.LogInformation("Project is null: {IsNull}", submission.Project == null);
        if (submission.Project != null)
        {
            _logger.LogInformation("Project {ProjectId} loaded. GroupId: {GroupId}", 
                submission.Project.ProjectId, submission.Project.GroupId);
            
            _logger.LogInformation("Project.Group is null: {IsNull}", submission.Project.Group == null);
            if (submission.Project.Group != null)
            {
                _logger.LogInformation("Group {GroupId} loaded. ClassId: {ClassId}", 
                    submission.Project.Group.GroupId, 
                    submission.Project.Group.ClassId);
                
                _logger.LogInformation("Group.Class is null: {IsNull}", submission.Project.Group.Class == null);
                if (submission.Project.Group.Class != null)
                {
                    _logger.LogInformation("Class {ClassId} loaded. InstructorId: {InstructorId}", 
                        submission.Project.Group.Class.ClassId, 
                        submission.Project.Group.Class.InstructorId);
                }
                else
                {
                    _logger.LogError("Class navigation is NULL for Group {GroupId} with ClassId {ClassId}", 
                        submission.Project.Group.GroupId, 
                        submission.Project.Group.ClassId);
                }
                
                _logger.LogInformation("Group.GroupMembers is null: {IsNull}, Count: {Count}", 
                    submission.Project.Group.GroupMembers == null, 
                    submission.Project.Group.GroupMembers?.Count ?? 0);
            }
            else
            {
                _logger.LogError("Group navigation is NULL for Project {ProjectId} with GroupId {GroupId}", 
                    submission.Project.ProjectId, 
                    submission.Project.GroupId);
            }
        }

        _logger.LogInformation("=== GetByProjectIdWithDetailsAsync END ===");
        return submission;
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
