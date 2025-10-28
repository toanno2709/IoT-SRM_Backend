using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.MilestoneGrading;

public interface IMilestoneGradingService
{
    Task<ResultModel<MilestoneGradeResponseDto>> GradeMilestoneAsync(MilestoneGradeRequestDto request);
    Task<ResultModel<List<MilestoneGradeResponseDto>>> GetGradesByProjectAsync(int projectId);
}


