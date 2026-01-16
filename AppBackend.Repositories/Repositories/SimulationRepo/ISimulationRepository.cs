using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.SimulationRepo;

public interface ISimulationRepository : IGenericRepository<Simulation>
{
    Task<List<Simulation>> GetByProjectIdAsync(int projectId);
    Task<Simulation?> GetByIdWithDetailsAsync(int simulationId);
    Task<List<Simulation>> GetByStatusAsync(string status);
}
