using AppBackend.BusinessObjects.Dtos.Project;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Simulation;

public interface ISimulationService
{
    Task<ResultModel<SimulationCreateResponseDto>> CreateSimulationAsync(SimulationCreateRequestDto dto, int userId);
    Task<ResultModel<SimulationResponseDto>> GetSimulationByIdAsync(int simulationId);
    Task<ResultModel<List<SimulationResponseDto>>> GetSimulationsByProjectIdAsync(int projectId);
    Task<ResultModel<SimulationResponseDto>> UpdateSimulationAsync(int simulationId, SimulationUpdateRequestDto dto, int userId);
    Task<ResultModel<string>> DeleteSimulationAsync(int simulationId, int userId);
    Task<ResultModel<SimulationResponseDto>> UpdateSimulationStatusAsync(int simulationId, SimulationStatusUpdateDto dto, int userId);
}
