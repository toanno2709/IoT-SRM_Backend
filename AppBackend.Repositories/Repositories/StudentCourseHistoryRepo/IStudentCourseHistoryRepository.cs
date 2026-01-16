using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.StudentCourseHistoryRepo;

public interface IStudentCourseHistoryRepository : IGenericRepository<StudentCourseHistory>
{
    /// <summary>
    /// Get current course history for a student
    /// </summary>
    Task<StudentCourseHistory?> GetCurrentByStudentIdAsync(int studentId);

    /// <summary>
    /// Get all history records for a student
    /// </summary>
    Task<List<StudentCourseHistory>> GetAllByStudentIdAsync(int studentId);

    /// <summary>
    /// Get students by status
    /// </summary>
    Task<List<StudentCourseHistory>> GetByStatusAsync(string status);

    /// <summary>
    /// Check if student has completed the course (status = "Pass")
    /// </summary>
    Task<bool> HasPassedCourseAsync(int studentId);

    /// <summary>
    /// Check if student is eligible for enrollment (has not passed)
    /// </summary>
    Task<bool> IsEligibleForEnrollmentAsync(int studentId);

    /// <summary>
    /// Mark current record as not current and create a new one
    /// </summary>
    Task<StudentCourseHistory> CreateNewHistoryRecordAsync(StudentCourseHistory newRecord);

    /// <summary>
    /// Get by ID with navigation properties loaded
    /// </summary>
    Task<StudentCourseHistory?> GetByIdWithIncludesAsync(int historyId);
}
