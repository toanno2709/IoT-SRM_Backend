using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.ClassRepo;

public interface IClassRepository : IGenericRepository<Class>
{
    Task<List<Class>> GetAssignedClassesAsync(int instructorId);
    Task<Class?> GetClassWithDetailsAsync(int classId);
}

public class ClassRepository : GenericRepository<Class>, IClassRepository
{
    public ClassRepository(IOTShowroomContext context) : base(context)
    {
    }

    public async Task<List<Class>> GetAssignedClassesAsync(int instructorId)
    {
        return await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.Semester)
            .Include(c => c.ClassEnrollments)
            .Include(c => c.Projects)
            .Where(c => c.InstructorId == instructorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Class?> GetClassWithDetailsAsync(int classId)
    {
        return await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.Semester)
            .Include(c => c.ClassEnrollments)
            .Include(c => c.Projects)
            .FirstOrDefaultAsync(c => c.ClassId == classId);
    }
}




