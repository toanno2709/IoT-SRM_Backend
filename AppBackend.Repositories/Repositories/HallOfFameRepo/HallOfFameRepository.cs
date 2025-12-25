using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.HallOfFameRepo;

public class HallOfFameRepository : IHallOfFameRepository
{
    private readonly IotShowroomContext _context;

    public HallOfFameRepository(IotShowroomContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HallOfFame>> GetAllAsync()
    {
        return await _context.HallOfFames
            .Include(h => h.Project)
                .ThenInclude(p => p!.Group)
            .Include(h => h.Project)
                .ThenInclude(p => p!.FinalProjectSubmission)
            .Include(h => h.Semester)
            .OrderByDescending(h => h.NominatedAt)
            .ToListAsync();
    }

    public async Task<HallOfFame?> GetByIdAsync(int hofId)
    {
        return await _context.HallOfFames
            .Include(h => h.Project)
                .ThenInclude(p => p!.Group)
            .Include(h => h.Project)
                .ThenInclude(p => p!.FinalProjectSubmission)
            .Include(h => h.Semester)
            .FirstOrDefaultAsync(h => h.HofId == hofId);
    }

    public async Task<IEnumerable<HallOfFame>> GetBySemesterAsync(int semesterId)
    {
        return await _context.HallOfFames
            .Include(h => h.Project)
                .ThenInclude(p => p!.Group)
            .Include(h => h.Project)
                .ThenInclude(p => p!.FinalProjectSubmission)
            .Include(h => h.Semester)
            .Where(h => h.SemesterId == semesterId)
            .OrderBy(h => h.Rank)
            .ThenByDescending(h => h.NominatedAt)
            .ToListAsync();
    }

    public async Task<HallOfFame?> GetByProjectIdAsync(int projectId)
    {
        return await _context.HallOfFames
            .Include(h => h.Project)
                .ThenInclude(p => p!.Group)
            .Include(h => h.Project)
                .ThenInclude(p => p!.FinalProjectSubmission)
            .Include(h => h.Semester)
            .FirstOrDefaultAsync(h => h.ProjectId == projectId);
    }

    public async Task<IEnumerable<HallOfFame>> GetTop10BySemesterAsync(int semesterId)
    {
        return await _context.HallOfFames
            .Include(h => h.Project)
                .ThenInclude(p => p!.Group)
            .Include(h => h.Project)
                .ThenInclude(p => p!.FinalProjectSubmission)
            .Include(h => h.Semester)
            .Where(h => h.SemesterId == semesterId)
            .OrderBy(h => h.Rank)
            .ThenByDescending(h => h.NominatedAt)
            .Take(10)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int projectId, int semesterId)
    {
        return await _context.HallOfFames
            .AnyAsync(h => h.ProjectId == projectId && h.SemesterId == semesterId);
    }

    public async Task AddAsync(HallOfFame hallOfFame)
    {
        await _context.HallOfFames.AddAsync(hallOfFame);
    }

    public async Task UpdateAsync(HallOfFame hallOfFame)
    {
        _context.HallOfFames.Update(hallOfFame);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(HallOfFame hallOfFame)
    {
        _context.HallOfFames.Remove(hallOfFame);
        await Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
