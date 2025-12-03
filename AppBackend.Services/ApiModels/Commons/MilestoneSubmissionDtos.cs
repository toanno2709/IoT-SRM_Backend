using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Request DTO for submitting a milestone
/// </summary>
public class MilestoneSubmissionRequestDto
{
    [Required(ErrorMessage = "Project ID is required")]
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Milestone ID is required")]
    public int MilestoneId { get; set; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [StringLength(2000, ErrorMessage = "Submission notes cannot exceed 2000 characters")]
    public string? SubmissionNotes { get; set; }

    // File URLs will be added separately after upload
}

/// <summary>
/// Response DTO after submission
/// </summary>
public class MilestoneSubmissionResponseDto
{
    public int SubmissionId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int MilestoneId { get; set; }
    public string? MilestoneTitle { get; set; }
    public int Version { get; set; }
    public int SubmittedBy { get; set; }
    public string? SubmittedByName { get; set; }
    public string? Description { get; set; }
    public string? SubmissionNotes { get; set; }
    public DateTime SubmittedAt { get; set; }
    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public int? GradedBy { get; set; }
    public string? GradedByName { get; set; }
    public DateTime? GradedAt { get; set; }
    public string Status { get; set; } = "Submitted"; // Submitted, Graded
    public bool CanResubmit { get; set; }
    public DateTime? Deadline { get; set; }
    public List<MilestoneFileDto> Files { get; set; } = new();
}

/// <summary>
/// DTO for submission file information
/// </summary>
public class MilestoneFileDto
{
    public int FileId { get; set; }
    public int SubmissionId { get; set; }
    public string FileName { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public long FileSize { get; set; }
    public string? FileType { get; set; }
    public int UploadedBy { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// DTO for submission history
/// </summary>
public class MilestoneSubmissionHistoryDto
{
    public int MilestoneId { get; set; }
    public string? MilestoneTitle { get; set; }
    public decimal? Weight { get; set; }
    public DateTime? Deadline { get; set; }
    public int TotalSubmissions { get; set; }
    public MilestoneSubmissionResponseDto? LatestSubmission { get; set; }
    public List<MilestoneSubmissionResponseDto> AllVersions { get; set; } = new();
}

/// <summary>
/// Request DTO for file upload
/// </summary>
public class FileUploadRequestDto
{
    [Required(ErrorMessage = "Submission ID is required")]
    public int SubmissionId { get; set; }

    // Files will be handled as IFormFile in controller
}

/// <summary>
/// Response DTO after file upload
/// </summary>
public class FileUploadResponseDto
{
    public List<MilestoneFileDto> UploadedFiles { get; set; } = new();
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}
