using AppBackend.BusinessObjects.Data;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.PublicStatistics;

/// <summary>
/// Service for public statistics - No authentication required
/// </summary>
public class PublicStatisticsService : IPublicStatisticsService
{
    private readonly IotShowroomContext _db;
    private readonly ILogger<PublicStatisticsService> _logger;

    public PublicStatisticsService(
        IotShowroomContext db,
        ILogger<PublicStatisticsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ResultModel<PublicStatisticsResponseDto>> GetPublicStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Getting public statistics");

            // Count total projects
            var totalProjects = await _db.Projects.CountAsync();

            // Count active classes (classes with at least 1 enrolled student)
            var activeClasses = await _db.Classes
                .Where(c => c.ClassEnrollments!.Any())
                .CountAsync();

            // Count projects with submitted simulations (Live Demos)
            var liveDemos = await _db.Projects
                .Where(p => p.Simulations.Any(s => s.Status == "submitted"))
                .CountAsync();

            // Count connected devices (sensors with recent activity - data within last 30 days)
            // If no sensor data, count all sensors instead
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var activeSensors = await _db.Sensors
                .Where(s => s.SensorData.Any(sd => sd.Timestamp != null && sd.Timestamp > thirtyDaysAgo))
                .CountAsync();
            
            // If no active sensors, fall back to total sensor count
            var connectedDevices = activeSensors > 0 ? activeSensors : await _db.Sensors.CountAsync();

            var statistics = new PublicStatisticsResponseDto
            {
                TotalProjects = totalProjects,
                ActiveClasses = activeClasses,
                LiveDemos = liveDemos,
                ConnectedDevices = connectedDevices
            };

            _logger.LogInformation(
                "Public statistics retrieved: {TotalProjects} projects, {ActiveClasses} classes, {LiveDemos} projects with submitted simulations, {ConnectedDevices} active devices",
                totalProjects, activeClasses, liveDemos, connectedDevices);

            return new ResultModel<PublicStatisticsResponseDto>
            {
                IsSuccess = true,
                Message = "Public statistics retrieved successfully",
                Data = statistics,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public statistics");
            return new ResultModel<PublicStatisticsResponseDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving public statistics: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
