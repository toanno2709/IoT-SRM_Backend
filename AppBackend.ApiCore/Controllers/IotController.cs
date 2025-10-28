using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.Sensor;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.ApiModels.Sensor;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/iot")]
    public class IotController : ControllerBase
    {
        private readonly ISensorService _sensorService;

        public IotController(ISensorService sensorService)
        {
            _sensorService = sensorService;
        }

        /// <summary>
        /// Create a new sensor for a project
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="request">Sensor creation data</param>
        /// <returns>Created sensor</returns>
        [HttpPost("projects/{projectId}/sensors")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        [ProducesResponseType(typeof(ResultModel<SensorResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResultModel<SensorResponseDto>>> CreateSensor(
            int projectId, 
            [FromBody] CreateSensorRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<SensorResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var result = await _sensorService.CreateSensorAsync(projectId, currentUserId, userRole, request);
            
            if (result.IsSuccess)
                return CreatedAtAction(
                    nameof(GetSensorById), 
                    new { projectId = projectId, sensorId = result.Data?.SensorId }, 
                    result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get all sensors of a project with search and pagination
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="q">Search query (optional)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50, max: 200)</param>
        /// <returns>Paginated list of sensors</returns>
        [HttpGet("projects/{projectId}/sensors")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        [ProducesResponseType(typeof(ResultModel<SensorListResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultModel<SensorListResponseDto>>> ListSensors(
            int projectId,
            [FromQuery] string? q = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var result = await _sensorService.GetSensorsByProjectAsync(projectId, currentUserId, userRole, q, page, pageSize);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get sensor by ID
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="sensorId">Sensor ID</param>
        /// <returns>Sensor details</returns>
        [HttpGet("projects/{projectId}/sensors/{sensorId}")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        [ProducesResponseType(typeof(ResultModel<SensorDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultModel<SensorDetailDto>>> GetSensorById(
            int projectId,
            int sensorId)
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var result = await _sensorService.GetSensorByIdAsync(projectId, sensorId, currentUserId, userRole);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Update sensor information
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="sensorId">Sensor ID</param>
        /// <param name="request">Sensor update data</param>
        /// <returns>Updated sensor</returns>
        [HttpPut("projects/{projectId}/sensors/{sensorId}")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        [ProducesResponseType(typeof(ResultModel<SensorResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResultModel<SensorResponseDto>>> UpdateSensor(
            int projectId,
            int sensorId,
            [FromBody] UpdateSensorRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<SensorResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var result = await _sensorService.UpdateSensorAsync(projectId, sensorId, currentUserId, userRole, request);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Delete a sensor
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="sensorId">Sensor ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("projects/{projectId}/sensors/{sensorId}")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> DeleteSensor(
            int projectId,
            int sensorId)
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var result = await _sensorService.DeleteSensorAsync(projectId, sensorId, currentUserId, userRole);
            
            if (result.IsSuccess)
                return NoContent();
            
            return StatusCode(result.StatusCode, result);
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return 0;
        }

        private string GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            return roleClaim ?? "Student"; // Default to Student if no role found
        }

        #endregion
    }
}
