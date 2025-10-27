using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
namespace AppBackend.Repositories.Repositories.MilestoneSubmissionRepo;

public interface IMilestoneSubmissionRepository : IGenericRepository<MilestoneSubmission>
{
    Task<List<MilestoneSubmission>> GetPendingProposalsByInstructorAsync(int instructorId);
    Task<MilestoneSubmission?> GetSubmissionWithDetailsAsync(int submissionId);
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
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g.Leader)
            .Include(s => s.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g.Class)
            .Include(s => s.MilestoneDef)
            .Include(s => s.SubmissionFiles)
            .Include(s => s.ProjectApprovalHistories)
            .Where(s => s.Project.Group.Class.InstructorId == instructorId
                && s.MilestoneDef.Title.Contains("Proposal")
                && !s.ProjectApprovalHistories.Any(h => h.Action == "Approved" || h.Action == "Rejected"))
            .OrderByDescending(s => s.LastSubmittedAt)
            .ToListAsync();
    }

    public async Task<MilestoneSubmission?> GetSubmissionWithDetailsAsync(int submissionId)
    {
        return await _context.MilestoneSubmissions
            .Include(s => s.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g.Leader)
            .Include(s => s.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g.Class)
            .Include(s => s.MilestoneDef)
            .Include(s => s.SubmissionFiles)
            .Include(s => s.ProjectApprovalHistories)
            .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);
    }
}


