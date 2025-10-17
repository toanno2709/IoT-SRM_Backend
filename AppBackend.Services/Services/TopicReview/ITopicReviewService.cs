using AppBackend.Repositories.Repositories.SubmissionRepo;
using AppBackend.Repositories.Repositories.ApprovalHistoryRepo;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.TopicReview;

public interface ITopicReviewService
{
    // TODO: C?n refactor cho phù h?p v?i schema m?i (Group-based, MilestoneSubmission)
    // Task<ResultModel<List<ProposalSummaryDto>>> GetPendingProposalsAsync(int instructorId);
    // Task<ResultModel<ReviewResponseDto>> ReviewProposalAsync(int instructorId, int submissionId, ReviewRequestDto request);
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

    // TODO: Refactor cho phù h?p v?i ki?n trúc m?i
    // Schema m?i:
    // - Project thu?c Group (không có Leader tr?c ti?p)
    // - Submission theo milestone (MilestoneSubmission)
    // - Không còn ProjectSubmission.Status

    /*
    public async Task<ResultModel<List<ProposalSummaryDto>>> GetPendingProposalsAsync(int instructorId)
    {
        // C?n refactor: GetPendingProposalsByInstructorAsync không còn t?n t?i
        // C?n l?y submissions qua Class -> Groups -> Projects -> MilestoneSubmissions
        
        return new ResultModel<List<ProposalSummaryDto>>
        {
            IsSuccess = true,
            Message = "Feature under development",
            Data = new List<ProposalSummaryDto>()
        };
    }

    public async Task<ResultModel<ReviewResponseDto>> ReviewProposalAsync(int instructorId, int submissionId, ReviewRequestDto request)
    {
        // C?n refactor cho MilestoneSubmission
        
        return new ResultModel<ReviewResponseDto>
        {
            IsSuccess = true,
            Message = "Feature under development",
            Data = new ReviewResponseDto()
        };
    }
    */
}



