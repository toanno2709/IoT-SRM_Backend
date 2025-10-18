using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.Services.Class;
using AppBackend.Services.Services.Project;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.Announcement;
using AppBackend.Services.Services.TopicReview;
using AppBackend.Services.Services.Grading;

namespace AppBackend.ApiCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstructorController : ControllerBase
{
    private readonly IClassService _classService;
    private readonly IProjectService _projectService;
    private readonly IAnnouncementService _announcementService;
    private readonly ITopicReviewService _topicReviewService;
    private readonly IGradingService _gradingService;

    public InstructorController(IClassService classService, IProjectService projectService, IAnnouncementService announcementService, ITopicReviewService topicReviewService, IGradingService gradingService)
    {
        _classService = classService;
        _projectService = projectService;
        _announcementService = announcementService;
        _topicReviewService = topicReviewService;
        _gradingService = gradingService;
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
    /// Chấm điểm và phản hồi cho project milestone
    /// </summary>
    [HttpPost("grade")]
    public async Task<ActionResult<ResultModel<GradeSubmissionResponseDto>>> Grade([FromBody] GradeSubmissionRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResultModel<GradeSubmissionResponseDto> { IsSuccess = false, Message = "Invalid request" });

        // TODO: override InstructorId từ JWT
        if (request.InstructorId == 0) request.InstructorId = 1;
        var result = await _gradingService.GradeSubmissionAsync(request);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Lấy chi tiết evaluation theo id
    /// </summary>
    [HttpGet("evaluations/{evaluationId}")]
    public async Task<ActionResult<ResultModel<GradeSubmissionResponseDto>>> GetEvaluation([FromRoute] int evaluationId)
    {
        var result = await _gradingService.GetEvaluationAsync(evaluationId);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }
    
    /// <summary>
    /// Danh sách đề tài chờ duyệt (pending)
    /// </summary>
    [HttpGet("pending-proposals")]
    public async Task<ActionResult<ResultModel<List<ProposalSummaryDto>>>> GetPendingProposals()
    {
        // TODO: lấy instructorId từ JWT
        var instructorId = 1;
        var result = await _topicReviewService.GetPendingProposalsAsync(instructorId);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    /// <summary>
    /// Duyệt đề tài: Approve/Revision/Reject
    /// </summary>
    [HttpPost("proposals/{submissionId}/review")]
    public async Task<ActionResult<ResultModel<ReviewResponseDto>>> ReviewProposal([FromRoute] int submissionId, [FromBody] ReviewRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ResultModel<ReviewResponseDto> { IsSuccess = false, Message = "Invalid request" });
        }
        var instructorId = 1; // TODO: từ JWT
        var result = await _topicReviewService.ReviewProposalAsync(instructorId, submissionId, request);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }
    */

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
    /// Get all groups (projects) in a class
    /// </summary>
    /// <param name="classId">Class Id</param>
    /// <returns>List of project groups</returns>
    [HttpGet("classes/{classId}/projects")]
    public async Task<ActionResult<ResultModel<List<ProjectGroupResponseDto>>>> GetProjectsInClass([FromRoute] int classId)
    {
        try
        {
            var result = await _projectService.GetProjectsByClassAsync(classId);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new ResultModel<List<ProjectGroupResponseDto>>
            {
                IsSuccess = false,
                Message = "Internal server error",
                Data = null
            });
        }
    }
}

