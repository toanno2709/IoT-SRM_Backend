using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.TopicProposal;

public interface ITopicProposalService
{
    Task<ResultModel<List<TopicProposalResponseDto>>> GetPendingProposalsAsync(int instructorId);
    Task<ResultModel<TopicProposalReviewResponseDto>> ReviewProposalAsync(int instructorId, int submissionId, TopicProposalReviewRequestDto request);
}


