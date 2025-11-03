using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.AdminDashboard;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Attributes;

namespace AppBackend.ApiCore.Controllers;

/// <summary>
/// Admin-specific operations and dashboard
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    #region Dashboard APIs

    /// <summary>
    /// Get admin dashboard overview
    /// </summary>
    /// <returns>System-wide overview with statistics and recent activities</returns>
    /// <remarks>
    /// Provides a comprehensive overview including:
    /// - Total counts (classes, instructors, students, groups, projects)
    /// - Active semester count
    /// - Pending approvals
    /// - Recent announcements
    /// - Recent activities (last 10)
    /// - System alerts
    /// </remarks>
    [HttpGet("dashboard/overview")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<AdminDashboardOverviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<AdminDashboardOverviewDto>>> GetDashboardOverview()
    {
        var result = await _dashboardService.GetDashboardOverviewAsync();

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get detailed system statistics
    /// </summary>
    /// <returns>Comprehensive system statistics</returns>
    /// <remarks>
    /// Provides detailed statistics including:
    /// - User statistics (total, by role, active users, new users)
    /// - Class statistics (total, active, average size, classes without instructor)
    /// - Project statistics (total, by status, completion rate)
    /// - Submission statistics (total, graded, pending, late submissions, average score)
    /// - System health (announcements, backup status, database size)
    /// </remarks>
    [HttpGet("dashboard/statistics")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<AdminStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<AdminStatisticsDto>>> GetSystemStatistics()
    {
        var result = await _dashboardService.GetSystemStatisticsAsync();

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region Chart APIs

    /// <summary>
    /// Get classes by semester chart data
    /// </summary>
    /// <returns>Bar chart data showing number of classes per semester</returns>
    /// <remarks>
    /// Returns data suitable for rendering a bar chart showing:
    /// - Number of classes in each semester
    /// - Semester names as labels
    /// - Color coding for each semester
    /// </remarks>
    [HttpGet("dashboard/charts/classes-by-semester")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ClassesBySemesterChartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ClassesBySemesterChartDto>>> GetClassesBySemesterChart()
    {
        var result = await _dashboardService.GetClassesBySemesterChartAsync();

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get project distribution chart data
    /// </summary>
    /// <returns>Pie chart data showing project distribution by status</returns>
    /// <remarks>
    /// Returns data suitable for rendering a pie chart showing:
    /// - Number of projects in each status (Pending, Approved, Completed, Rejected)
    /// - Percentage distribution
    /// - Color coding by status
    /// </remarks>
    [HttpGet("dashboard/charts/project-distribution")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ProjectDistributionChartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ProjectDistributionChartDto>>> GetProjectDistributionChart()
    {
        var result = await _dashboardService.GetProjectDistributionChartAsync();

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get milestone completion chart data
    /// </summary>
    /// <returns>Line chart data showing milestone completion progress</returns>
    /// <remarks>
    /// Returns data suitable for rendering a line chart showing:
    /// - Number of completed milestones
    /// - Completion rate per milestone
    /// - Progress over time
    /// </remarks>
    [HttpGet("dashboard/charts/milestone-completion")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<MilestoneCompletionChartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<MilestoneCompletionChartDto>>> GetMilestoneCompletionChart()
    {
        var result = await _dashboardService.GetMilestoneCompletionChartAsync();

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion
}
