using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO cho đề xuất topic (dùng MilestoneSubmission)
/// </summary>
public class TopicProposalResponseDto
{
    public int SubmissionId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public string? ProjectDescription { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public int? LeaderId { get; set; }
    public string? LeaderName { get; set; }
    public string? ProposalNote { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? SubmittedAt { get; set; }
    public List<SubmissionFileDto> Files { get; set; } = new();
}

public class SubmissionFileDto
{
    public int FileId { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? UploadedAt { get; set; }
}

public class TopicProposalReviewRequestDto
{
    [Required]
    [RegularExpression("^(Approved|Rejected|Revision)$", ErrorMessage = "Status must be Approved, Rejected, or Revision")]
    public string ReviewStatus { get; set; } = string.Empty;
    
    public string? ReviewComment { get; set; }
}

public class TopicProposalReviewResponseDto
{
    public int HistoryId { get; set; }
    public int SubmissionId { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public string? ReviewComment { get; set; }
    public DateTime ReviewedAt { get; set; }
}



