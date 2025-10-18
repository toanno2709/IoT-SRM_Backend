using AppBackend.Repositories.Repositories.EvaluationRepo;
using AppBackend.Repositories.Repositories.EvaluationDetailRepo;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Grading;

public interface IGradingService
{
    // TODO: Refactor cho MilestoneEvaluation (không còn EvaluationDetail)
    // Task<ResultModel<GradeSubmissionResponseDto>> GradeSubmissionAsync(GradeSubmissionRequestDto request);
    // Task<ResultModel<GradeSubmissionResponseDto>> GetEvaluationAsync(int evaluationId);
}

public class GradingService : IGradingService
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly IEvaluationDetailRepository _evaluationDetailRepository;

    public GradingService(IEvaluationRepository evaluationRepository, IEvaluationDetailRepository evaluationDetailRepository)
    {
        _evaluationRepository = evaluationRepository;
        _evaluationDetailRepository = evaluationDetailRepository;
    }

    // TODO: Refactor theo ki?n trúc m?i
    // Schema m?i: MilestoneEvaluation không có EvaluationDetail
    // Ch? có: ProjectId, MilestoneDefId, InstructorId, Score, Feedback, WeightRatioSnapshot
    
    /*
    public async Task<ResultModel<GradeSubmissionResponseDto>> GradeSubmissionAsync(GradeSubmissionRequestDto request)
    {
        // C?n refactor cho MilestoneEvaluation
        return new ResultModel<GradeSubmissionResponseDto>
        {
            IsSuccess = false,
            Message = "Feature under development"
        };
    }

    public async Task<ResultModel<GradeSubmissionResponseDto>> GetEvaluationAsync(int evaluationId)
    {
        // C?n refactor cho MilestoneEvaluation
        return new ResultModel<GradeSubmissionResponseDto>
        {
            IsSuccess = false,
            Message = "Feature under development"
        };
    }
    */
}



