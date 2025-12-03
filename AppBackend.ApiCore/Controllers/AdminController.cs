using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.AdminDashboard;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Attributes;
using AppBackend.Services.Services.HallOfFame;
using AppBackend.Services.Services.AdminReport;
using AppBackend.Services.Services.ClassEnrollment;
using AppBackend.Services.Services.StudentGrade;
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

    public AdminController(
        IAdminDashboardService dashboardService,
        IHallOfFameService hallOfFameService,
        IAdminReportService reportService,
        IClassEnrollmentService classEnrollmentService,
        IStudentGradeService studentGradeService)
    {
        _dashboardService = dashboardService;
        _hallOfFameService = hallOfFameService;
        _reportService = reportService;
        _classEnrollmentService = classEnrollmentService;
        _studentGradeService = studentGradeService;
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
    /// Add a specific student to a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="request">Student ID to add</param>
    /// <returns>Enrollment result</returns>
    [HttpPost("classes/{classId}/students")]
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
}
