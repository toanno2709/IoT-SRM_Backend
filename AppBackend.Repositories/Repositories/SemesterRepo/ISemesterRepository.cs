using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.SemesterRepo
{
    public interface ISemesterRepository : IGenericRepository<Semester>
    {
        Task<Semester?> GetByCodeAsync(string code);
        Task<bool> CodeExistsAsync(string code);
        Task<bool> CodeExistsAsync(string code, int excludeSemesterId);
        Task<Semester?> GetActiveAsync();
        Task<IEnumerable<Semester>> GetByYearAsync(int year);
    }
}
