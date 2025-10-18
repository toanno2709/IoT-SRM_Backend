using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.EvaluationRepo;

// MilestoneEvaluation thay th? Evaluation trong schema m?i
public interface IEvaluationRepository : IGenericRepository<MilestoneEvaluation>
{
    Task<MilestoneEvaluation?> GetByProjectMilestoneAndInstructorAsync(int projectId, int milestoneDefId, int instructorId);
    Task<IEnumerable<MilestoneEvaluation>> GetByProjectAsync(int projectId);
    Task<IEnumerable<MilestoneEvaluation>> GetByMilestoneAsync(int milestoneDefId);
}

public class EvaluationRepository : GenericRepository<MilestoneEvaluation>, IEvaluationRepository
{
    public EvaluationRepository(IOTShowroomContext context) : base(context)
    {
    }

    public async Task<MilestoneEvaluation?> GetByProjectMilestoneAndInstructorAsync(int projectId, int milestoneDefId, int instructorId)
    {
        return await _context.MilestoneEvaluations
            .Include(me => me.MilestoneDef)
            .Include(me => me.Instructor)
            .FirstOrDefaultAsync(me => me.ProjectId == projectId 
                && me.MilestoneDefId == milestoneDefId 
                && me.InstructorId == instructorId);
    }

    public async Task<IEnumerable<MilestoneEvaluation>> GetByProjectAsync(int projectId)
    {
        return await _context.MilestoneEvaluations
            .Include(me => me.MilestoneDef)
            .Include(me => me.Instructor)
            .Where(me => me.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<IEnumerable<MilestoneEvaluation>> GetByMilestoneAsync(int milestoneDefId)
    {
        return await _context.MilestoneEvaluations
            .Include(me => me.Project)
            .Include(me => me.Instructor)
            .Where(me => me.MilestoneDefId == milestoneDefId)
            .ToListAsync();
    }
}



