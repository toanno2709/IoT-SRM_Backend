using AppBackend.Repositories.Repositories.EvaluationRepo;
using AppBackend.Repositories.Repositories.EvaluationDetailRepo;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Grading;

public interface IGradingService
{
    Task<ResultModel<GradeSubmissionResponseDto>> GradeSubmissionAsync(GradeSubmissionRequestDto request);
    Task<ResultModel<GradeSubmissionResponseDto>> GetEvaluationAsync(int evaluationId);
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

    public async Task<ResultModel<GradeSubmissionResponseDto>> GradeSubmissionAsync(GradeSubmissionRequestDto request)
    {
        if (request.TotalScore < 0 || request.TotalScore > 100)
        {
            return new ResultModel<GradeSubmissionResponseDto> { IsSuccess = false, Message = "TotalScore must be between 0 and 100" };
        }

        var evaluation = await _evaluationRepository.GetByProjectAndInstructorAsync(request.ProjectId, request.InstructorId);
        var isNew = evaluation == null;
        if (isNew)
        {
            evaluation = new AppBackend.BusinessObjects.Models.Evaluation
            {
                ProjectId = request.ProjectId,
                InstructorId = request.InstructorId,
                TotalScore = request.TotalScore,
                Feedback = request.Feedback,
                EvaluatedAt = DateTime.UtcNow
            };
            await _evaluationRepository.AddAsync(evaluation);
        }
        else
        {
            evaluation.TotalScore = request.TotalScore;
            evaluation.Feedback = request.Feedback;
            evaluation.EvaluatedAt = DateTime.UtcNow;
            await _evaluationRepository.UpdateAsync(evaluation);
        }

        // Persist evaluation first to have EvaluationId
        await _evaluationRepository.SaveChangesAsync();

        if (request.Details != null && request.Details.Any())
        {
            // Simple approach: remove old details and add new ones (could be optimized later)
            foreach (var old in evaluation.EvaluationDetails.ToList())
            {
                await _evaluationDetailRepository.DeleteAsync(old);
            }
            await _evaluationDetailRepository.SaveChangesAsync();

            foreach (var d in request.Details)
            {
                var detail = new AppBackend.BusinessObjects.Models.EvaluationDetail
                {
                    EvaluationId = evaluation.EvaluationId,
                    RubricId = d.RubricId,
                    Score = d.Score
                };
                await _evaluationDetailRepository.AddAsync(detail);
            }
            await _evaluationDetailRepository.SaveChangesAsync();
        }

        return new ResultModel<GradeSubmissionResponseDto>
        {
            IsSuccess = true,
            Message = isNew ? "Evaluation created" : "Evaluation updated",
            Data = new GradeSubmissionResponseDto
            {
                EvaluationId = evaluation.EvaluationId,
                ProjectId = evaluation.ProjectId ?? 0,
                InstructorId = evaluation.InstructorId ?? 0,
                TotalScore = evaluation.TotalScore ?? 0,
                Feedback = evaluation.Feedback,
                EvaluatedAt = evaluation.EvaluatedAt,
                Details = evaluation.EvaluationDetails.Select(ed => new EvaluationDetailDto
                {
                    DetailId = ed.DetailId,
                    RubricId = ed.RubricId ?? 0,
                    Score = ed.Score ?? 0
                }).ToList()
            }
        };
    }

    public async Task<ResultModel<GradeSubmissionResponseDto>> GetEvaluationAsync(int evaluationId)
    {
        var evaluation = await _evaluationRepository.GetWithDetailsAsync(evaluationId);
        if (evaluation == null)
        {
            return new ResultModel<GradeSubmissionResponseDto> { IsSuccess = false, Message = "Evaluation not found" };
        }

        return new ResultModel<GradeSubmissionResponseDto>
        {
            IsSuccess = true,
            Message = "Evaluation retrieved",
            Data = new GradeSubmissionResponseDto
            {
                EvaluationId = evaluation.EvaluationId,
                ProjectId = evaluation.ProjectId ?? 0,
                InstructorId = evaluation.InstructorId ?? 0,
                TotalScore = evaluation.TotalScore ?? 0,
                Feedback = evaluation.Feedback,
                EvaluatedAt = evaluation.EvaluatedAt,
                Details = evaluation.EvaluationDetails.Select(ed => new EvaluationDetailDto
                {
                    DetailId = ed.DetailId,
                    RubricId = ed.RubricId ?? 0,
                    Score = ed.Score ?? 0
                }).ToList()
            }
        };
    }
}



