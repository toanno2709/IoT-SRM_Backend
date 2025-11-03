using AppBackend.BusinessObjects.Models;

namespace AppBackend.Repositories.Repositories.HallOfFameRepo;

public interface IHallOfFameRepository
{
    Task<IEnumerable<HallOfFame>> GetAllAsync();
    Task<HallOfFame?> GetByIdAsync(int hofId);
    Task<IEnumerable<HallOfFame>> GetBySemesterAsync(int semesterId);
    Task<HallOfFame?> GetByProjectIdAsync(int projectId);
    Task<IEnumerable<HallOfFame>> GetTop10BySemesterAsync(int semesterId);
    Task<bool> ExistsAsync(int projectId, int semesterId);
    Task AddAsync(HallOfFame hallOfFame);
    Task UpdateAsync(HallOfFame hallOfFame);
    Task DeleteAsync(HallOfFame hallOfFame);
    Task<int> SaveChangesAsync();
}
