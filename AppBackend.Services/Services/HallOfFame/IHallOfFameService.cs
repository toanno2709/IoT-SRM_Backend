using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.HallOfFame;

public interface IHallOfFameService
{
    Task<ResultModel<List<HallOfFameResponseDto>>> GetAllHallOfFameAsync();
    Task<ResultModel<HallOfFameResponseDto>> GetHallOfFameByIdAsync(int hofId);
    Task<ResultModel<List<HallOfFameResponseDto>>> GetHallOfFameBySemesterAsync(int semesterId);
    Task<ResultModel<LeaderboardResponseDto>> GetLeaderboardBySemesterAsync(int semesterId);
    Task<ResultModel<HallOfFameResponseDto>> NominateProjectAsync(int adminId, HallOfFameNominateRequestDto request);
    Task<ResultModel<HallOfFameResponseDto>> UpdateHallOfFameAsync(int hofId, HallOfFameUpdateRequestDto request);
    Task<ResultModel<bool>> DeleteHallOfFameAsync(int hofId);
}
