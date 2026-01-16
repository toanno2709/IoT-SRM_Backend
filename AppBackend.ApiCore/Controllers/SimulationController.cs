using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.Simulation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SimulationController : ControllerBase
{
    private readonly ISimulationService _simulationService;

    public SimulationController(ISimulationService simulationService)
    {
        _simulationService = simulationService;
    }

    /// <summary>
    /// Create a new simulation for a project
    /// </summary>
    /// <remarks>
    /// Only group leader, class instructor, or admin can create simulations
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(SimulationCreateResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSimulation([FromBody] SimulationCreateRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var result = await _simulationService.CreateSimulationAsync(dto, userId);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get simulation by ID
    /// </summary>
    [HttpGet("{simulationId}")]
    [ProducesResponseType(typeof(SimulationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSimulationById(int simulationId)
    {
        var result = await _simulationService.GetSimulationByIdAsync(simulationId);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all simulations for a project
    /// </summary>
    [HttpGet("project/{projectId}")]
    [ProducesResponseType(typeof(List<SimulationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSimulationsByProjectId(int projectId)
    {
        var result = await _simulationService.GetSimulationsByProjectIdAsync(projectId);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Update a simulation
    /// </summary>
    /// <remarks>
    /// Only group leader, class instructor, or admin can update simulations
    /// </remarks>
    [HttpPut("{simulationId}")]
    [ProducesResponseType(typeof(SimulationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSimulation(int simulationId, [FromBody] SimulationUpdateRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var result = await _simulationService.UpdateSimulationAsync(simulationId, dto, userId);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a simulation
    /// </summary>
    /// <remarks>
    /// Only group leader, class instructor, or admin can delete simulations
    /// </remarks>
    [HttpDelete("{simulationId}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSimulation(int simulationId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var result = await _simulationService.DeleteSimulationAsync(simulationId, userId);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Update simulation status (draft / submitted / graded)
    /// </summary>
    /// <remarks>
    /// Only group leader, class instructor, or admin can update simulation status
    /// </remarks>
    [HttpPatch("{simulationId}/status")]
    [ProducesResponseType(typeof(SimulationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSimulationStatus(int simulationId, [FromBody] SimulationStatusUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var result = await _simulationService.UpdateSimulationStatusAsync(simulationId, dto, userId);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result);
    }
}
