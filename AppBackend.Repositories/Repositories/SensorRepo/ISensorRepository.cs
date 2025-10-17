using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.SensorRepo
{
    public interface ISensorRepository : IGenericRepository<Sensor>
    {
        Task<List<Sensor>> GetByProjectIdAsync(int projectId);
        Task<Sensor?> GetByIdWithProjectAsync(int sensorId);
        Task<List<Sensor>> SearchSensorsAsync(int projectId, string? searchQuery, int page, int pageSize);
        Task<int> CountByProjectAsync(int projectId, string? searchQuery);
        Task<bool> SensorNameExistsAsync(int projectId, string name);
        Task<bool> SensorNameExistsAsync(int projectId, string name, int excludeSensorId);
        Task<bool> BelongsToProjectAsync(int sensorId, int projectId);
        Task<bool> HasSensorDataAsync(int sensorId);
        Task<bool> HasLiveDemoRelationsAsync(int sensorId);
    }
}
