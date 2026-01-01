using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.StudentCourseHistory;

public interface IStudentCourseHistoryService
{
    Task<ResultModel<StudentCourseHistoryResponseDto>> GetByIdAsync(int historyId);
    Task<ResultModel<StudentCourseHistoryResponseDto>> GetCurrentByStudentIdAsync(int studentId);
    Task<ResultModel<List<StudentCourseHistoryResponseDto>>> GetAllByStudentIdAsync(int studentId);
    Task<ResultModel<List<StudentsByStatusResponseDto>>> GetStudentsByStatusAsync();
    Task<ResultModel<StudentCourseHistoryResponseDto>> CreateAsync(StudentCourseHistoryCreateDto dto);
    Task<ResultModel<StudentCourseHistoryResponseDto>> UpdateAsync(int historyId, StudentCourseHistoryUpdateDto dto);
    Task<ResultModel<StudentCourseHistoryResponseDto>> UpdateStatusAsync(int studentId, UpdateStudentCourseStatusDto dto);
    Task<ResultModel<bool>> DeleteAsync(int historyId);
}
