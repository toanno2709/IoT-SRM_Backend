using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.ClassConfigRepo;

public interface IClassConfigRepository : IGenericRepository<ClassConfiguration>
{
    Task<ClassConfiguration?> GetByClassIdAsync(int classId);
    Task<ClassConfiguration?> GetByClassIdWithDetailsAsync(int classId);
    Task<bool> ExistsForClassAsync(int classId);
}
