using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.ApiModels.Sensor;

namespace AppBackend.Services.Services.Sensor
{
    public interface ISensorService
    {
        // CRUD operations
        Task<ResultModel<SensorResponseDto>> CreateSensorAsync(int projectId, int currentUserId, CreateSensorRequestDto request);
        Task<ResultModel<SensorListResponseDto>> GetSensorsByProjectAsync(int projectId, int currentUserId, string? searchQuery, int page, int pageSize);
        Task<ResultModel<SensorDetailDto>> GetSensorByIdAsync(int projectId, int sensorId, int currentUserId);
        Task<ResultModel<SensorResponseDto>> UpdateSensorAsync(int projectId, int sensorId, int currentUserId, UpdateSensorRequestDto request);
        Task<ResultModel<bool>> DeleteSensorAsync(int projectId, int sensorId, int currentUserId);

        // Helper methods
        Task<bool> CanAccessProjectAsync(int projectId, int userId, string userRole);
    }
}
