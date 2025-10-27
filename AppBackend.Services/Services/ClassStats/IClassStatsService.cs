using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.ClassStats;

public interface IClassStatsService
{
    Task<ResultModel<ClassStatsResponseDto>> GetClassStatsAsync(int classId);
}
