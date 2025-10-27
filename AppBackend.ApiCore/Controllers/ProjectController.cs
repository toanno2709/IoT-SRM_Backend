using AppBackend.BusinessObjects.Dtos.Project;
using AppBackend.Services.Services.Project;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    
        [Route("api/[controller]")]
        [ApiController]
        public class ProjectController : ControllerBase
        {
            private readonly IProjectService _projectService;
            private readonly ILogger<ProjectController> _logger;

            public ProjectController(IProjectService projectService, ILogger<ProjectController> logger)
            {
                _projectService = projectService;
                _logger = logger;
            }

            [HttpPost("create")]
            public async Task<IActionResult> CreateProject([FromBody] ProjectCreateDto dto)
            {
                try
                {
                    var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                    if (!int.TryParse(userIdClaim, out var leaderId))
                        return Unauthorized(new { status = "error", message = "Invalid user" });

                    var result = await _projectService.CreateProjectAsync(dto, leaderId);
                    return Ok(new { status = "success", data = result });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CreateProject");
                    return BadRequest(new { status = "error", message = ex.Message });
                }
            }

            [HttpPut("update")]
            public async Task<IActionResult> UpdateProject([FromBody] ProjectUpdateDto dto)
            {
                try
                {
                    await _projectService.UpdateProjectAsync(dto);
                    return Ok(new { status = "success", message = "Project updated" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UpdateProject");
                    return BadRequest(new { status = "error", message = ex.Message });
                }
            }

            [HttpGet("by-group/{groupId}")]
            public async Task<IActionResult> GetByGroup(int groupId)
            {
                try
                {
                    var result = await _projectService.GetProjectByGroupAsync(groupId);
                    return Ok(new { status = "success", data = result });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetByGroup");
                    return BadRequest(new { status = "error", message = ex.Message });
                }
            }

            [HttpGet("by-class/{classId}")]
            public async Task<IActionResult> GetByClass(int classId)
            {
                try
                {
                    var result = await _projectService.GetProjectsByClassAsync1(classId);
                    return Ok(new { status = "success", data = result });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetByClass");
                    return BadRequest(new { status = "error", message = ex.Message });
                }
            }

            [HttpPut("change-status")]
            public async Task<IActionResult> ChangeStatus([FromBody] ProjectStatusDto dto)
            {
                try
                {
                    await _projectService.ChangeStatusAsync(dto);
                    return Ok(new { status = "success", message = "Project status updated" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ChangeStatus");
                    return BadRequest(new { status = "error", message = ex.Message });
                }
            }

            [HttpDelete("delete/{projectId}")]
            public async Task<IActionResult> Delete(int projectId)
            {
                try
                {
                    var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                    if (!int.TryParse(userIdClaim, out var userId))
                        return Unauthorized(new { status = "error", message = "Invalid user" });

                    await _projectService.DeleteProjectAsync(projectId, userId);
                    return Ok(new { status = "success", message = "Project deleted" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DeleteProject");
                    return BadRequest(new { status = "error", message = ex.Message });
                }
            }
        }
    }
