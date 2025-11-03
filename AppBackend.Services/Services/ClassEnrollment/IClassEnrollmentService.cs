using AppBackend.Services.ApiModels.Commons;

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
}
