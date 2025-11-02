using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.ClassConfigRepo;

public class ClassConfigRepository : GenericRepository<ClassConfiguration>, IClassConfigRepository
{
    public ClassConfigRepository(IotShowroomContext context) : base(context)
    {
    }

    public async Task<ClassConfiguration?> GetByClassIdAsync(int classId)
    {
        return await _context.ClassConfigurations
            .FirstOrDefaultAsync(cc => cc.ClassId == classId);
    }

    public async Task<ClassConfiguration?> GetByClassIdWithDetailsAsync(int classId)
    {
        return await _context.ClassConfigurations
            .Include(cc => cc.Class)
            .FirstOrDefaultAsync(cc => cc.ClassId == classId);
    }

    public async Task<bool> ExistsForClassAsync(int classId)
    {
        return await _context.ClassConfigurations
            .AnyAsync(cc => cc.ClassId == classId);
    }
}
