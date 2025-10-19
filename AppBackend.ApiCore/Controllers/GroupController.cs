using AppBackend.BusinessObjects.Dtos.Group;
using AppBackend.Services.Services.Group;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly ILogger<GroupController> _logger;

        public GroupController(IGroupService groupService, ILogger<GroupController> logger)
        {
            _groupService = groupService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateGroup([FromBody] GroupCreateDto dto)
        {
            try
            {
                // get user id from token (example; adapt if your auth stores different claim)
                var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { status = "error", message = "Invalid user." });
                }

                var result = await _groupService.CreateGroupAsync(dto, userId);
                return Ok(new { status = "success", data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateGroup failed");
                return StatusCode(500, new { status = "error", message = "Internal Server Error" });
            }
        }

        [HttpPost("invite")]
        public async Task<IActionResult> InviteMember([FromBody] GroupInviteDto dto)
        {
            try
            {
                await _groupService.InviteMemberAsync(dto);
                return Ok(new { status = "success", message = "Invitation sent." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { status = "error", message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { status = "error", message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "InviteMember"); return StatusCode(500, new { status = "error", message = "Internal Server Error" }); }
        }

        [HttpPost("accept-invite")]
        public async Task<IActionResult> AcceptInvite([FromBody] GroupAcceptInviteDto dto)
        {
            try
            {
                await _groupService.AcceptInviteAsync(dto);
                return Ok(new { status = "success", message = "Joined group." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = "error", message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { status = "error", message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "AcceptInvite"); return StatusCode(500, new { status = "error", message = "Internal Server Error" }); }
        }

        [HttpGet("by-class/{classId}")]
        public async Task<IActionResult> GetGroupsByClass(int classId)
        {
            try
            {
                var result = await _groupService.GetGroupsByClassAsync(classId);
                return Ok(new { status = "success", data = result });
            }
            catch (Exception ex) { _logger.LogError(ex, "GetGroupsByClass"); return StatusCode(500, new { status = "error", message = "Internal Server Error" }); }
        }

        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroupDetail(int groupId)
        {
            try
            {
                var result = await _groupService.GetGroupDetailAsync(groupId);
                return Ok(new { status = "success", data = result });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { status = "error", message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "GetGroupDetail"); return StatusCode(500, new { status = "error", message = "Internal Server Error" }); }
        }

        [HttpPost("leave")]
        public async Task<IActionResult> LeaveGroup([FromBody] GroupLeaveDto dto)
        {
            try
            {
                await _groupService.LeaveGroupAsync(dto);
                return Ok(new { status = "success", message = "Left group" });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = "error", message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { status = "error", message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "LeaveGroup"); return StatusCode(500, new { status = "error", message = "Internal Server Error" }); }
        }

        [HttpPost("kick")]
        public async Task<IActionResult> KickMember([FromBody] GroupKickDto dto)
        {
            try
            {
                await _groupService.KickMemberAsync(dto);
                return Ok(new { status = "success", message = "Member removed" });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = "error", message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { status = "error", message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "KickMember"); return StatusCode(500, new { status = "error", message = "Internal Server Error" }); }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateGroup([FromBody] GroupUpdateDto dto)
        {
            try
            {
                await _groupService.UpdateGroupAsync(dto);
                return Ok(new { status = "success", message = "Group updated" });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = "error", message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { status = "error", message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "UpdateGroup"); return StatusCode(500, new { status = "error", message = "Internal Server Error" }); }
        }

        [HttpDelete("delete/{groupId}")]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            try
            {
                // get requester id from token
                var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized(new { status = "error", message = "Invalid user." });

                await _groupService.DeleteGroupAsync(groupId, userId);
                return Ok(new { status = "success", message = "Group deleted" });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = "error", message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { status = "error", message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "DeleteGroup"); return StatusCode(500, new { status = "error", message = "Internal Server Error" }); }
        }
    }
}
