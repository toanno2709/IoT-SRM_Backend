using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.Class;
using AppBackend.Services.Services.Project;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.Announcement;
using AppBackend.Services.Services.MilestoneGrading;
using AppBackend.Services.Services.ClassStats;
using AppBackend.Services.Services.Group;
using AppBackend.Services.Services.TopicProposal;
using AppBackend.Services.Services.InstructorDashboard;
using AppBackend.Services.Services.GroupManagement;
using AppBackend.Services.Services.FinalProject;
using AppBackend.Services.Services.ClassConfig;
using AppBackend.Services.Services.InstructorSubmissionView;
using AppBackend.Services.Services.ClassEnrollment;
using AppBackend.Services.Services.StudentGrade;
using AppBackend.Services.Services.ProjectTemplate;
using AppBackend.Services.Services.ClassGrader;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppBackend.ApiCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstructorController : ControllerBase
{
    private readonly IClassService _classService;
    private readonly IProjectService _projectService;
    private readonly IAnnouncementService _announcementService;
    private readonly IClassStatsService _classStatsService;
    private readonly IGroupService _groupService;
    private readonly IMilestoneGradingService _milestoneGradingService;
    private readonly ITopicProposalService _topicProposalService;
    private readonly IInstructorDashboardService _dashboardService;
    private readonly IGroupManagementService _groupManagementService;
    private readonly IFinalProjectService _finalProjectService;
    private readonly IClassConfigService _classConfigService;
    private readonly IInstructorSubmissionViewService _submissionViewService;
    private readonly IClassEnrollmentService _classEnrollmentService;
    private readonly IStudentGradeService _studentGradeService;
    private readonly IProjectTemplateService _templateService;
    private readonly IClassGraderService _classGraderService;

    public InstructorController(
        IClassService classService, 
        IProjectService projectService, 
        IAnnouncementService announcementService, 
        IClassStatsService classStatsService, 
        IGroupService groupService, 
        IMilestoneGradingService milestoneGradingService, 
        ITopicProposalService topicProposalService, 
        IInstructorDashboardService dashboardService, 
        IGroupManagementService groupManagementService,
        IFinalProjectService finalProjectService,
        IClassConfigService classConfigService,
        IInstructorSubmissionViewService submissionViewService,
        IClassEnrollmentService classEnrollmentService,
        IStudentGradeService studentGradeService,
        IProjectTemplateService templateService,
        IClassGraderService classGraderService)
    {
        _classService = classService;
        _projectService = projectService;
        _announcementService = announcementService;
        _classStatsService = classStatsService;
        _groupService = groupService;
        _milestoneGradingService = milestoneGradingService;
        _topicProposalService = topicProposalService;
        _dashboardService = dashboardService;
        _groupManagementService = groupManagementService;
        _finalProjectService = finalProjectService;
        _classConfigService = classConfigService;
        _submissionViewService = submissionViewService;
        _classEnrollmentService = classEnrollmentService;
        _studentGradeService = studentGradeService;
        _templateService = templateService;
        _classGraderService = classGraderService;
    }

    /// <summary>
    /// Lấy dashboard overview cho instructor (tổng quan)
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ResultModel<InstructorDashboardResponseDto>>> GetDashboard()
    {
        // Get instructor ID from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int instructorId;
        
        if (!int.TryParse(userIdClaim, out instructorId))
        {
            // Fallback for testing - using instructorId = 2 (same as classes endpoint)
            instructorId = 2;
        }
        
        var result = await _dashboardService.GetDashboardAsync(instructorId);
        
        if (result.IsSuccess)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Lấy danh sách thông báo đã gửi bởi giảng viên hiện tại
    /// </summary>
    [HttpGet("announcements")]
    public async Task<ActionResult<ResultModel<List<AnnouncementResponseDto>>>> GetSentAnnouncements()
    {
        // TODO: Lấy admin/instructor id từ JWT
        var adminUserId = 1;
        var result = await _announcementService.GetAnnouncementsByAdminAsync(adminUserId);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Get all classes assigned to the current instructor
    /// </summary>
    /// <returns>List of assigned classes</returns>
    [HttpGet("classes")]
    public async Task<ActionResult<ResultModel<List<ClassResponseDto>>>> GetAssignedClasses()
    {
        // Get instructor ID from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int instructorId;
        
        if (!int.TryParse(userIdClaim, out instructorId))
        {
            // Fallback for testing - using instructorId = 2 (has multiple classes in database)
            instructorId = 2;
        }
        
        var result = await _classService.GetAssignedClassesAsync(instructorId);
        
        if (result.IsSuccess)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Chấm điểm milestone (UPDATED - dùng MilestoneEvaluation)
    /// </summary>
    [HttpPost("milestones/grade")]
    public async Task<ActionResult<ResultModel<MilestoneGradeResponseDto>>> GradeMilestone([FromBody] MilestoneGradeRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<MilestoneGradeResponseDto> { IsSuccess = false, Message = "Invalid request" });
        }
        var result = await _milestoneGradingService.GradeMilestoneAsync(request);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Lấy tất cả điểm milestone của một project
    /// </summary>
    [HttpGet("projects/{projectId}/grades")]
    public async Task<ActionResult<ResultModel<List<MilestoneGradeResponseDto>>>> GetProjectGrades([FromRoute] int projectId)
    {
        var result = await _milestoneGradingService.GetGradesByProjectAsync(projectId);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Lấy danh sách topic proposal chờ duyệt
    /// </summary>
    [HttpGet("pending-proposals")]
    public async Task<ActionResult<ResultModel<List<TopicProposalResponseDto>>>> GetPendingProposals()
    {
        // TODO: lấy instructorId từ JWT
        var instructorId = 1;
        var result = await _topicProposalService.GetPendingProposalsAsync(instructorId);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Duyệt topic proposal: Approve/Revision/Reject
    /// </summary>
    [HttpPost("proposals/{submissionId}/review")]
    public async Task<ActionResult<ResultModel<TopicProposalReviewResponseDto>>> ReviewProposal(
        [FromRoute] int submissionId, 
        [FromBody] TopicProposalReviewRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<TopicProposalReviewResponseDto> 
            { 
                IsSuccess = false, 
                Message = "Invalid request" 
            });
        }

        // TODO: lấy instructorId từ JWT
        var instructorId = 1;
        var result = await _topicProposalService.ReviewProposalAsync(instructorId, submissionId, request);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Gửi thông báo mới (giảng viên)
    /// </summary>
    [HttpPost("announcements")]
    public async Task<ActionResult<ResultModel<AnnouncementResponseDto>>> CreateAnnouncement([FromBody] AnnouncementCreateRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<AnnouncementResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                Data = null
            });
        }

        // TODO: Lấy admin id từ JWT
        var adminUserId = 1;
        var result = await _announcementService.CreateAnnouncementAsync(adminUserId, request);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Get all groups in a class
    /// </summary>
    /// <param name="classId">Class Id</param>
    /// <returns>List of groups</returns>
    [HttpGet("classes/{classId}/groups")]
    public async Task<ActionResult<ResultModel<List<GroupResponseDto>>>> GetGroupsInClass([FromRoute] int classId)
    {
        var result = await _groupService.GetGroupsByClassAsync(classId);
        if (result.IsSuccess)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }

    /// <summary>
    /// Cập nhật thông tin group (tên, mô tả)
    /// </summary>
    [HttpPut("groups/{groupId}")]
    public async Task<ActionResult<ResultModel<GroupResponseDto>>> UpdateGroup(
        [FromRoute] int groupId,
        [FromBody] GroupUpdateRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<GroupResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                Data = null
            });
        }

        var result = await _groupManagementService.UpdateGroupInfoAsync(groupId, request);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Thêm member vào group
    /// </summary>
    [HttpPost("groups/{groupId}/members")]
    public async Task<ActionResult<ResultModel<GroupMemberOperationResponseDto>>> AddGroupMember(
        [FromRoute] int groupId,
        [FromBody] AddGroupMemberRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<GroupMemberOperationResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                Data = null
            });
        }

        var result = await _groupManagementService.AddMemberAsync(groupId, request);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Xóa member khỏi group
    /// </summary>
    [HttpDelete("groups/{groupId}/members/{userId}")]
    public async Task<ActionResult<ResultModel<GroupMemberOperationResponseDto>>> RemoveGroupMember(
        [FromRoute] int groupId,
        [FromRoute] int userId)
    {
        var result = await _groupManagementService.RemoveMemberAsync(groupId, userId);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Cập nhật role của member trong group
    /// </summary>
    [HttpPut("groups/{groupId}/members/{userId}/role")]
    public async Task<ActionResult<ResultModel<GroupMemberOperationResponseDto>>> UpdateMemberRole(
        [FromRoute] int groupId,
        [FromRoute] int userId,
        [FromBody] UpdateMemberRoleRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<GroupMemberOperationResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                Data = null
            });
        }

        var result = await _groupManagementService.UpdateMemberRoleAsync(groupId, userId, request);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Lấy thống kê lớp học (submission rate, average score, etc.)
    /// </summary>
    [HttpGet("classes/{classId}/stats")]
    public async Task<ActionResult<ResultModel<ClassStatsResponseDto>>> GetClassStats([FromRoute] int classId)
    {
        var result = await _classStatsService.GetClassStatsAsync(classId);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Grade final project submission (Instructor only)
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="request">Grade and feedback</param>
    /// <returns>Graded submission</returns>
    [HttpPost("projects/{projectId}/final-grade")]
    public async Task<ActionResult<ResultModel<FinalProjectSubmissionResponseDto>>> GradeFinalProject(
        [FromRoute] int projectId,
        [FromBody] FinalProjectGradeRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<FinalProjectSubmissionResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request"
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            // TODO: Get from JWT - temporary fallback
            instructorId = 1;
        }

        var result = await _finalProjectService.GradeFinalProjectAsync(projectId, request, instructorId);
        
        if (result.IsSuccess) 
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get class configuration (max groups, member limits, deadlines)
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>Class configuration</returns>
    [HttpGet("classes/{classId}/config")]
    public async Task<ActionResult<ResultModel<ClassConfigResponseDto>>> GetClassConfig([FromRoute] int classId)
    {
        var result = await _classConfigService.GetConfigAsync(classId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update class configuration (max groups, member limits, deadlines)
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="request">Configuration update data</param>
    /// <returns>Updated configuration</returns>
    /// <remarks>
    /// Allows instructor to configure:
    /// - Max groups allowed in class
    /// - Min/max members per group
    /// - Group formation deadline
    /// - Whether students can create groups
    /// </remarks>
    [HttpPut("classes/{classId}/config")]
    public async Task<ActionResult<ResultModel<ClassConfigResponseDto>>> UpdateClassConfig(
        [FromRoute] int classId,
        [FromBody] ClassConfigUpdateDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<ClassConfigResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request"
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            // TODO: Get from JWT - temporary fallback
            instructorId = 1;
        }

        var result = await _classConfigService.UpdateConfigAsync(classId, request, instructorId);
        
        if (result.IsSuccess)
            return Ok(result);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all submissions for a specific milestone (all groups)
    /// </summary>
    /// <param name="milestoneId">Milestone ID</param>
    /// <param name="isGraded">Filter by grading status (optional)</param>
    /// <param name="isLate">Filter by late submissions (optional)</param>
    /// <param name="sortBy">Sort field: SubmittedAt, GroupName, Grade (optional)</param>
    /// <param name="sortOrder">Sort order: asc or desc (optional)</param>
    /// <returns>List of all submissions for the milestone</returns>
    [HttpGet("milestones/{milestoneId}/submissions")]
    public async Task<ActionResult<ResultModel<List<InstructorSubmissionViewDto>>>> GetMilestoneSubmissions(
        [FromRoute] int milestoneId,
        [FromQuery] bool? isGraded = null,
        [FromQuery] bool? isLate = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 1;
        }

        var filter = new SubmissionFilterDto
        {
            IsGraded = isGraded,
            IsLate = isLate,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _submissionViewService.GetSubmissionsByMilestoneAsync(milestoneId, instructorId, filter);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all submissions in a class, grouped by milestone
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="milestoneId">Filter by milestone ID (optional)</param>
    /// <param name="isGraded">Filter by grading status (optional)</param>
    /// <returns>Class submission overview with statistics</returns>
    [HttpGet("classes/{classId}/submissions")]
    public async Task<ActionResult<ResultModel<ClassSubmissionOverviewDto>>> GetClassSubmissions(
        [FromRoute] int classId,
        [FromQuery] int? milestoneId = null,
        [FromQuery] bool? isGraded = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 1;
        }

        var filter = new SubmissionFilterDto
        {
            MilestoneDefId = milestoneId,
            IsGraded = isGraded
        };

        var result = await _submissionViewService.GetClassSubmissionsAsync(classId, instructorId, filter);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get detailed files for a specific submission (for grading)
    /// </summary>
    /// <param name="submissionId">Submission ID</param>
    /// <returns>Submission details with all files and download URLs</returns>
    [HttpGet("submissions/{submissionId}/files")]
    public async Task<ActionResult<ResultModel<InstructorSubmissionFilesDto>>> GetSubmissionFiles(
        [FromRoute] int submissionId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 1;
        }

        var result = await _submissionViewService.GetSubmissionFilesAsync(submissionId, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Debug endpoint to check database connection and data integrity
    /// </summary>
    [HttpGet("debug/database-check")]
    [AllowAnonymous]
    public async Task<IActionResult> DebugDatabaseCheck()
    {
        // Inject DbContext for debugging
        var _context = HttpContext.RequestServices.GetRequiredService<AppBackend.BusinessObjects.Data.IotShowroomContext>();
        
        var instructors = await _context.Users
            .Where(u => u.UserId == 2 || u.UserId == 3 || u.UserId == 9)
            .Select(u => new { u.UserId, u.FullName, u.Email, u.RoleId })
            .ToListAsync();
        
        var semesters = await _context.Semesters
            .Where(s => s.SemesterId == 1 || s.SemesterId == 2 || 
                        s.SemesterId == 4 || s.SemesterId == 7 || s.SemesterId == 9)
            .Select(s => new { s.SemesterId, s.Name, s.Code })
            .ToListAsync();
        
        var classes = await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.Semester)
            .Where(c => c.InstructorId == 2)
            .Select(c => new
            {
                c.ClassId,
                c.ClassName,
                c.InstructorId,
                InstructorName = c.Instructor != null ? c.Instructor.FullName : "NULL_IN_DB",
                InstructorExists = c.Instructor != null,
                c.SemesterId,
                SemesterName = c.Semester != null ? c.Semester.Name : "NULL_IN_DB",
                SemesterExists = c.Semester != null
            })
            .ToListAsync();
        
        return Ok(new
        {
            DatabaseName = _context.Database.GetDbConnection().Database,
            Instructors = instructors,
            InstructorsCount = instructors.Count,
            Semesters = semesters,
            SemestersCount = semesters.Count,
            Classes = classes,
            ClassesCount = classes.Count,
            Note = "Check if navigation properties are null - indicates missing FK records"
        });
    }

    /// <summary>
    /// Debug endpoint to check submission data and relationships
    /// </summary>
    [HttpGet("debug/submissions/{milestoneId}")]
    [AllowAnonymous]
    public async Task<IActionResult> DebugSubmissionData([FromRoute] int milestoneId)
    {
        var _context = HttpContext.RequestServices.GetRequiredService<AppBackend.BusinessObjects.Data.IotShowroomContext>();
        
        // Check milestone
        var milestone = await _context.ProjectMilestones
            .FirstOrDefaultAsync(m => m.MilestoneId == milestoneId);
        
        // Check submissions with all related data
        var submissions = await _context.MilestoneSubmissions
            .Include(s => s.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g!.Class)
            .Include(s => s.SubmissionFiles)
            .Include(s => s.MilestoneDef)
            .Where(s => s.MilestoneDefId == milestoneId)
            .Select(s => new
            {
                SubmissionId = s.SubmissionId,
                ProjectId = s.ProjectId,
                ProjectTitle = s.Project != null ? s.Project.Title : "NULL",
                GroupId = s.Project != null && s.Project.Group != null ? s.Project.Group.GroupId : 0,
                GroupName = s.Project != null && s.Project.Group != null ? s.Project.Group.GroupName : "NULL",
                ClassId = s.Project != null && s.Project.Group != null && s.Project.Group.Class != null ? s.Project.Group.Class.ClassId : 0,
                ClassName = s.Project != null && s.Project.Group != null && s.Project.Group.Class != null ? s.Project.Group.Class.ClassName : "NULL",
                InstructorId = s.Project != null && s.Project.Group != null && s.Project.Group.Class != null ? s.Project.Group.Class.InstructorId : null,
                FileCount = s.SubmissionFiles != null ? s.SubmissionFiles.Count : 0,
                LastSubmittedAt = s.LastSubmittedAt,
                VersionNo = s.LastVersionNo
            })
            .ToListAsync();
        
        // Check all instructors
        var instructors = await _context.Users
            .Where(u => u.RoleId == 2) // Assuming role_id 2 is instructor
            .Select(u => new { u.UserId, u.FullName, u.Email })
            .ToListAsync();
        
        // Check classes
        var classes = await _context.Classes
            .Include(c => c.Instructor)
            .Select(c => new
            {
                c.ClassId,
                c.ClassName,
                c.InstructorId,
                InstructorName = c.Instructor != null ? c.Instructor.FullName : "NULL"
            })
            .ToListAsync();
        
        return Ok(new
        {
            MilestoneId = milestoneId,
            MilestoneExists = milestone != null,
            MilestoneTitle = milestone?.Title,
            TotalSubmissions = submissions.Count,
            Submissions = submissions,
            Instructors = instructors,
            Classes = classes,
            Note = "Check if submissions have valid project->group->class->instructor chain"
        });
    }

    /// <summary>
    /// Get all submissions that need grading (across all classes or specific class)
    /// </summary>
    /// <param name="classId">Filter by class ID (optional)</param>
    /// <returns>List of submissions pending grading</returns>
    [HttpGet("submissions/pending-grading")]
    public async Task<ActionResult<ResultModel<List<InstructorSubmissionViewDto>>>> GetPendingGradingSubmissions(
        [FromQuery] int? classId = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 1;
        }

        var result = await _submissionViewService.GetPendingGradingSubmissionsAsync(instructorId, classId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update project status with comment (Instructor only)
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="request">Status and comment</param>
    /// <returns>Updated project status details</returns>
    /// <remarks>
    /// Allows instructor to update project status and add comment. 
    /// Students in the group will be notified and can view the comment.
    /// 
    /// Common status values:
    /// - Approved: Project is approved to proceed
    /// - Rejected: Project is rejected  
    /// - Revision: Project needs changes
    /// - In Progress: Project is actively being worked on
    /// - Completed: Project is finished
    /// </remarks>
    [HttpPut("projects/{projectId}/status")]
    public async Task<ActionResult<ResultModel<UpdateProjectStatusResponseDto>>> UpdateProjectStatus(
        [FromRoute] int projectId,
        [FromBody] UpdateProjectStatusRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<UpdateProjectStatusResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 1;
        }

        var result = await _projectService.UpdateProjectStatusAsync(projectId, request, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get unassigned students in a class (students without groups)
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="q">Optional search query for student name or email</param>
    /// <returns>List of students not assigned to any group</returns>
    /// <remarks>
    /// Returns students who are enrolled in the class but are not members of any group.
    /// Useful for instructors to identify which students need group assignments.
    /// 
    /// Query parameter 'q' allows filtering by student name or email (case-insensitive).
    /// </remarks>
    [HttpGet("classes/{classId}/unassigned-students")]
    public async Task<ActionResult<ResultModel<UnassignedStudentsResponseDto>>> GetUnassignedStudents(
        [FromRoute] int classId,
        [FromQuery] string? q = null)
    {
        var result = await _classEnrollmentService.GetUnassignedStudentsAsync(classId, q);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all student grades in a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>Comprehensive grades report for all students in the class</returns>
    /// <remarks>
    /// Returns detailed grade information for all students enrolled in the class:
    /// - Student information (ID, name, email)
    /// - Group and project information
    /// - Individual milestone grades
    /// - Overall calculated grade
    /// - Project status
    /// 
    /// Only the instructor assigned to the class can access this endpoint.
    /// </remarks>
    [HttpGet("classes/{classId}/grades")]
    [ProducesResponseType(typeof(ResultModel<ClassGradesReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<ClassGradesReportDto>>> GetClassGrades([FromRoute] int classId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 1; // Fallback for testing
        }

        var result = await _studentGradeService.GetClassGradesAsync(classId, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Export class grades to Excel file
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="includeMilestoneDetails">Include individual milestone grades (default: true)</param>
    /// <param name="includeFeedback">Include feedback comments (default: false)</param>
    /// <returns>Excel file with all student grades</returns>
    /// <remarks>
    /// Downloads an Excel file containing:
    /// - Class information (name, semester, instructor)
    /// - All enrolled students
    /// - Group assignments
    /// - Project titles
    /// - Individual milestone grades (if includeMilestoneDetails = true)
    /// - Overall calculated grades
    /// - Project status
    /// 
    /// The Excel file includes:
    /// - Color-coded grades (green: ≥80, yellow: 50-79, red: &lt;50)
    /// - Auto-fitted columns
    /// - Formatted headers
    /// - Summary statistics
    /// 
    /// Example filename: ClassGrades_SE1234_20250120_143025.xlsx
    /// </remarks>
    [HttpGet("classes/{classId}/grades/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportClassGradesToExcel(
        [FromRoute] int classId,
        [FromQuery] bool includeMilestoneDetails = true,
        [FromQuery] bool includeFeedback = false)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 1; // Fallback for testing
        }

        var result = await _studentGradeService.ExportClassGradesToExcelAsync(
            classId, 
            includeMilestoneDetails, 
            includeFeedback, 
            instructorId);

        if (result.IsSuccess && result.Data != null)
        {
            return File(
                result.Data.FileContent,
                result.Data.ContentType,
                result.Data.FileName);
        }

        return StatusCode(result.StatusCode, new
        {
            isSuccess = false,
            message = result.Message
        });
    }

    #region Project Templates

    /// <summary>
    /// Create a new project template for a class
    /// </summary>
    /// <param name="dto">Template creation data with milestones</param>
    /// <returns>Created template with details</returns>
    /// <remarks>
    /// Allows instructor to create a project template that students can register to.
    /// 
    /// Features:
    /// - Define project title, description, and components
    /// - Set max groups limit (null = unlimited)
    /// - Define milestones with order, weight, and duration
    /// - Students will see available templates and can register their groups
    /// 
    /// When a group registers:
    /// - System automatically creates a project from the template
    /// - All milestones are created with calculated due dates
    /// - registered_count is incremented
    /// </remarks>
    [HttpPost("templates")]
    public async Task<ActionResult<ResultModel<ProjectTemplateResponseDto>>> CreateTemplate(
        [FromBody] CreateProjectTemplateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<ProjectTemplateResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2; // Fallback
        }

        var result = await _templateService.CreateTemplateAsync(dto, instructorId);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetTemplateById), new { templateId = result.Data!.TemplateId }, result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all templates for a class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of templates with statistics</returns>
    [HttpGet("classes/{classId}/templates")]
    public async Task<ActionResult<ResultModel<List<ProjectTemplateResponseDto>>>> GetTemplatesByClass(
        [FromRoute] int classId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2;
        }

        var result = await _templateService.GetTemplatesByClassIdAsync(classId, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get template details by ID
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>Template details with milestones</returns>
    [HttpGet("templates/{templateId}")]
    public async Task<ActionResult<ResultModel<ProjectTemplateResponseDto>>> GetTemplateById(
        [FromRoute] int templateId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2;
        }

        var result = await _templateService.GetTemplateByIdAsync(templateId, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update template information
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated template</returns>
    [HttpPut("templates/{templateId}")]
    public async Task<ActionResult<ResultModel<ProjectTemplateResponseDto>>> UpdateTemplate(
        [FromRoute] int templateId,
        [FromBody] UpdateProjectTemplateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<ProjectTemplateResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2;
        }

        var result = await _templateService.UpdateTemplateAsync(templateId, dto, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>Success status</returns>
    /// <remarks>
    /// Can only delete templates with no active registrations
    /// </remarks>
    [HttpDelete("templates/{templateId}")]
    public async Task<ActionResult<ResultModel<bool>>> DeleteTemplate([FromRoute] int templateId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2;
        }

        var result = await _templateService.DeleteTemplateAsync(templateId, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all registrations for a template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>List of groups registered to this template</returns>
    [HttpGet("templates/{templateId}/registrations")]
    public async Task<ActionResult<ResultModel<List<TemplateRegistrationListDto>>>> GetTemplateRegistrations(
        [FromRoute] int templateId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2;
        }

        var result = await _templateService.GetTemplateRegistrationsAsync(templateId, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get template statistics
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <returns>Statistics including registered count, available slots, etc.</returns>
    [HttpGet("templates/{templateId}/statistics")]
    public async Task<ActionResult<ResultModel<TemplateStatisticsDto>>> GetTemplateStatistics(
        [FromRoute] int templateId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2;
        }

        var result = await _templateService.GetTemplateStatisticsAsync(templateId, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion

    #region Class Grading Assignment

    /// <summary>
    /// Get all classes where instructor is assigned as grader
    /// </summary>
    /// <returns>List of classes with grading statistics</returns>
    /// <remarks>
    /// Returns classes where the current instructor is assigned to grade final projects.
    /// This is separate from the main instructor assignment - multiple instructors can
    /// be assigned to grade projects in the same class.
    /// 
    /// Response includes:
    /// - Class information
    /// - Total projects and approved projects
    /// - Projects with final submissions
    /// - Projects graded by this instructor
    /// - Projects pending this instructor's grade
    /// </remarks>
    [HttpGet("grading/classes")]
    [ProducesResponseType(typeof(ResultModel<List<GradingClassDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<List<GradingClassDto>>>> GetGradingClasses()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2; // Fallback for testing
        }

        var result = await _classGraderService.GetGradingClassesAsync(instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all approved projects in a class for grading
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of approved projects with grading status</returns>
    /// <remarks>
    /// Returns all projects with status "Approved" in the specified class.
    /// Only accessible to instructors assigned to grade this class.
    /// 
    /// Response includes:
    /// - Project and group information
    /// - Final submission status
    /// - Grading status (has my grade, average grade, total grades)
    /// - Whether submission is pending this instructor's grade
    /// </remarks>
    [HttpGet("grading/classes/{classId}/projects")]
    [ProducesResponseType(typeof(ResultModel<List<ApprovedProjectForGradingDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<List<ApprovedProjectForGradingDto>>>> GetApprovedProjectsForGrading(
        [FromRoute] int classId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2; // Fallback for testing
        }

        var result = await _classGraderService.GetApprovedProjectsForGradingAsync(classId, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get detailed final submission for grading
    /// </summary>
    /// <param name="finalSubmissionId">Final submission ID</param>
    /// <returns>Detailed submission with all files and grades</returns>
    /// <remarks>
    /// Returns detailed information about a final submission for grading purposes.
    /// Only accessible to instructors assigned to grade the class.
    /// 
    /// Response includes:
    /// - All submission files and URLs
    /// - Group members
    /// - All grades from all assigned instructors
    /// - Current instructor's grade (if already graded)
    /// - Average grade
    /// </remarks>
    [HttpGet("grading/submissions/{finalSubmissionId}")]
    [ProducesResponseType(typeof(ResultModel<GraderFinalSubmissionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<GraderFinalSubmissionDetailDto>>> GetFinalSubmissionForGrading(
        [FromRoute] int finalSubmissionId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2; // Fallback for testing
        }

        var result = await _classGraderService.GetFinalSubmissionForGradingAsync(finalSubmissionId, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Grade or update grade for final submission
    /// </summary>
    /// <param name="finalSubmissionId">Final submission ID</param>
    /// <param name="request">Grade and feedback</param>
    /// <returns>Grading result with average grade from all instructors</returns>
    /// <remarks>
    /// Allows assigned instructor to grade or update their grade for a final submission.
    /// 
    /// Multiple instructors can grade the same submission independently.
    /// The system automatically calculates the average grade from all instructor grades.
    /// 
    /// Actions performed:
    /// - Creates or updates instructor's grade in Final_Submission_Grades table
    /// - Trigger automatically recalculates average and updates Final_Project_Submissions.grade
    /// - Sends notification to all group members
    /// 
    /// Example: If 2 instructors grade the same project as 85 and 90, 
    /// the average grade will be 87.5
    /// </remarks>
    [HttpPost("grading/submissions/{finalSubmissionId}/grade")]
    [ProducesResponseType(typeof(ResultModel<GraderFinalProjectGradeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResultModel<GraderFinalProjectGradeResponseDto>>> GradeFinalSubmission(
        [FromRoute] int finalSubmissionId,
        [FromBody] GraderFinalProjectGradeRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<GraderFinalProjectGradeResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var instructorId))
        {
            instructorId = 2; // Fallback for testing
        }

        var result = await _classGraderService.GradeFinalSubmissionAsync(finalSubmissionId, request, instructorId);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(result.StatusCode, result);
    }

    #endregion
}

