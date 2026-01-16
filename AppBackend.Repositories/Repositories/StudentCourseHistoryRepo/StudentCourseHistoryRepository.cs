using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.StudentCourseHistoryRepo;

public class StudentCourseHistoryRepository : GenericRepository<StudentCourseHistory>, IStudentCourseHistoryRepository
{
    public StudentCourseHistoryRepository(IotShowroomContext context) : base(context)
    {
    }

    public async Task<StudentCourseHistory?> GetCurrentByStudentIdAsync(int studentId)
    {
        return await _context.StudentCourseHistories
            .Include(sch => sch.Student)
            .Include(sch => sch.Semester)
            .Include(sch => sch.FinalSubmission)
            .FirstOrDefaultAsync(sch => sch.StudentId == studentId && sch.IsCurrent == true);
    }

    public async Task<List<StudentCourseHistory>> GetAllByStudentIdAsync(int studentId)
    {
        return await _context.StudentCourseHistories
            .Include(sch => sch.Student)
            .Include(sch => sch.Semester)
            .Include(sch => sch.FinalSubmission)
            .Where(sch => sch.StudentId == studentId)
            .OrderByDescending(sch => sch.IsCurrent)
            .ThenByDescending(sch => sch.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<StudentCourseHistory>> GetByStatusAsync(string status)
    {
        return await _context.StudentCourseHistories
            .Include(sch => sch.Student)
            .Include(sch => sch.Semester)
            .Where(sch => sch.Status == status && sch.IsCurrent == true)
            .OrderBy(sch => sch.Student!.FullName)
            .ToListAsync();
    }

    public async Task<bool> HasPassedCourseAsync(int studentId)
    {
        return await _context.StudentCourseHistories
            .AnyAsync(sch => sch.StudentId == studentId && 
                           sch.Status == "Pass" && 
                           sch.IsCurrent == true);
    }

    public async Task<bool> IsEligibleForEnrollmentAsync(int studentId)
    {
        var current = await GetCurrentByStudentIdAsync(studentId);
        
        if (current == null)
            return true; // No history, eligible
            
        // Not eligible if already passed
        if (current.Status == "Pass")
            return false;
            
        return true;
    }

    public async Task<StudentCourseHistory> CreateNewHistoryRecordAsync(StudentCourseHistory newRecord)
    {
        // Mark current record as not current
        var currentRecord = await GetCurrentByStudentIdAsync(newRecord.StudentId);
        if (currentRecord != null)
        {
            currentRecord.IsCurrent = false;
            currentRecord.UpdatedAt = DateTime.UtcNow;
            _context.StudentCourseHistories.Update(currentRecord);
        }

        // Add new record as current
        newRecord.IsCurrent = true;
        newRecord.CreatedAt = DateTime.UtcNow;
        newRecord.UpdatedAt = DateTime.UtcNow;
        
        await _context.StudentCourseHistories.AddAsync(newRecord);
        await _context.SaveChangesAsync();
        
        return newRecord;
    }

    public async Task<StudentCourseHistory?> GetByIdWithIncludesAsync(int historyId)
    {
        return await _context.StudentCourseHistories
            .Include(sch => sch.Student)
            .Include(sch => sch.Semester)
            .Include(sch => sch.FinalSubmission)
            .FirstOrDefaultAsync(sch => sch.HistoryId == historyId);
    }
}
