using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Repositories.Repositories.ProjectMilestoneRepo;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.MilestoneGrading;

public class MilestoneGradingService : IMilestoneGradingService
{
    private readonly IMilestoneEvaluationRepository _evaluationRepository;
    private readonly IProjectMilestoneRepository _milestoneRepository;

    public MilestoneGradingService(IMilestoneEvaluationRepository evaluationRepository, IProjectMilestoneRepository milestoneRepository)
    {
        _evaluationRepository = evaluationRepository;
        _milestoneRepository = milestoneRepository;
    }

    public async Task<ResultModel<MilestoneGradeResponseDto>> GradeMilestoneAsync(MilestoneGradeRequestDto request)
    {
        try
        {
            // Validate milestone exists
            var milestone = await _milestoneRepository.GetByIdAsync(request.MilestoneDefId);
            if (milestone == null)
            {
                return new ResultModel<MilestoneGradeResponseDto> 
                { 
                    IsSuccess = false, 
                    Message = "Milestone not found" 
                };
            }

            // Check if evaluation already exists
            var evaluation = await _evaluationRepository.GetByProjectMilestoneInstructorAsync(
                request.ProjectId, 
                request.MilestoneDefId, 
                request.InstructorId);

            var isNew = evaluation == null;

            if (isNew)
            {
                evaluation = new AppBackend.BusinessObjects.Models.MilestoneEvaluation
                {
                    ProjectId = request.ProjectId,
                    MilestoneDefId = request.MilestoneDefId,
                    InstructorId = request.InstructorId,
                    WeightRatioSnapshot = request.WeightRatioSnapshot,
                    Score = request.Score,
                    Feedback = request.Feedback,
                    EvaluatedAt = DateTime.UtcNow
                };
                await _evaluationRepository.AddAsync(evaluation);
            }
            else
            {
                evaluation.WeightRatioSnapshot = request.WeightRatioSnapshot;
                evaluation.Score = request.Score;
                evaluation.Feedback = request.Feedback;
                evaluation.EvaluatedAt = DateTime.UtcNow;
                await _evaluationRepository.UpdateAsync(evaluation);
            }

            await _evaluationRepository.SaveChangesAsync();

            return new ResultModel<MilestoneGradeResponseDto>
            {
                IsSuccess = true,
                Message = isNew ? "Milestone graded successfully" : "Milestone grade updated successfully",
                Data = new MilestoneGradeResponseDto
                {
                    MeId = evaluation.MeId,
                    ProjectId = evaluation.ProjectId,
                    MilestoneDefId = evaluation.MilestoneDefId,
                    MilestoneTitle = milestone.Title,
                    InstructorId = evaluation.InstructorId,
                    WeightRatioSnapshot = evaluation.WeightRatioSnapshot,
                    Score = evaluation.Score,
                    Feedback = evaluation.Feedback,
                    EvaluatedAt = evaluation.EvaluatedAt
                }
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<MilestoneGradeResponseDto>
            {
                IsSuccess = false,
                Message = $"Error grading milestone: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<List<MilestoneGradeResponseDto>>> GetGradesByProjectAsync(int projectId)
    {
        try
        {
            var evaluations = await _evaluationRepository.GetByProjectAsync(projectId);

            var dtos = evaluations.Select(e => new MilestoneGradeResponseDto
            {
                MeId = e.MeId,
                ProjectId = e.ProjectId,
                MilestoneDefId = e.MilestoneDefId,
                MilestoneTitle = e.MilestoneDef?.Title,
                InstructorId = e.InstructorId,
                InstructorName = e.Instructor?.FullName,
                WeightRatioSnapshot = e.WeightRatioSnapshot,
                Score = e.Score,
                Feedback = e.Feedback,
                EvaluatedAt = e.EvaluatedAt
            }).ToList();

            return new ResultModel<List<MilestoneGradeResponseDto>>
            {
                IsSuccess = true,
                Message = "Milestone grades retrieved successfully",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<MilestoneGradeResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving grades: {ex.Message}",
                Data = null
            };
        }
    }
}

