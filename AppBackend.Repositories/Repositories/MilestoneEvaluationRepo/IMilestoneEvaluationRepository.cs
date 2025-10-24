using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;

public interface IMilestoneEvaluationRepository : IGenericRepository<MilestoneEvaluation>
{
    Task<MilestoneEvaluation?> GetByProjectMilestoneInstructorAsync(int projectId, int milestoneDefId, int instructorId);
    Task<List<MilestoneEvaluation>> GetByProjectAsync(int projectId);
}

public class MilestoneEvaluationRepository : GenericRepository<MilestoneEvaluation>, IMilestoneEvaluationRepository
{
    public MilestoneEvaluationRepository(IOTShowroomContext context) : base(context)
    {
    }

    public async Task<MilestoneEvaluation?> GetByProjectMilestoneInstructorAsync(int projectId, int milestoneDefId, int instructorId)
    {
        return await _context.MilestoneEvaluations
            .Include(e => e.Project)
            .Include(e => e.MilestoneDef)
            .Include(e => e.Instructor)
            .FirstOrDefaultAsync(e => e.ProjectId == projectId 
                && e.MilestoneDefId == milestoneDefId 
                && e.InstructorId == instructorId);
    }

    public async Task<List<MilestoneEvaluation>> GetByProjectAsync(int projectId)
    {
        return await _context.MilestoneEvaluations
            .Include(e => e.MilestoneDef)
            .Include(e => e.Instructor)
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.MilestoneDefId)
            .ToListAsync();
    }
}



