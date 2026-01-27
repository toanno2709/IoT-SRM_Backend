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

            // Count active classes (classes with status "Active" or all classes if no status field)
            // Based on Class model, if there's a Status field, we can filter by it
            // Otherwise, count all classes with at least 1 enrollment
            var activeClasses = await _db.Classes
                .Where(c => c.ClassEnrollments!.Any())
                .CountAsync();

            // Count live demos (demos that are currently running - ended_at is null or in the future)
            var liveDemos = await _db.LiveDemos
                .Where(d => d.EndedAt == null || d.EndedAt > DateTime.UtcNow)
                .CountAsync();

            // Count connected devices (sensors)
            var connectedDevices = await _db.Sensors.CountAsync();

            var statistics = new PublicStatisticsResponseDto
            {
                TotalProjects = totalProjects,
                ActiveClasses = activeClasses,
                LiveDemos = liveDemos,
                ConnectedDevices = connectedDevices
            };

            _logger.LogInformation(
                "Public statistics retrieved: {TotalProjects} projects, {ActiveClasses} classes, {LiveDemos} demos, {ConnectedDevices} devices",
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
