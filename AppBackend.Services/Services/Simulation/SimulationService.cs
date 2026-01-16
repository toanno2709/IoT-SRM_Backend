using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Dtos.Project;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Constants;
using AppBackend.Repositories.Repositories.SimulationRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.Simulation;

public class SimulationService : ISimulationService
{
    private readonly ISimulationRepository _simulationRepository;
    private readonly IotShowroomContext _db;

    public SimulationService(ISimulationRepository simulationRepository, IotShowroomContext db)
    {
        _simulationRepository = simulationRepository;
        _db = db;
    }

    public async Task<ResultModel<SimulationCreateResponseDto>> CreateSimulationAsync(SimulationCreateRequestDto dto, int userId)
    {
        try
        {
            // Validate project exists
            var project = await _db.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                .FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);

            if (project == null)
            {
                return new ResultModel<SimulationCreateResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Project not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Validate user exists
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return new ResultModel<SimulationCreateResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "User not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check permissions: must be group leader or instructor of the class
            bool isGroupLeader = project.Group?.LeaderId == userId;
            bool isInstructor = user.RoleId == 2 && project.Group?.Class?.InstructorId == userId;
            bool isAdmin = user.RoleId == 1;

            if (!isGroupLeader && !isInstructor && !isAdmin)
            {
                return new ResultModel<SimulationCreateResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FORBIDDEN,
                    Message = "Only group leader, class instructor, or admin can create simulations",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Create simulation
            var simulation = new AppBackend.BusinessObjects.Models.Simulation
            {
                ProjectId = dto.ProjectId,
                Title = dto.Title,
                Description = dto.Description,
                WokwiProjectUrl = dto.WokwiProjectUrl,
                WokwiProjectId = dto.WokwiProjectId,
                Status = "draft",
                CreatedAt = DateTime.UtcNow
            };

            await _simulationRepository.AddAsync(simulation);
            await _simulationRepository.SaveChangesAsync();

            return new ResultModel<SimulationCreateResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Simulation created successfully",
                Data = new SimulationCreateResponseDto
                {
                    SimulationId = simulation.SimulationId,
                    ProjectId = simulation.ProjectId,
                    Title = simulation.Title,
                    Status = simulation.Status,
                    CreatedAt = simulation.CreatedAt
                },
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<SimulationCreateResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error creating simulation: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<SimulationResponseDto>> GetSimulationByIdAsync(int simulationId)
    {
        try
        {
            var simulation = await _simulationRepository.GetByIdWithDetailsAsync(simulationId);

            if (simulation == null)
            {
                return new ResultModel<SimulationResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Simulation not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var dto = new SimulationResponseDto
            {
                SimulationId = simulation.SimulationId,
                ProjectId = simulation.ProjectId,
                ProjectTitle = simulation.Project?.Title,
                GroupName = simulation.Project?.Group?.GroupName,
                Title = simulation.Title,
                Description = simulation.Description,
                Status = simulation.Status,
                WokwiProjectUrl = simulation.WokwiProjectUrl,
                WokwiProjectId = simulation.WokwiProjectId,
                CreatedAt = simulation.CreatedAt,
                UpdatedAt = simulation.UpdatedAt
            };

            return new ResultModel<SimulationResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Simulation retrieved successfully",
                Data = dto,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<SimulationResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error retrieving simulation: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<List<SimulationResponseDto>>> GetSimulationsByProjectIdAsync(int projectId)
    {
        try
        {
            // Validate project exists
            var projectExists = await _db.Projects.AnyAsync(p => p.ProjectId == projectId);
            if (!projectExists)
            {
                return new ResultModel<List<SimulationResponseDto>>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Project not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var simulations = await _simulationRepository.GetByProjectIdAsync(projectId);

            var dtos = simulations.Select(s => new SimulationResponseDto
            {
                SimulationId = s.SimulationId,
                ProjectId = s.ProjectId,
                ProjectTitle = s.Project?.Title,
                GroupName = s.Project?.Group?.GroupName,
                Title = s.Title,
                Description = s.Description,
                Status = s.Status,
                WokwiProjectUrl = s.WokwiProjectUrl,
                WokwiProjectId = s.WokwiProjectId,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return new ResultModel<List<SimulationResponseDto>>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Retrieved {dtos.Count} simulation(s) for project {projectId}",
                Data = dtos,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<SimulationResponseDto>>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error retrieving simulations: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<SimulationResponseDto>> UpdateSimulationAsync(int simulationId, SimulationUpdateRequestDto dto, int userId)
    {
        try
        {
            var simulation = await _simulationRepository.GetByIdWithDetailsAsync(simulationId);

            if (simulation == null)
            {
                return new ResultModel<SimulationResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Simulation not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Validate user exists
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return new ResultModel<SimulationResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "User not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check permissions
            bool isGroupLeader = simulation.Project?.Group?.LeaderId == userId;
            bool isInstructor = user.RoleId == 2 && simulation.Project?.Group?.Class?.InstructorId == userId;
            bool isAdmin = user.RoleId == 1;

            if (!isGroupLeader && !isInstructor && !isAdmin)
            {
                return new ResultModel<SimulationResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FORBIDDEN,
                    Message = "Only group leader, class instructor, or admin can update simulations",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Update simulation
            simulation.Title = dto.Title;
            simulation.Description = dto.Description;
            simulation.WokwiProjectUrl = dto.WokwiProjectUrl;
            simulation.WokwiProjectId = dto.WokwiProjectId;
            
            if (!string.IsNullOrEmpty(dto.Status))
            {
                simulation.Status = dto.Status;
            }
            
            simulation.UpdatedAt = DateTime.UtcNow;

            await _simulationRepository.UpdateAsync(simulation);
            await _simulationRepository.SaveChangesAsync();

            // Reload simulation with full details to ensure navigation properties are populated
            var updatedSimulation = await _simulationRepository.GetByIdWithDetailsAsync(simulationId);

            var responseDto = new SimulationResponseDto
            {
                SimulationId = updatedSimulation!.SimulationId,
                ProjectId = updatedSimulation.ProjectId,
                ProjectTitle = updatedSimulation.Project?.Title,
                GroupName = updatedSimulation.Project?.Group?.GroupName,
                Title = updatedSimulation.Title,
                Description = updatedSimulation.Description,
                Status = updatedSimulation.Status,
                WokwiProjectUrl = updatedSimulation.WokwiProjectUrl,
                WokwiProjectId = updatedSimulation.WokwiProjectId,
                CreatedAt = updatedSimulation.CreatedAt,
                UpdatedAt = updatedSimulation.UpdatedAt
            };

            return new ResultModel<SimulationResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Simulation updated successfully",
                Data = responseDto,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<SimulationResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error updating simulation: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<string>> DeleteSimulationAsync(int simulationId, int userId)
    {
        try
        {
            var simulation = await _simulationRepository.GetByIdWithDetailsAsync(simulationId);

            if (simulation == null)
            {
                return new ResultModel<string>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Simulation not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Validate user exists
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return new ResultModel<string>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "User not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check permissions
            bool isGroupLeader = simulation.Project?.Group?.LeaderId == userId;
            bool isInstructor = user.RoleId == 2 && simulation.Project?.Group?.Class?.InstructorId == userId;
            bool isAdmin = user.RoleId == 1;

            if (!isGroupLeader && !isInstructor && !isAdmin)
            {
                return new ResultModel<string>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FORBIDDEN,
                    Message = "Only group leader, class instructor, or admin can delete simulations",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            await _simulationRepository.DeleteAsync(simulation);
            await _simulationRepository.SaveChangesAsync();

            return new ResultModel<string>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Simulation deleted successfully",
                Data = $"Simulation '{simulation.Title}' has been deleted",
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<string>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error deleting simulation: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<SimulationResponseDto>> UpdateSimulationStatusAsync(int simulationId, SimulationStatusUpdateDto dto, int userId)
    {
        try
        {
            var simulation = await _simulationRepository.GetByIdWithDetailsAsync(simulationId);

            if (simulation == null)
            {
                return new ResultModel<SimulationResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Simulation not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Validate user exists
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return new ResultModel<SimulationResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "User not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check permissions
            bool isGroupLeader = simulation.Project?.Group?.LeaderId == userId;
            bool isInstructor = user.RoleId == 2 && simulation.Project?.Group?.Class?.InstructorId == userId;
            bool isAdmin = user.RoleId == 1;

            if (!isGroupLeader && !isInstructor && !isAdmin)
            {
                return new ResultModel<SimulationResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FORBIDDEN,
                    Message = "Only group leader, class instructor, or admin can update simulation status",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            simulation.Status = dto.Status;
            simulation.UpdatedAt = DateTime.UtcNow;

            await _simulationRepository.UpdateAsync(simulation);
            await _simulationRepository.SaveChangesAsync();

            // Reload simulation with full details to ensure navigation properties are populated
            var updatedSimulation = await _simulationRepository.GetByIdWithDetailsAsync(simulationId);

            var responseDto = new SimulationResponseDto
            {
                SimulationId = updatedSimulation!.SimulationId,
                ProjectId = updatedSimulation.ProjectId,
                ProjectTitle = updatedSimulation.Project?.Title,
                GroupName = updatedSimulation.Project?.Group?.GroupName,
                Title = updatedSimulation.Title,
                Description = updatedSimulation.Description,
                Status = updatedSimulation.Status,
                WokwiProjectUrl = updatedSimulation.WokwiProjectUrl,
                WokwiProjectId = updatedSimulation.WokwiProjectId,
                CreatedAt = updatedSimulation.CreatedAt,
                UpdatedAt = updatedSimulation.UpdatedAt
            };

            return new ResultModel<SimulationResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Simulation status updated to '{dto.Status}' successfully",
                Data = responseDto,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<SimulationResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error updating simulation status: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
