using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.ApiModels.Semester;

namespace AppBackend.Services.Services.Semester
{
    public interface ISemesterService
    {
        // Get all semesters
        Task<ResultModel<List<SemesterResponseDto>>> GetAllSemestersAsync();

        // Get semester by ID
        Task<ResultModel<SemesterDetailDto>> GetSemesterByIdAsync(int semesterId);

        // Get active semester
        Task<ResultModel<SemesterResponseDto>> GetActiveSemesterAsync();

        // Get semesters by year
        Task<ResultModel<List<SemesterResponseDto>>> GetSemestersByYearAsync(int year);

        // Create semester
        Task<ResultModel<SemesterResponseDto>> CreateSemesterAsync(CreateSemesterRequestDto request);

        // Update semester
        Task<ResultModel<SemesterResponseDto>> UpdateSemesterAsync(int semesterId, UpdateSemesterRequestDto request);

        // Delete semester
        Task<ResultModel<bool>> DeleteSemesterAsync(int semesterId);

        // Set active semester
        Task<ResultModel<bool>> SetActiveSemesterAsync(int semesterId);
    }
}
