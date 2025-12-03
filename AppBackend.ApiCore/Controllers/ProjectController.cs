using AppBackend.BusinessObjects.Dtos.Project;
using AppBackend.Services.Services.Project;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        /// <summary>
        /// Get all projects in a class with full details (Group, Leader, Members, Status)
        /// </summary>
        /// <param name="classId">Class ID</param>
        /// <returns>List of projects with complete information</returns>
        [HttpGet("class/{classId}")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        public async Task<ActionResult<ResultModel<List<ProjectGroupResponseDto>>>> GetProjectsByClass(int classId)
        {
            var result = await _projectService.GetProjectsByClassAsync(classId);
            if (result.IsSuccess)
                return Ok(result);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get projects by group ID
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <returns>List of projects for the group</returns>
        [HttpGet("group/{groupId}")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        public async Task<ActionResult<ResultModel<List<ProjectDetailDto>>>> GetProjectsByGroup(int groupId)
        {
            var result = await _projectService.GetProjectsByGroupAsync(groupId);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Create a new project (Group Leader or Instructor of the class)
        /// </summary>
        /// <param name="dto">Project creation data (GroupId, Title, Description, Component)</param>
        /// <returns>Created project information with auto-assigned "Pending" status</returns>
        /// <remarks>
        /// Business Rules:
        /// - Student: Only group leader can create project
        /// - Instructor: Can create project for any group in their class
        /// - Group must not already have a project
        /// - Status automatically set to "Pending"
        /// - Notification sent to class instructor (if created by student)
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Student,Instructor")]
        public async Task<ActionResult<ProjectCreateResultDto>> CreateProject([FromBody] ProjectCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    status = "error", 
                    message = "Invalid input data"
                });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new 
                { 
                    status = "error", 
                    message = "User not authenticated"
                });
            }

            var result = await _projectService.CreateProjectAsync(dto, userId);
            return CreatedAtAction(nameof(GetProjectsByGroup), new { groupId = dto.GroupId }, new { status = "success", data = result });
        }

        /// <summary>
        /// Update project information (Group Leader or Instructor of the class)
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="dto">Project update data (Title, Description, Component)</param>
        /// <returns>Success status</returns>
        /// <remarks>
        /// Business Rules:
        /// - Student: Only group leader can update project
        /// - Instructor: Can update any project in their class
        /// </remarks>
        [HttpPut("{projectId}")]
        [Authorize(Roles = "Student,Instructor")]
        public async Task<ActionResult> UpdateProject(int projectId, [FromBody] ProjectUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    status = "error", 
                    message = "Invalid input data"
                });
            }

            // Get current user ID from JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new 
                { 
                    status = "error", 
                    message = "User not authenticated"
                });
            }

            dto.ProjectId = projectId;
            dto.RequesterUserId = userId;

            await _projectService.UpdateProjectAsync(dto);
            return Ok(new { status = "success", message = "Project updated successfully" });
        }

        /// <summary>
        /// Change project status (Instructor only)
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="dto">Status change data (Status: Pending/Approved/Revision/Rejected, Comment)</param>
        /// <returns>Success status</returns>
        /// <remarks>
        /// Valid statuses: Pending, Approved, Revision, Rejected, InProgress, Completed
        /// Creates entry in ProjectApprovalHistory
        /// Sends notifications to all group members
        /// </remarks>
        [HttpPut("{projectId}/status")]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult> ChangeProjectStatus(int projectId, [FromBody] ProjectStatusDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    status = "error", 
                    message = "Invalid input data"
                });
            }

            // Get current instructor ID from JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var instructorId))
            {
                return Unauthorized(new 
                { 
                    status = "error", 
                    message = "User not authenticated"
                });
            }

            dto.ProjectId = projectId;
            dto.InstructorId = instructorId;

            await _projectService.ChangeStatusAsync(dto);
            return Ok(new { status = "success", message = "Project status updated successfully" });
        }

        /// <summary>
        /// Delete a project
        /// </summary>
        /// <param name="projectId">Project ID to delete</param>
        /// <returns>Success status</returns>
        /// <remarks>
        /// Authorized roles:
        /// - Admin: Can delete any project
        /// - Instructor: Can delete projects in their classes
        /// - Student: Can only delete if they are the group leader
        /// </remarks>
        [HttpDelete("{projectId}")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        public async Task<ActionResult> DeleteProject(int projectId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new 
                { 
                    status = "error", 
                    message = "User not authenticated"
                });
            }

            await _projectService.DeleteProjectAsync(projectId, userId);
            return Ok(new { status = "success", message = "Project deleted successfully" });
        }

        /// <summary>
        /// Get project status history with instructor comments
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <returns>List of status changes with comments</returns>
        /// <remarks>
        /// Shows all status updates made by instructors including:
        /// - Status (Approved, Rejected, Revision, etc.)
        /// - Instructor comments and feedback
        /// - Reviewer name
        /// - Date of review
        /// 
        /// This allows students to track feedback history on their project.
        /// </remarks>
        [HttpGet("{projectId}/status-history")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        public async Task<ActionResult<ResultModel<List<ProjectStatusHistoryDto>>>> GetProjectStatusHistory(int projectId)
        {
            var result = await _projectService.GetProjectStatusHistoryAsync(projectId);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }
    }
}
