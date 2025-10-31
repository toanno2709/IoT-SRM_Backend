using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.Services.ProjectMilestone;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.ApiCore.Controllers;

[ApiController]
[Route("api/projects/{projectId}/milestones")]
public class ProjectMilestonesController : ControllerBase
{
    private readonly IProjectMilestoneService _milestoneService;

    public ProjectMilestonesController(IProjectMilestoneService milestoneService)
    {
        _milestoneService = milestoneService;
    }

    [HttpGet("total-weight")]
    public async Task<ActionResult<object>> GetTotalWeight([FromRoute] int projectId)
    {
        // Endpoint tiện ích: tính tổng weight hiện tại của project
        var items = await _milestoneService.GetByProjectAsync(projectId);
        if (!items.IsSuccess || items.Data == null) return BadRequest(items);
        var total = items.Data.Sum(m => m.Weight ?? 0);
        return Ok(new { totalWeight = total });
    }

    [HttpGet]
    public async Task<ActionResult<ResultModel<List<ProjectMilestoneResponseDto>>>> GetByProject([FromRoute] int projectId)
    {
        var result = await _milestoneService.GetByProjectAsync(projectId);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    public async Task<ActionResult<ResultModel<ProjectMilestoneResponseDto>>> Create([FromRoute] int projectId, [FromBody] ProjectMilestoneCreateRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResultModel<ProjectMilestoneResponseDto> { IsSuccess = false, Message = "Invalid request" });
        request.ProjectId = projectId;
        var result = await _milestoneService.CreateAsync(request);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    [HttpPut("{milestoneId}")]
    public async Task<ActionResult<ResultModel<ProjectMilestoneResponseDto>>> Update([FromRoute] int projectId, [FromRoute] int milestoneId, [FromBody] ProjectMilestoneUpdateRequestDto request)
    {
        request.MilestoneId = milestoneId;
        var result = await _milestoneService.UpdateAsync(request);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }

    [HttpDelete("{milestoneId}")]
    public async Task<ActionResult<ResultModel<bool>>> Delete([FromRoute] int projectId, [FromRoute] int milestoneId)
    {
        var result = await _milestoneService.DeleteAsync(milestoneId);
        if (result.IsSuccess) return Ok(result);
        return BadRequest(result);
    }
}


