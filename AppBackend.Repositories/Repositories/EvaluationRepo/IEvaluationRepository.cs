using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.EvaluationRepo;

public interface IEvaluationRepository : IGenericRepository<Evaluation>
{
    Task<Evaluation?> GetByProjectAndInstructorAsync(int projectId, int instructorId);
    Task<Evaluation?> GetWithDetailsAsync(int evaluationId);
}

public class EvaluationRepository : GenericRepository<Evaluation>, IEvaluationRepository
{
    public EvaluationRepository(IOTShowroomContext context) : base(context)
    {
    }

    public async Task<Evaluation?> GetByProjectAndInstructorAsync(int projectId, int instructorId)
    {
        return await _context.Evaluations
            .Include(e => e.EvaluationDetails)
            .FirstOrDefaultAsync(e => e.ProjectId == projectId && e.InstructorId == instructorId);
    }

    public async Task<Evaluation?> GetWithDetailsAsync(int evaluationId)
    {
        return await _context.Evaluations
            .Include(e => e.EvaluationDetails)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId);
    }
}



