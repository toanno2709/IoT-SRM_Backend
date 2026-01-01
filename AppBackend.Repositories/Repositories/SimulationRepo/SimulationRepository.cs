using AppBackend.BusinessObjects.Models;
using AppBackend.BusinessObjects.Data;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.SimulationRepo;

public class SimulationRepository : GenericRepository<Simulation>, ISimulationRepository
{
    public SimulationRepository(IotShowroomContext context) : base(context)
    {
    }

    public async Task<List<Simulation>> GetByProjectIdAsync(int projectId)
    {
        return await _context.Simulations
            .Include(s => s.Project)
                .ThenInclude(p => p!.Group)
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Simulation?> GetByIdWithDetailsAsync(int simulationId)
    {
        return await _context.Simulations
            .Include(s => s.Project)
                .ThenInclude(p => p!.Group)
                    .ThenInclude(g => g!.Leader)
            .Include(s => s.Project)
                .ThenInclude(p => p!.Group)
                    .ThenInclude(g => g!.Class)
            .Include(s => s.Project)
                .ThenInclude(p => p!.Group)
                    .ThenInclude(g => g!.GroupMembers)
                        .ThenInclude(gm => gm.User)
            .FirstOrDefaultAsync(s => s.SimulationId == simulationId);
    }

    public async Task<List<Simulation>> GetByStatusAsync(string status)
    {
        return await _context.Simulations
            .Include(s => s.Project)
                .ThenInclude(p => p!.Group)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }
}
