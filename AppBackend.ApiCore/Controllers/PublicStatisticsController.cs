using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.PublicStatistics;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.ApiCore.Controllers;

/// <summary>
/// Public Statistics Controller - No authentication required
/// Provides public-facing statistics for IoT projects, classes, demos, and devices
/// </summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicStatisticsController : ControllerBase
{
    private readonly IPublicStatisticsService _statisticsService;
    private readonly ILogger<PublicStatisticsController> _logger;

    public PublicStatisticsController(
        IPublicStatisticsService statisticsService,
        ILogger<PublicStatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get public statistics (IoT Projects, Active Classes, Live Demos, Connected Devices)
    /// </summary>
    /// <returns>Public statistics</returns>
    /// <remarks>
    /// Returns public statistics for display on homepage or dashboard:
    /// - **TotalProjects**: Total number of IoT projects in the system
    /// - **ActiveClasses**: Number of active classes (classes with at least one enrolled student)
    /// - **LiveDemos**: Number of live demos currently running (demos without end date or ending in the future)
    /// - **ConnectedDevices**: Total number of sensors/devices registered in the system
    /// 
    /// **No authentication required** - This endpoint is publicly accessible.
    /// 
    /// Example response:
    /// ```json
    /// {
    ///   "isSuccess": true,
    ///   "message": "Public statistics retrieved successfully",
    ///   "data": {
    ///     "totalProjects": 9,
    ///     "activeClasses": 8,
    ///     "liveDemos": 9,
    ///     "connectedDevices": 9
    ///   },
    ///   "statusCode": 200
    /// }
    /// ```
    /// </remarks>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ResultModel<PublicStatisticsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<PublicStatisticsResponseDto>>> GetPublicStatistics()
    {
        _logger.LogInformation("Public statistics endpoint called");

        var result = await _statisticsService.GetPublicStatisticsAsync();

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }
}
