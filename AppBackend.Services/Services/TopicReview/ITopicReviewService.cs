using AppBackend.Repositories.Repositories.SubmissionRepo;
using AppBackend.Repositories.Repositories.ApprovalHistoryRepo;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.TopicReview;

public interface ITopicReviewService
{
    Task<ResultModel<List<ProposalSummaryDto>>> GetPendingProposalsAsync(int instructorId);
    Task<ResultModel<ReviewResponseDto>> ReviewProposalAsync(int instructorId, int submissionId, ReviewRequestDto request);
}

public class TopicReviewService : ITopicReviewService
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IApprovalHistoryRepository _approvalHistoryRepository;

    public TopicReviewService(ISubmissionRepository submissionRepository, IApprovalHistoryRepository approvalHistoryRepository)
    {
        _submissionRepository = submissionRepository;
        _approvalHistoryRepository = approvalHistoryRepository;
    }

    public async Task<ResultModel<List<ProposalSummaryDto>>> GetPendingProposalsAsync(int instructorId)
    {
        var subs = await _submissionRepository.GetPendingProposalsByInstructorAsync(instructorId);
        var dtos = subs.Select(s => new ProposalSummaryDto
        {
            SubmissionId = s.SubmissionId,
            ProjectId = s.ProjectId,
            ProjectTitle = s.Project?.Title,
            ClassId = s.Project?.ClassId,
            ClassName = s.Project?.Class?.ClassName,
            LeaderId = s.Project?.LeaderId ?? 0,
            LeaderName = s.Project?.Leader?.FullName,
            Status = s.Status,
            SubmittedAt = s.CreatedAt,
            Note = s.Note
        }).ToList();

        return new ResultModel<List<ProposalSummaryDto>>
        {
            IsSuccess = true,
            Message = "Pending proposals retrieved",
            Data = dtos
        };
    }

    public async Task<ResultModel<ReviewResponseDto>> ReviewProposalAsync(int instructorId, int submissionId, ReviewRequestDto request)
    {
        var sub = await _submissionRepository.GetByIdAsync(submissionId);
        if (sub == null)
        {
            return new ResultModel<ReviewResponseDto> { IsSuccess = false, Message = "Submission not found" };
        }

        // Optional: verify instructor owns this class
        // For now assume authorized already

        var normalized = request.Action.Trim();
        if (normalized == "Approve") sub.Status = "Approved";
        else if (normalized == "Revision") sub.Status = "RevisionRequested";
        else if (normalized == "Reject") sub.Status = "Rejected";

        await _submissionRepository.UpdateAsync(sub);

        var history = new AppBackend.BusinessObjects.Models.ProjectApprovalHistory
        {
            SubmissionId = sub.SubmissionId,
            ReviewerId = instructorId,
            Action = request.Action,
            Comment = request.Comment,
            ActedAt = DateTime.UtcNow
        };
        await _approvalHistoryRepository.AddAsync(history);

        await _submissionRepository.SaveChangesAsync();
        await _approvalHistoryRepository.SaveChangesAsync();

        return new ResultModel<ReviewResponseDto>
        {
            IsSuccess = true,
            Message = "Review recorded",
            Data = new ReviewResponseDto
            {
                SubmissionId = sub.SubmissionId,
                NewStatus = sub.Status,
                ActedAt = history.ActedAt
            }
        };
    }
}



