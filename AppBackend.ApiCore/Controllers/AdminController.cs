using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.AdminDashboard;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Attributes;
using AppBackend.Services.Services.HallOfFame;
using AppBackend.Services.Services.AdminReport;
using AppBackend.Services.Services.ClassEnrollment;
using AppBackend.Services.Services.StudentGrade;
using AppBackend.Services.Services.AdminClassGrader;
using System.Security.Claims;

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
    private readonly IHallOfFameService _hallOfFameService;
    private readonly IAdminReportService _reportService;
    private readonly IClassEnrollmentService _classEnrollmentService;
    private readonly IStudentGradeService _studentGradeService;
    private readonly IAdminClassGraderService _adminClassGraderService;

    public AdminController(
        IAdminDashboardService dashboardService,
        IHallOfFameService hallOfFameService,
        IAdminReportService reportService,
        IClassEnrollmentService classEnrollmentService,
        IStudentGradeService studentGradeService,
        IAdminClassGraderService adminClassGraderService)
    {
        _dashboardService = dashboardService;
        _hallOfFameService = hallOfFameService;
        _reportService = reportService;
        _classEnrollmentService = classEnrollmentService;
        _studentGradeService = studentGradeService;
        _adminClassGraderService = adminClassGraderService;
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
    [ApiExplorerSettings(GroupName = "admin-dashboard")]
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
    [ApiExplorerSettings(GroupName = "admin-dashboard")]
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
    [ApiExplorerSettings(GroupName = "admin-charts")]
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
    [ApiExplorerSettings(GroupName = "admin-charts")]
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
    [ApiExplorerSettings(GroupName = "admin-charts")]
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

    #region Hall of Fame APIs

    /// <summary>
    /// Get all Hall of Fame entries
    /// </summary>
    /// <returns>List of all Hall of Fame entries across all semesters</returns>
    /// <remarks>
    /// Returns all projects that have been nominated to the Hall of Fame,
    /// ordered by nomination date (most recent first).
    /// </remarks>
    [HttpGet("hall-of-fame")]
    [ApiExplorerSettings(GroupName = "admin-hall-of-fame")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<List<HallOfFameResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<List<HallOfFameResponseDto>>>> GetAllHallOfFame()
    {
        var result = await _hallOfFameService.GetAllHallOfFameAsync();

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get Hall of Fame entries for a specific semester
    /// </summary>
    /// <param name="semesterId">Semester ID</param>
    /// <returns>List of Hall of Fame entries for the semester</returns>
    /// <remarks>
    /// Returns all projects nominated to Hall of Fame for the specified semester,
    /// ordered by rank (ascending) then by nomination date.
    /// </remarks>
    [HttpGet("hall-of-fame/{semesterId}")]
    [ApiExplorerSettings(GroupName = "admin-hall-of-fame")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<List<HallOfFameResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<List<HallOfFameResponseDto>>>> GetHallOfFameBySemester([FromRoute] int semesterId)
    {
        var result = await _hallOfFameService.GetHallOfFameBySemesterAsync(semesterId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get leaderboard with top 10 projects for a semester
    /// </summary>
    /// <param name="semesterId">Semester ID</param>
    /// <returns>Leaderboard with top 10 projects ranked by final score</returns>
    /// <remarks>
    /// Returns the top 10 completed projects in the semester, ranked by final score.
    /// Indicates which projects are already in the Hall of Fame.
    /// Only includes projects with status "Completed" and a final score.
    /// </remarks>
    [HttpGet("leaderboard/{semesterId}/top-10")]
    [ApiExplorerSettings(GroupName = "admin-hall-of-fame")]
    [AllowAnonymous] // Public leaderboard
    [RateLimit(permitLimit: 60, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<LeaderboardResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<LeaderboardResponseDto>>> GetLeaderboardTop10([FromRoute] int semesterId)
    {
        var result = await _hallOfFameService.GetLeaderboardBySemesterAsync(semesterId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Nominate a project to Hall of Fame
    /// </summary>
    /// <param name="request">Nomination request with project and semester details</param>
    /// <returns>Created Hall of Fame entry</returns>
    /// <remarks>
    /// Allows admin to manually nominate a completed project to the Hall of Fame.
    /// 
    /// Requirements:
    /// - Project must have status "Completed"
    /// - Project must have a final score of at least 80
    /// - Project cannot already be nominated for the same semester
    /// - If rank is not provided, it will be auto-calculated based on existing entries
    /// </remarks>
    [HttpPost("hall-of-fame")]
    [ApiExplorerSettings(GroupName = "admin-hall-of-fame")]
    [RateLimit(permitLimit: 10, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<HallOfFameResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<HallOfFameResponseDto>>> NominateProject([FromBody] HallOfFameNominateRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<HallOfFameResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Invalid request data"
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var adminId))
        {
            adminId = 1; // Fallback for testing
        }

        var result = await _hallOfFameService.NominateProjectAsync(adminId, request);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update Hall of Fame entry
    /// </summary>
    /// <param name="id">Hall of Fame entry ID</param>
    /// <param name="request">Update request with rank and note</param>
    /// <returns>Updated Hall of Fame entry</returns>
    /// <remarks>
    /// Allows admin to update the rank or note of a Hall of Fame entry.
    /// </remarks>
    [HttpPut("hall-of-fame/{id}")]
    [ApiExplorerSettings(GroupName = "admin-hall-of-fame")]
    [RateLimit(permitLimit: 10, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<HallOfFameResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<HallOfFameResponseDto>>> UpdateHallOfFame(
        [FromRoute] int id,
        [FromBody] HallOfFameUpdateRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<HallOfFameResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Invalid request data"
            });
        }

        var result = await _hallOfFameService.UpdateHallOfFameAsync(id, request);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete Hall of Fame entry
    /// </summary>
    /// <param name="id">Hall of Fame entry ID</param>
    /// <returns>Success status</returns>
    /// <remarks>
    /// Removes a project from the Hall of Fame.
    /// This does not affect the project itself, only its Hall of Fame nomination.
    /// </remarks>
    [HttpDelete("hall-of-fame/{id}")]
    [ApiExplorerSettings(GroupName = "admin-hall-of-fame")]
    [RateLimit(permitLimit: 10, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<bool>>> DeleteHallOfFame([FromRoute] int id)
    {
        var result = await _hallOfFameService.DeleteHallOfFameAsync(id);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region Reports & Analytics APIs

    /// <summary>
    /// Get classes summary report
    /// </summary>
    /// <param name="semesterId">Optional semester ID to filter by</param>
    /// <returns>Comprehensive classes summary with statistics</returns>
    /// <remarks>
    /// Provides detailed statistics about classes including:
    /// - Total classes, active classes, classes without instructor
    /// - Average class size
    /// - Breakdown by semester with student, group, and project counts
    /// </remarks>
    [HttpGet("reports/classes-summary")]
    [ApiExplorerSettings(GroupName = "admin-reports")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ClassesSummaryReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ClassesSummaryReportDto>>> GetClassesSummary([FromQuery] int? semesterId = null)
    {
        var result = await _reportService.GetClassesSummaryAsync(semesterId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get instructors workload report
    /// </summary>
    /// <param name="semesterId">Optional semester ID to filter by</param>
    /// <returns>Detailed workload analysis for all instructors</returns>
    /// <remarks>
    /// Provides insights into instructor assignments including:
    /// - Total instructors and average classes per instructor
    /// - Instructors without classes
    /// - Detailed workload per instructor (classes, students, groups, pending work)
    /// </remarks>
    [HttpGet("reports/instructors-workload")]
    [ApiExplorerSettings(GroupName = "admin-reports")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<InstructorsWorkloadReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<InstructorsWorkloadReportDto>>> GetInstructorsWorkload([FromQuery] int? semesterId = null)
    {
        var result = await _reportService.GetInstructorsWorkloadAsync(semesterId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get students distribution report
    /// </summary>
    /// <param name="semesterId">Optional semester ID to filter by</param>
    /// <returns>Student participation and distribution analysis</returns>
    /// <remarks>
    /// Analyzes student engagement including:
    /// - Total students, students in/without groups
    /// - Group participation rate
    /// - Distribution by semester and class
    /// - Average group sizes
    /// </remarks>
    [HttpGet("reports/students-distribution")]
    [ApiExplorerSettings(GroupName = "admin-reports")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<StudentsDistributionReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<StudentsDistributionReportDto>>> GetStudentsDistribution([FromQuery] int? semesterId = null)
    {
        var result = await _reportService.GetStudentsDistributionAsync(semesterId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get projects status report
    /// </summary>
    /// <param name="semesterId">Optional semester ID to filter by</param>
    /// <returns>Comprehensive project status analysis</returns>
    /// <remarks>
    /// Provides project progress overview including:
    /// - Total projects by status (Pending, Approved, Completed, Rejected)
    /// - Completion rate
    /// - Status distribution percentages
    /// - Breakdown by semester
    /// </remarks>
    [HttpGet("reports/projects-status")]
    [ApiExplorerSettings(GroupName = "admin-reports")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ProjectsStatusReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ProjectsStatusReportDto>>> GetProjectsStatus([FromQuery] int? semesterId = null)
    {
        var result = await _reportService.GetProjectsStatusAsync(semesterId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get milestone progress report
    /// </summary>
    /// <param name="semesterId">Optional semester ID to filter by</param>
    /// <returns>Milestone completion and grading analysis</returns>
    /// <remarks>
    /// Analyzes milestone achievements including:
    /// - Total milestones, completed vs pending
    /// - Overall completion rate and average grade
    /// - Completion rates by milestone type
    /// - Progress by semester
    /// </remarks>
    [HttpGet("reports/milestone-progress")]
    [ApiExplorerSettings(GroupName = "admin-reports")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<MilestoneProgressReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<MilestoneProgressReportDto>>> GetMilestoneProgress([FromQuery] int? semesterId = null)
    {
        var result = await _reportService.GetMilestoneProgressAsync(semesterId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get grades distribution report
    /// </summary>
    /// <param name="semesterId">Optional semester ID to filter by</param>
    /// <returns>Comprehensive grade distribution analysis</returns>
    /// <remarks>
    /// Provides detailed grading statistics including:
    /// - Total graded projects, average/highest/lowest/median grades
    /// - Grade distribution by ranges (90-100, 80-89, etc.)
    /// - Top 10 performing projects
    /// - Grades breakdown by semester
    /// </remarks>
    [HttpGet("reports/grades-distribution")]
    [ApiExplorerSettings(GroupName = "admin-reports")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<GradesDistributionReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<GradesDistributionReportDto>>> GetGradesDistribution([FromQuery] int? semesterId = null)
    {
        var result = await _reportService.GetGradesDistributionAsync(semesterId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Export report with filters
    /// </summary>
    /// <param name="request">Export configuration with format and filters</param>
    /// <returns>Downloadable report file</returns>
    /// <remarks>
    /// **Note:** This endpoint is currently a placeholder.
    /// Implementation requires additional libraries:
    /// - **EPPlus** for Excel exports
    /// - **iTextSharp** or **QuestPDF** for PDF exports
    /// 
    /// Supports exporting various report types in Excel or PDF format with filters.
    /// </remarks>
    [HttpPost("reports/export")]
    [ApiExplorerSettings(GroupName = "admin-reports")]
    [RateLimit(permitLimit: 5, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ReportExportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ReportExportResponseDto>>> ExportReport([FromBody] ReportExportRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<ReportExportResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Invalid request data"
            });
        }

        var result = await _reportService.ExportReportAsync(request);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Export comprehensive semester report (Excel format)
    /// </summary>
    /// <param name="semesterId">Semester ID to export</param>
    /// <returns>Excel file with comprehensive semester information</returns>
    /// <remarks>
    /// Xu?t báo cáo toàn di?n cho m?t k? h?c bao g?m:
    /// 
    /// **Sheet 1 - T?ng Quan K? H?c:**
    /// - Thông tin k? h?c (mã, tên, n?m, h?c k?, ngày b?t ??u/k?t thúc)
    /// - Th?ng kê t?ng quan (s? l?p, sinh viên, nhóm, d? án)
    /// 
    /// **Sheet 2 - Danh Sách L?p:**
    /// - ID l?p, tên l?p, gi?ng viên ph? trách
    /// - S? sinh viên, s? nhóm, s? d? án trong m?i l?p
    /// - Tr?ng thái l?p
    /// 
    /// **Sheet 3 - Danh Sách Gi?ng Viên:**
    /// - ID, h? tên, email gi?ng viên
    /// - Các l?p ph? trách
    /// - T?ng s? sinh viên và d? án ???c qu?n lý
    /// 
    /// **Sheet 4 - Danh Sách Sinh Viên:**
    /// - ID, h? tên, email sinh viên
    /// - L?p ?ang h?c
    /// - Nhóm và d? án tham gia
    /// - Vai trò (nhóm tr??ng/thành viên)
    /// 
    /// **Sheet 5 - ?i?m Milestone:**
    /// - Chi ti?t ?i?m ?ánh giá t?ng milestone
    /// - L?p, nhóm, d? án, sinh viên
    /// - Tên milestone, tr?ng s?, ?i?m s?
    /// - Gi?ng viên ch?m và ngày ch?m
    /// 
    /// **Sheet 6 - ?i?m Cu?i K?:**
    /// - ?i?m final project t? 2 graders
    /// - ?i?m trung bình final
    /// - Ngày n?p và tr?ng thái
    /// - Chi ti?t t?ng sinh viên trong nhóm
    /// 
    /// **Sheet 7 - Tr?ng Thái Pass/Not Pass:**
    /// - T?ng ?i?m milestone và final
    /// - ?i?m t?ng k?t (40% milestone + 60% final)
    /// - Tr?ng thái d? án
    /// - K?t qu? cu?i cùng: PASS/NOT PASS
    /// - Highlight màu xanh (PASS) và ?? (NOT PASS)
    /// 
    /// **?i?u ki?n PASS:**
    /// - ?i?m t?ng k?t >= 50
    /// - Project status = "Completed"
    /// - ?ã n?p final submission
    /// 
    /// File Excel ???c format ??p v?i:
    /// - Header có màu s?c riêng cho m?i sheet
    /// - Auto-fit columns
    /// - Bold headers
    /// - Border cho các ô
    /// </remarks>
    [HttpGet("reports/semester/{semesterId}/comprehensive-export")]
    [ApiExplorerSettings(GroupName = "admin-reports")]
    [RateLimit(permitLimit: 5, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ReportExportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ReportExportResponseDto>>> ExportComprehensiveSemesterReport(
        [FromRoute] int semesterId)
    {
        var result = await _reportService.ExportComprehensiveSemesterReportAsync(semesterId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region Class Enrollment Management APIs

    /// <summary>
    /// Bulk add students to a class automatically
    /// </summary>
    /// <param name="request">Request with class ID and max members limit</param>
    /// <returns>Bulk enrollment result with added students list</returns>
    /// <remarks>
    /// This endpoint automatically finds available students (role_id = 3) who are NOT already enrolled in the class,
    /// and adds them up to the specified max members limit.
    /// 
    /// Logic:
    /// 1. Checks current enrollment count in the class
    /// 2. Calculates how many more students needed (maxMembers - currentCount)
    /// 3. Finds available students with role_id = 3 who are not in this class
    /// 4. Adds them to Class_Enrollments table
    /// 5. Returns detailed report of additions
    /// 
    /// Example:
    /// - Class currently has 20 students
    /// - Request maxMembers = 50
    /// - System will try to add 30 students automatically
    /// </remarks>
    [HttpPost("classes/bulk-add-students")]
    [ApiExplorerSettings(GroupName = "admin-class-management")]
    [RateLimit(permitLimit: 10, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<BulkAddStudentsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<BulkAddStudentsResponseDto>>> BulkAddStudents([FromBody] BulkAddStudentsRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<BulkAddStudentsResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Invalid request data"
            });
        }

        var result = await _classEnrollmentService.BulkAddStudentsAsync(request);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Import students to class from Excel file
    /// </summary>
    /// <param name="classId">Class ID to add students to</param>
    /// <param name="excelFile">Excel file (.xlsx or .xls) with Email and Status columns</param>
    /// <returns>Import result with successful and failed entries</returns>
    /// <remarks>
    /// Imports students to a class from an Excel template file.
    /// 
    /// **Excel Format Requirements:**
    /// - **Column A (Email)**: Student email address (required, must exist in system)
    /// - **Column B (Status)**: IOT course status - "Passed" or "Not Pass"
    /// 
    /// **Validation Rules:**
    /// 1. **Email must exist** in the Users table
    /// 2. **User must be a student** (role_id = 3)
    /// 3. **Student cannot already be enrolled** in this class (no duplicates)
    /// 4. **Status must be "Not Pass"** or empty/null (students with "Passed" cannot be added)
    /// 
    /// **Response Structure:**
    /// - Returns detailed results for each row
    /// - Success list: Students successfully added with their details
    /// - Failed list: Students not added with specific reason codes and messages in Vietnamese
    /// 
    /// **Failure Reason Codes:**
    /// - `EMAIL_NOT_FOUND`: Email không t?n t?i trong h? th?ng
    /// - `NOT_STUDENT`: Ng??i dùng không ph?i là sinh viên
    /// - `DUPLICATE`: Sinh viên ?ã có trong l?p
    /// - `ALREADY_PASSED`: Sinh viên ?ã hoàn thành môn IOT (Status: Passed)
    /// - `INVALID_STATUS`: Status không h?p l?
    /// 
    /// **Example Usage:**
    /// ```
    /// POST /api/admin/classes/123/import-students
    /// Content-Type: multipart/form-data
    /// 
    /// excelFile: [Excel file with Email and Status columns]
    /// ```
    /// 
    /// **Sample Excel Data:**
    /// | Email | Status |
    /// |-------|--------|
    /// | student@fpt.edu.vn | Not Pass |
    /// | student2@example.com | Passed |
    /// 
    /// In this example:
    /// - First student will be added successfully
    /// - Second student will fail with "ALREADY_PASSED" reason
    /// </remarks>
    [HttpPost("classes/{classId}/import-students")]
    [ApiExplorerSettings(GroupName = "admin-class-management")]
    [Consumes("multipart/form-data")]
    [RateLimit(permitLimit: 5, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ImportStudentsResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ImportStudentsResultDto>>> ImportStudentsFromExcel(
        [FromRoute] int classId,
        [FromForm] IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            return BadRequest(new ResultModel<ImportStudentsResultDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Excel file is required"
            });
        }

        var result = await _classEnrollmentService.ImportStudentsFromExcelAsync(classId, excelFile);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Add a specific student to a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="request">Student ID to add</param>
    /// <returns>Enrollment result</returns>
    [HttpPost("classes/{classId}/students")]
    [ApiExplorerSettings(GroupName = "admin-class-management")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<AddStudentToClassResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<AddStudentToClassResponseDto>>> AddStudentToClass(
        [FromRoute] int classId,
        [FromBody] AddStudentToClassRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<AddStudentToClassResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Invalid request data"
            });
        }

        var result = await _classEnrollmentService.AddStudentToClassAsync(classId, request.StudentId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all students in a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of enrolled students</returns>
    [HttpGet("classes/{classId}/students")]
    [ApiExplorerSettings(GroupName = "admin-class-management")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ClassStudentsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<ClassStudentsResponseDto>>> GetClassStudents([FromRoute] int classId)
    {
        var result = await _classEnrollmentService.GetClassStudentsAsync(classId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Remove a student from a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="studentId">Student ID to remove</param>
    /// <returns>Removal result</returns>
    /// <remarks>
    /// Will fail if student is a member of any group in this class.
    /// Remove student from groups first before removing from class.
    /// </remarks>
    [HttpDelete("classes/{classId}/students/{studentId}")]
    [ApiExplorerSettings(GroupName = "admin-class-management")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultModel<bool>>> RemoveStudentFromClass(
        [FromRoute] int classId,
        [FromRoute] int studentId)
    {
        var result = await _classEnrollmentService.RemoveStudentFromClassAsync(classId, studentId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region Class Grader Management APIs

    /// <summary>
    /// Get all graders assigned to a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of assigned graders with statistics</returns>
    /// <remarks>
    /// Returns all instructors assigned to grade final projects in the specified class.
    /// 
    /// Response includes:
    /// - Grader assignment details
    /// - Total final submissions in the class
    /// - Graded count by each instructor
    /// - Pending grades count
    /// - Assignment date and who assigned them
    /// </remarks>
    [HttpGet("classes/{classId}/graders")]
    [ApiExplorerSettings(GroupName = "admin-grader-management")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<List<ClassGraderDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<List<ClassGraderDetailDto>>>> GetClassGraders([FromRoute] int classId)
    {
        var result = await _adminClassGraderService.GetClassGradersAsync(classId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Assign an instructor to grade projects in a class
    /// </summary>
    /// <param name="request">Assignment request with class and instructor IDs</param>
    /// <returns>Created grader assignment</returns>
    /// <remarks>
    /// Assigns an instructor as a grader for a class's final projects.
    /// Multiple instructors can be assigned to the same class.
    /// 
    /// Requirements:
    /// - Instructor must have role_id = 2 (Instructor)
    /// - Cannot assign the same instructor twice to the same class (unless previously deactivated)
    /// - If a deactivated assignment exists, it will be reactivated
    /// 
    /// Actions performed:
    /// - Creates record in Class_Graders table
    /// - Sends notification to instructor
    /// - Returns assignment details with statistics
    /// </remarks>
    [HttpPost("graders/assign")]
    [ApiExplorerSettings(GroupName = "admin-grader-management")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ClassGraderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ClassGraderDetailDto>>> AssignGrader(
        [FromBody] AssignClassGraderRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<ClassGraderDetailDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Invalid request data"
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var adminId))
        {
            adminId = 1; // Fallback for testing
        }

        var result = await _adminClassGraderService.AssignGraderAsync(request, adminId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Bulk assign multiple graders to a class
    /// </summary>
    /// <param name="request">Bulk assignment request with instructor IDs</param>
    /// <returns>Bulk assignment result with individual outcomes</returns>
    /// <remarks>
    /// Assigns multiple instructors as graders for a class in a single operation.
    /// 
    /// Features:
    /// - Processes each instructor independently
    /// - Returns detailed results for each assignment attempt
    /// - Continues processing even if some assignments fail
    /// - Reactivates deactivated assignments if they exist
    /// - Sends notifications to all successfully assigned instructors
    /// 
    /// Response includes:
    /// - Total success and failure counts
    /// - Detailed results for each instructor
    /// - Error messages for failures
    /// </remarks>
    [HttpPost("graders/bulk-assign")]
    [ApiExplorerSettings(GroupName = "admin-grader-management")]
    [RateLimit(permitLimit: 10, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<BulkAssignGradersResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<BulkAssignGradersResponseDto>>> BulkAssignGraders(
        [FromBody] BulkAssignGradersRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<BulkAssignGradersResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Invalid request data"
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var adminId))
        {
            adminId = 1; // Fallback for testing
        }

        var result = await _adminClassGraderService.BulkAssignGradersAsync(request, adminId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Remove grader assignment from a class
    /// </summary>
    /// <param name="graderId">Grader ID to remove</param>
    /// <returns>Success status</returns>
    /// <remarks>
    /// Removes an instructor's grader assignment from a class.
    /// 
    /// Behavior:
    /// - If instructor has already submitted grades: Assignment is **deactivated** (not deleted)
    /// - If instructor has no grades: Assignment is **permanently deleted**
    /// - Sends notification to instructor about removal
    /// 
    /// This prevents data integrity issues with existing grades.
    /// </remarks>
    [HttpDelete("graders/{graderId}")]
    [ApiExplorerSettings(GroupName = "admin-grader-management")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<bool>>> RemoveGrader([FromRoute] int graderId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var adminId))
        {
            adminId = 1; // Fallback for testing
        }

        var result = await _adminClassGraderService.RemoveGraderAsync(graderId, adminId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Toggle grader active status
    /// </summary>
    /// <param name="graderId">Grader ID</param>
    /// <param name="isActive">New active status</param>
    /// <returns>Updated grader assignment</returns>
    /// <remarks>
    /// Activates or deactivates a grader assignment without deleting it.
    /// 
    /// Use cases:
    /// - Temporarily disable a grader without losing assignment history
    /// - Reactivate a previously deactivated grader
    /// - Manage grader availability without affecting existing grades
    /// 
    /// Sends notification to instructor about status change.
    /// </remarks>
    [HttpPut("graders/{graderId}/status")]
    [ApiExplorerSettings(GroupName = "admin-grader-management")]
    [RateLimit(permitLimit: 20, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ClassGraderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ClassGraderDetailDto>>> UpdateGraderStatus(
        [FromRoute] int graderId,
        [FromQuery] bool isActive)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var adminId))
        {
            adminId = 1; // Fallback for testing
        }

        var result = await _adminClassGraderService.UpdateGraderStatusAsync(graderId, isActive, adminId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all grader assignments across all classes
    /// </summary>
    /// <param name="instructorId">Optional filter by instructor</param>
    /// <param name="classId">Optional filter by class</param>
    /// <param name="isActive">Optional filter by active status</param>
    /// <returns>List of all grader assignments with statistics</returns>
    /// <remarks>
    /// Returns comprehensive overview of all grader assignments system-wide.
    /// 
    /// Filters:
    /// - **instructorId**: Get all classes where specific instructor is a grader
    /// - **classId**: Get all graders for specific class
    /// - **isActive**: Filter by active/inactive status
    /// 
    /// Response includes:
    /// - Assignment details
    /// - Workload statistics (total submissions, graded, pending)
    /// - Completion percentages
    /// - Semester and class information
    /// 
    /// Useful for:
    /// - Admin dashboard
    /// - Workload balancing
    /// - Instructor assignment overview
    /// </remarks>
    [HttpGet("graders")]
    [ApiExplorerSettings(GroupName = "admin-grader-management")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<List<ClassGraderSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<List<ClassGraderSummaryDto>>>> GetAllGraderAssignments(
        [FromQuery] int? instructorId = null,
        [FromQuery] int? classId = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _adminClassGraderService.GetAllGraderAssignmentsAsync(instructorId, classId, isActive);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get comprehensive grading statistics for a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>Detailed grading statistics and workload analysis</returns>
    /// <remarks>
    /// Provides comprehensive grading overview for a class including:
    /// 
    /// **Grader Information:**
    /// - Total and active graders
    /// - Individual workload for each grader
    /// - Completion percentages
    /// - Average grades given by each grader
    /// 
    /// **Project Statistics:**
    /// - Total projects, approved projects
    /// - Projects with final submissions
    /// 
    /// **Grading Statistics:**
    /// - Total grades submitted
    /// - Fully graded projects (all graders completed)
    /// - Partially graded projects (some graders completed)
    /// - Ungraded projects
    /// - Average, highest, lowest grades
    /// - Overall grading completion percentage
    /// 
    /// Useful for:
    /// - Monitoring grading progress
    /// - Identifying bottlenecks
    /// - Balancing workload
    /// - Quality assurance
    /// </remarks>
    [HttpGet("classes/{classId}/grading-statistics")]
    [ApiExplorerSettings(GroupName = "admin-grader-management")]
    [RateLimit(permitLimit: 30, windowSeconds: 60)]
    [ProducesResponseType(typeof(ResultModel<ClassGradingStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ClassGradingStatisticsDto>>> GetClassGradingStatistics(
        [FromRoute] int classId)
    {
        var result = await _adminClassGraderService.GetClassGradingStatisticsAsync(classId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion
}
