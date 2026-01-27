using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.PublicStatistics;

/// <summary>
/// Service interface for public statistics
/// </summary>
public interface IPublicStatisticsService
{
    /// <summary>
    /// Get public statistics without authentication
    /// </summary>
    /// <returns>Statistics including total projects, active classes, live demos, and connected devices</returns>
    Task<ResultModel<PublicStatisticsResponseDto>> GetPublicStatisticsAsync();
}
