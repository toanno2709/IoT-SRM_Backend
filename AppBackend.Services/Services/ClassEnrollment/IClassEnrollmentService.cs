using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.ClassEnrollment;

public interface IClassEnrollmentService
{
    /// <summary>
    /// Bulk add students to a class automatically
    /// Finds available students (role_id = 3) who are not already in the class
    /// and adds them up to the specified max members limit
    /// </summary>
    /// <param name="request">Request containing class ID and max members</param>
    /// <returns>Result with enrollment details</returns>
    Task<ResultModel<BulkAddStudentsResponseDto>> BulkAddStudentsAsync(BulkAddStudentsRequestDto request);

    /// <summary>
    /// Add a specific student to a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="studentId">Student ID to add</param>
    /// <returns>Result with enrollment details</returns>
    Task<ResultModel<AddStudentToClassResponseDto>> AddStudentToClassAsync(int classId, int studentId);

    /// <summary>
    /// Remove a student from a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="studentId">Student ID to remove</param>
    /// <returns>Result indicating success/failure</returns>
    Task<ResultModel<bool>> RemoveStudentFromClassAsync(int classId, int studentId);

    /// <summary>
    /// Get all students in a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of students in the class</returns>
    Task<ResultModel<ClassStudentsResponseDto>> GetClassStudentsAsync(int classId);
    
    /// <summary>
    /// Get all students in a class with their group status
    /// Shows which students have groups and which don't, including group details
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of students with group information</returns>
    Task<ResultModel<ClassStudentsWithGroupResponseDto>> GetClassStudentsWithGroupAsync(int classId);
    
    /// <summary>
    /// Get students in a class who are not assigned to any group
    /// Supports optional search query to filter by name or email
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="searchQuery">Optional search query for student name or email</param>
    /// <returns>List of unassigned students</returns>
    Task<ResultModel<UnassignedStudentsResponseDto>> GetUnassignedStudentsAsync(int classId, string? searchQuery = null);
    
    /// <summary>
    /// Import students to class from Excel file
    /// Validates: No duplicate, Student passed IOT, Student not in class, Email exists
    /// </summary>
    /// <param name="classId">Class ID to add students to</param>
    /// <param name="excelFile">Excel file with Email and Status columns</param>
    /// <returns>Result with successful and failed imports</returns>
    Task<ResultModel<ImportStudentsResultDto>> ImportStudentsFromExcelAsync(int classId, IFormFile excelFile);
}
