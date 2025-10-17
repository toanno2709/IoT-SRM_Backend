using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.SubmissionRepo;

// MilestoneSubmission thay th? ProjectSubmission trong schema m?i
public interface ISubmissionRepository : IGenericRepository<MilestoneSubmission>
{
    Task<List<MilestoneSubmission>> GetByProjectAsync(int projectId);
    Task<MilestoneSubmission?> GetByProjectAndMilestoneAsync(int projectId, int milestoneDefId);
    Task<List<SubmissionFile>> GetFilesBySubmissionAsync(int submissionId);
}

public class SubmissionRepository : GenericRepository<MilestoneSubmission>, ISubmissionRepository
{
    public SubmissionRepository(IOTShowroomContext context) : base(context)
    {
    }

    public async Task<List<MilestoneSubmission>> GetByProjectAsync(int projectId)
    {
        return await _context.MilestoneSubmissions
            .Include(ms => ms.MilestoneDef)
            .Include(ms => ms.SubmissionFiles)
            .Where(ms => ms.ProjectId == projectId)
            .OrderByDescending(ms => ms.LastSubmittedAt)
            .ToListAsync();
    }

    public async Task<MilestoneSubmission?> GetByProjectAndMilestoneAsync(int projectId, int milestoneDefId)
    {
        return await _context.MilestoneSubmissions
            .Include(ms => ms.MilestoneDef)
            .Include(ms => ms.SubmissionFiles)
            .FirstOrDefaultAsync(ms => ms.ProjectId == projectId && ms.MilestoneDefId == milestoneDefId);
    }

    public async Task<List<SubmissionFile>> GetFilesBySubmissionAsync(int submissionId)
    {
        return await _context.SubmissionFiles
            .Include(sf => sf.UploadedByNavigation)
            .Where(sf => sf.SubmissionId == submissionId)
            .OrderByDescending(sf => sf.VersionNo)
            .ThenByDescending(sf => sf.UploadedAt)
            .ToListAsync();
    }
}



