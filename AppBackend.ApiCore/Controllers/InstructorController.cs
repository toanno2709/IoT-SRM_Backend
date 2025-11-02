using Microsoft.AspNetCore.Mvc;
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
using System.Security.Claims;

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
        IInstructorSubmissionViewService submissionViewService)
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
    }

    /// <summary>
    /// Lấy dashboard overview cho instructor (tổng quan)
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ResultModel<InstructorDashboardResponseDto>>> GetDashboard()
    {
        // TODO: Lấy instructorId từ JWT
        var instructorId = 1;
        var result = await _dashboardService.GetDashboardAsync(instructorId);
        if (result.IsSuccess) return Ok(result);
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
        // TODO: Get instructor ID from JWT token
        var instructorId = 1; // Temporary hardcoded for testing
        
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
}

