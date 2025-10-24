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

    public InstructorController(IClassService classService, IProjectService projectService, IAnnouncementService announcementService, IClassStatsService classStatsService, IGroupService groupService, IMilestoneGradingService milestoneGradingService, ITopicProposalService topicProposalService, IInstructorDashboardService dashboardService, IGroupManagementService groupManagementService)
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
    }

    /// <summary>
    /// Lấy dashboard overview cho instructor (tổng quan)
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ResultModel<InstructorDashboardResponseDto>>> GetDashboard()
    {
        try
        {
            // TODO: Lấy instructorId từ JWT
            var instructorId = 1;
            var result = await _dashboardService.GetDashboardAsync(instructorId);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new ResultModel<InstructorDashboardResponseDto>
            {
                IsSuccess = false,
                Message = "Internal server error",
                Data = null
            });
        }
    }

    /// <summary>
    /// Lấy danh sách thông báo đã gửi bởi giảng viên hiện tại
    /// </summary>
    [HttpGet("announcements")]
    public async Task<ActionResult<ResultModel<List<AnnouncementResponseDto>>>> GetSentAnnouncements()
    {
        try
        {
            // TODO: Lấy admin/instructor id từ JWT
            var adminUserId = 1;
            var result = await _announcementService.GetAnnouncementsByAdminAsync(adminUserId);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new ResultModel<List<AnnouncementResponseDto>>
            {
                IsSuccess = false,
                Message = "Internal server error",
                Data = null
            });
        }
    }
    
    /// <summary>
    /// Get all classes assigned to the current instructor
    /// </summary>
    /// <returns>List of assigned classes</returns>
    [HttpGet("classes")]
    public async Task<ActionResult<ResultModel<List<ClassResponseDto>>>> GetAssignedClasses()
    {
        try
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
        catch (Exception)
        {
            return StatusCode(500, new ResultModel<List<ClassResponseDto>>
            {
                IsSuccess = false,
                Message = "Internal server error",
                Data = null
            });
        }
    }

    // TODO: Cần refactor các endpoints sau cho schema mới (MilestoneEvaluation)
    /*
    /// <summary>
    /// Lấy cấu hình class settings (max groups, max/min members)
    /// </summary>
    [HttpGet("classes/{classId}/settings")]
    public async Task<ActionResult<ResultModel<ClassSettingsResponseDto>>> GetClassSettings([FromRoute] int classId)
    {
        var result = await _classService.GetClassSettingsAsync(classId);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Cập nhật cấu hình class settings (max groups, max/min members per group)
    /// </summary>
    [HttpPut("classes/{classId}/settings")]
    public async Task<ActionResult<ResultModel<ClassSettingsResponseDto>>> UpdateClassSettings(
        [FromRoute] int classId,
        [FromBody] ClassSettingsUpdateRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<ClassSettingsResponseDto>
            {
                IsSuccess = false,
                Message = "Invalid request",
                Data = null
            });
        }

        var result = await _classService.UpdateClassSettingsAsync(classId, request);
        if (result.IsSuccess) return Ok(result);
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
        try
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
        catch (Exception)
        {
            return StatusCode(500, new ResultModel<AnnouncementResponseDto>
            {
                IsSuccess = false,
                Message = "Internal server error",
                Data = null
            });
        }
    }
    
    /// <summary>
    /// Get all groups in a class
    /// </summary>
    /// <param name="classId">Class Id</param>
    /// <returns>List of groups</returns>
    [HttpGet("classes/{classId}/groups")]
    public async Task<ActionResult<ResultModel<List<GroupResponseDto>>>> GetGroupsInClass([FromRoute] int classId)
    {
        try
        {
            var result = await _groupService.GetGroupsByClassAsync(classId);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new ResultModel<List<GroupResponseDto>>
            {
                IsSuccess = false,
                Message = "Internal server error",
                Data = null
            });
        }
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
        try
        {
            var result = await _classStatsService.GetClassStatsAsync(classId);
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new ResultModel<ClassStatsResponseDto>
            {
                IsSuccess = false,
                Message = "Internal server error",
                Data = null
            });
        }
    }
}

