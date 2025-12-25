using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;

namespace AppBackend.Repositories.Repositories.MilestoneSubmissionRepo;

public interface IMilestoneSubmissionRepository : IGenericRepository<MilestoneSubmission>
{
    // Existing methods
    Task<List<MilestoneSubmission>> GetPendingProposalsByInstructorAsync(int instructorId);
    Task<MilestoneSubmission?> GetSubmissionWithDetailsAsync(int submissionId);
    
    // New methods for student submissions
    Task<MilestoneSubmission?> GetSubmissionByProjectAndMilestoneAsync(int projectId, int milestoneDefId);
    Task<List<SubmissionFile>> GetSubmissionFilesAsync(int submissionId, int? versionNo = null);
    Task<int> GetNextVersionNumberAsync(int submissionId);
    Task<bool> CanResubmitAsync(int projectId, int milestoneDefId);
    Task<List<MilestoneSubmission>> GetSubmissionsByProjectAsync(int projectId);
}

public class MilestoneSubmissionRepository : GenericRepository<MilestoneSubmission>, IMilestoneSubmissionRepository
{
    public MilestoneSubmissionRepository(IotShowroomContext context) : base(context)
    {
    }

    /// <summary>
    /// Lấy các topic proposal chờ duyệt (submissions chưa có approval history hoặc đang pending)
    /// </summary>
    public async Task<List<MilestoneSubmission>> GetPendingProposalsByInstructorAsync(int instructorId)
    {
        return await _context.MilestoneSubmissions
            .Include(s => s.Project)
                .ThenInclude(p => p!.Group)
                    .ThenInclude(g => g!.Leader)
            .Include(s => s.Project)
                .ThenInclude(p => p!.Group)
                    .ThenInclude(g => g!.Class)
            .Include(s => s.MilestoneDef)
            .Include(s => s.SubmissionFiles)
            .Include(s => s.ProjectApprovalHistories)
            .Where(s => s.Project.Group!.Class!.InstructorId == instructorId
                && s.MilestoneDef.Title!.Contains("Proposal")
                && !s.ProjectApprovalHistories.Any(h => h.Action == "Approved" || h.Action == "Rejected"))
            .OrderByDescending(s => s.LastSubmittedAt)
            .ToListAsync();
    }

    public async Task<MilestoneSubmission?> GetSubmissionWithDetailsAsync(int submissionId)
    {
        return await _context.MilestoneSubmissions
            .Include(s => s.Project)
                .ThenInclude(p => p!.Group)
                    .ThenInclude(g => g!.GroupMembers) // ✅ FIX: Include GroupMembers for authorization check
            .Include(s => s.Project)
                .ThenInclude(p => p!.Group)
                    .ThenInclude(g => g!.Leader)
            .Include(s => s.Project)
                .ThenInclude(p => p!.Group)
                    .ThenInclude(g => g!.Class)
            .Include(s => s.MilestoneDef)
            .Include(s => s.SubmissionFiles)
            .Include(s => s.ProjectApprovalHistories)
            .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);
    }

    /// <summary>
    /// Get submission by project and milestone
    /// </summary>
    public async Task<MilestoneSubmission?> GetSubmissionByProjectAndMilestoneAsync(int projectId, int milestoneDefId)
    {
        return await _context.MilestoneSubmissions
            .Include(s => s.Project)
            .Include(s => s.MilestoneDef)
            .Include(s => s.SubmissionFiles)
                .ThenInclude(f => f.UploadedByNavigation)
            .Include(s => s.ProjectApprovalHistories)
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.MilestoneDefId == milestoneDefId);
    }

    /// <summary>
    /// Get all files for a submission (optionally filtered by version)
    /// </summary>
    public async Task<List<SubmissionFile>> GetSubmissionFilesAsync(int submissionId, int? versionNo = null)
    {
        var query = _context.SubmissionFiles
            .Include(f => f.UploadedByNavigation)
            .Where(f => f.SubmissionId == submissionId);

        if (versionNo.HasValue)
        {
            query = query.Where(f => f.VersionNo == versionNo.Value);
        }

        return await query
            .OrderBy(f => f.VersionNo)
            .ThenBy(f => f.UploadedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get next version number for a submission
    /// </summary>
    public async Task<int> GetNextVersionNumberAsync(int submissionId)
    {
        var maxVersion = await _context.SubmissionFiles
            .Where(f => f.SubmissionId == submissionId)
            .MaxAsync(f => (int?)f.VersionNo) ?? 0;

        return maxVersion + 1;
    }

    /// <summary>
    /// Check if can resubmit (before deadline)
    /// </summary>
    public async Task<bool> CanResubmitAsync(int projectId, int milestoneDefId)
    {
        var milestone = await _context.ProjectMilestones
            .FirstOrDefaultAsync(m => m.MilestoneId == milestoneDefId);

        if (milestone == null || !milestone.DueDate.HasValue)
            return false;

        // Convert DateOnly to DateTime for comparison
        var dueDateTime = milestone.DueDate.Value.ToDateTime(TimeOnly.MaxValue);
        return DateTime.UtcNow < dueDateTime;
    }

    /// <summary>
    /// Get all submissions for a project
    /// </summary>
    public async Task<List<MilestoneSubmission>> GetSubmissionsByProjectAsync(int projectId)
    {
        return await _context.MilestoneSubmissions
            .Include(s => s.MilestoneDef)
            .Include(s => s.SubmissionFiles)
                .ThenInclude(f => f.UploadedByNavigation)
            .Include(s => s.ProjectApprovalHistories)
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.MilestoneDef.CreatedAt)
            .ToListAsync();
    }
}


