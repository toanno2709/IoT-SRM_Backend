using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
namespace AppBackend.Repositories.Repositories.ClassRepo;

public class ClassRepository : GenericRepository<Class>, IClassRepository
{
    public ClassRepository(IotShowroomContext context) : base(context)
    {
    }

    /// <summary>
    /// Override GetAllAsync to include navigation properties
    /// Must match interface signature: Task<IEnumerable<Class>>
    /// </summary>
    public new async Task<IEnumerable<Class>> GetAllAsync()
    {
        return await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.Semester)
            .Include(c => c.ClassEnrollments)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Override GetByIdAsync to include navigation properties
    /// </summary>
    public new async Task<Class?> GetByIdAsync(object id)
    {
        return await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.Semester)
            .Include(c => c.ClassEnrollments)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
            .FirstOrDefaultAsync(c => c.ClassId == (int)id);
    }

    public async Task<List<Class>> GetAssignedClassesAsync(int instructorId)
    {
        return await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.Semester)
            .Include(c => c.ClassEnrollments)
                .ThenInclude(ce => ce.Student)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
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
                .ThenInclude(ce => ce.Student)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Leader)
            .Include(c => c.Groups)
                .ThenInclude(g => g.GroupMembers)
                    .ThenInclude(gm => gm.User)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
            .FirstOrDefaultAsync(c => c.ClassId == classId);
    }

    public async Task<List<Class>> GetBySemesterAsync(int semesterId)
    {
        return await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.Semester)
            .Include(c => c.ClassEnrollments)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
            .Where(c => c.SemesterId == semesterId)
            .OrderBy(c => c.ClassName)
            .ToListAsync();
    }

    public async Task<List<Class>> SearchClassesAsync(int? semesterId, string? searchQuery)
    {
        var query = _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.Semester)
            .Include(c => c.ClassEnrollments)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
            .AsQueryable();

        if (semesterId.HasValue)
        {
            query = query.Where(c => c.SemesterId == semesterId);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.ToLower();
            query = query.Where(c => 
                (c.ClassName != null && c.ClassName.ToLower().Contains(search)) ||
                (c.Description != null && c.Description.ToLower().Contains(search)) ||
                (c.Instructor != null && c.Instructor.FullName != null && c.Instructor.FullName.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ClassNameExistsAsync(string className, int semesterId)
    {
        return await _context.Classes
            .AnyAsync(c => c.ClassName == className && c.SemesterId == semesterId);
    }

    public async Task<bool> ClassNameExistsAsync(string className, int semesterId, int excludeClassId)
    {
        return await _context.Classes
            .AnyAsync(c => c.ClassName == className && c.SemesterId == semesterId && c.ClassId != excludeClassId);
    }

    public async Task<bool> HasGroupsAsync(int classId)
    {
        return await _context.Groups
            .AnyAsync(g => g.ClassId == classId);
    }

    public async Task<bool> HasEnrollmentsAsync(int classId)
    {
        return await _context.ClassEnrollments
            .AnyAsync(ce => ce.ClassId == classId);
    }
}
