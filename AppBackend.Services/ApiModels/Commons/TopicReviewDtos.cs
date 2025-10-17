using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

public class ProposalSummaryDto
{
    public int SubmissionId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }
    public int LeaderId { get; set; }
    public string? LeaderName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public string? Note { get; set; }
}

public class ReviewRequestDto
{
    [Required]
    [RegularExpression("^(Approve|Revision|Reject)$", ErrorMessage = "Action must be Approve, Revision, or Reject")] 
    public string Action { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Comment { get; set; }
}

public class ReviewResponseDto
{
    public int SubmissionId { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime? ActedAt { get; set; }
}



