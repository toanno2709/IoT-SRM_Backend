using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Class;

public interface IClassService
{
    Task<ResultModel<List<ClassResponseDto>>> GetAssignedClassesAsync(int instructorId);
    Task<ResultModel<List<ClassResponseDto>>> GetAllClassesAsync();
    Task<ResultModel<ClassResponseDto>> GetClassByIdAsync(int classId);
    Task<ResultModel<ClassDetailDto>> GetClassDetailAsync(int classId);
    Task<ResultModel<List<ClassResponseDto>>> GetClassesBySemesterAsync(int semesterId);
    Task<ResultModel<List<ClassResponseDto>>> SearchClassesAsync(int? semesterId, string? searchQuery);
    Task<ResultModel<ClassResponseDto>> CreateClassAsync(CreateClassRequestDto request);
    Task<ResultModel<ClassResponseDto>> UpdateClassAsync(int classId, UpdateClassRequestDto request);
    Task<ResultModel<bool>> DeleteClassAsync(int classId);
    Task<ResultModel<bool>> AssignInstructorAsync(int classId, int instructorId);
    Task<ResultModel<ClassSettingsResponseDto>> UpdateClassSettingsAsync(int classId, ClassSettingsUpdateRequestDto request);
    Task<ResultModel<ClassSettingsResponseDto>> GetClassSettingsAsync(int classId);
}
