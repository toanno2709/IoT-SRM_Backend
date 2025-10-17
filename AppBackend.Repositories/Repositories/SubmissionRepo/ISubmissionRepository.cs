using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.SubmissionRepo;

public interface ISubmissionRepository : IGenericRepository<ProjectSubmission>
{
    Task<List<ProjectSubmission>> GetPendingProposalsByInstructorAsync(int instructorId);
}

public class SubmissionRepository : GenericRepository<ProjectSubmission>, ISubmissionRepository
{
    public SubmissionRepository(IOTShowroomContext context) : base(context)
    {
    }

    public async Task<List<ProjectSubmission>> GetPendingProposalsByInstructorAsync(int instructorId)
    {
        return await _context.ProjectSubmissions
            .Include(s => s.Project).ThenInclude(p => p.Class)
            .Include(s => s.Project).ThenInclude(p => p.Leader)
            .Where(s => s.Status == "Pending" && s.Project.Class != null && s.Project.Class.InstructorId == instructorId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }
}



