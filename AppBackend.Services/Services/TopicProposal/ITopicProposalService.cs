using AppBackend.Repositories.Repositories.MilestoneSubmissionRepo;
using AppBackend.Repositories.Repositories.ProjectApprovalHistoryRepo;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.BusinessObjects.Models;

namespace AppBackend.Services.Services.TopicProposal;

public interface ITopicProposalService
{
    Task<ResultModel<List<TopicProposalResponseDto>>> GetPendingProposalsAsync(int instructorId);
    Task<ResultModel<TopicProposalReviewResponseDto>> ReviewProposalAsync(int instructorId, int submissionId, TopicProposalReviewRequestDto request);
}

public class TopicProposalService : ITopicProposalService
{
    private readonly IMilestoneSubmissionRepository _submissionRepository;
    private readonly IProjectApprovalHistoryRepository _approvalHistoryRepository;

    public TopicProposalService(
        IMilestoneSubmissionRepository submissionRepository,
        IProjectApprovalHistoryRepository approvalHistoryRepository)
    {
        _submissionRepository = submissionRepository;
        _approvalHistoryRepository = approvalHistoryRepository;
    }

    public async Task<ResultModel<List<TopicProposalResponseDto>>> GetPendingProposalsAsync(int instructorId)
    {
        try
        {
            var submissions = await _submissionRepository.GetPendingProposalsByInstructorAsync(instructorId);

            var dtos = submissions.Select(s => new TopicProposalResponseDto
            {
                SubmissionId = s.SubmissionId,
                ProjectId = s.ProjectId,
                ProjectTitle = s.Project?.Title,
                ProjectDescription = s.Project?.Description,
                GroupId = s.Project?.GroupId ?? 0,
                GroupName = s.Project?.Group?.GroupName,
                LeaderId = s.Project?.Group?.LeaderId,
                LeaderName = s.Project?.Group?.Leader?.FullName,
                ProposalNote = null, // MilestoneSubmission không có note field
                Status = GetSubmissionStatus(s),
                SubmittedAt = s.LastSubmittedAt,
                Files = (s.SubmissionFiles ?? new List<SubmissionFile>())
                    .Select(f => new SubmissionFileDto
                    {
                        FileId = f.FileId,
                        FileName = System.IO.Path.GetFileName(f.FileUrl),
                        FileUrl = f.FileUrl,
                        UploadedAt = f.UploadedAt
                    }).ToList()
            }).ToList();

            return new ResultModel<List<TopicProposalResponseDto>>
            {
                IsSuccess = true,
                Message = "Pending proposals retrieved successfully",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<TopicProposalResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving proposals: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<TopicProposalReviewResponseDto>> ReviewProposalAsync(
        int instructorId, 
        int submissionId, 
        TopicProposalReviewRequestDto request)
    {
        try
        {
            // Get submission
            var submission = await _submissionRepository.GetSubmissionWithDetailsAsync(submissionId);
            if (submission == null)
            {
                return new ResultModel<TopicProposalReviewResponseDto>
                {
                    IsSuccess = false,
                    Message = "Submission not found",
                    Data = null
                };
            }

            // Verify instructor owns this class
            if (submission.Project?.Group?.Class?.InstructorId != instructorId)
            {
                return new ResultModel<TopicProposalReviewResponseDto>
                {
                    IsSuccess = false,
                    Message = "Unauthorized to review this proposal",
                    Data = null
                };
            }

            // Create approval history
            var history = new ProjectApprovalHistory
            {
                SubmissionId = submissionId,
                ReviewerId = instructorId,
                Action = request.ReviewStatus,
                Comment = request.ReviewComment,
                ActedAt = DateTime.UtcNow
            };
            await _approvalHistoryRepository.AddApprovalHistoryAsync(history);

            // If approved, update project status
            if (request.ReviewStatus == "Approved" && submission.Project != null)
            {
                submission.Project.Status = "Approved";
                await _submissionRepository.SaveChangesAsync();
            }

            return new ResultModel<TopicProposalReviewResponseDto>
            {
                IsSuccess = true,
                Message = $"Proposal {request.ReviewStatus.ToLower()} successfully",
                Data = new TopicProposalReviewResponseDto
                {
                    HistoryId = history.HistoryId,
                    SubmissionId = submissionId,
                    ReviewStatus = request.ReviewStatus,
                    ReviewComment = request.ReviewComment,
                    ReviewedAt = history.ActedAt ?? DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<TopicProposalReviewResponseDto>
            {
                IsSuccess = false,
                Message = $"Error reviewing proposal: {ex.Message}",
                Data = null
            };
        }
    }

    private string GetSubmissionStatus(MilestoneSubmission submission)
    {
        if (submission.ProjectApprovalHistories == null || !submission.ProjectApprovalHistories.Any())
            return "Pending";

        var latestHistory = submission.ProjectApprovalHistories
            .OrderByDescending(h => h.ActedAt)
            .FirstOrDefault();

        return latestHistory?.Action ?? "Pending";
    }
}


