using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.InstructorDashboard;

public interface IInstructorDashboardService
{
    Task<ResultModel<InstructorDashboardResponseDto>> GetDashboardAsync(int instructorId);
}


