using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Request DTO for submitting final project deliverables
/// </summary>
public class FinalProjectSubmissionRequestDto
{
    [StringLength(2000, ErrorMessage = "Submission notes cannot exceed 2000 characters")]
    public string? SubmissionNotes { get; set; }

    [Url(ErrorMessage = "Repository URL must be a valid URL")]
    [StringLength(500)]
    public string? RepositoryUrl { get; set; }

    // Files will be uploaded separately via multipart/form-data
    // URLs will be stored after upload to Cloudinary
}

/// <summary>
/// Request wrapper for file uploads (Swagger compatible)
/// </summary>
public class FinalProjectFileUploadRequest
{
    public IFormFile? FinalReport { get; set; }
    public IFormFile? Presentation { get; set; }
    public IFormFile? SourceCode { get; set; }
    public IFormFile? VideoDemo { get; set; }
}

/// <summary>
/// Request DTO for updating final project submission (before deadline)
/// </summary>
public class FinalProjectUpdateRequestDto
{
    [StringLength(2000, ErrorMessage = "Submission notes cannot exceed 2000 characters")]
    public string? SubmissionNotes { get; set; }

    [Url(ErrorMessage = "Repository URL must be a valid URL")]
    [StringLength(500)]
    public string? RepositoryUrl { get; set; }
}

/// <summary>
/// Response DTO for final project submission
/// </summary>
public class FinalProjectSubmissionResponseDto
{
    public int FinalSubmissionId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public string? GroupName { get; set; }
    
    // File URLs
    public string? FinalReportUrl { get; set; }
    public string? PresentationUrl { get; set; }
    public string? SourceCodeUrl { get; set; }
    public string? VideoDemoUrl { get; set; }
    public string? RepositoryUrl { get; set; }
    
    // Submission info
    public string? SubmissionNotes { get; set; }
    public int SubmittedBy { get; set; }
    public string? SubmittedByName { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    
    // Grading info
    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public int? GradedBy { get; set; }
    public string? GradedByName { get; set; }
    public DateTime? GradedAt { get; set; }
    
    public string Status { get; set; } = "Submitted"; // Submitted, Graded
    
    // Metadata
    public bool CanUpdate { get; set; } // Can update if before deadline
    public DateTime? Deadline { get; set; }
}

/// <summary>
/// Response DTO for file upload in final submission
/// </summary>
public class FinalProjectFileUploadResponseDto
{
    public string? FinalReportUrl { get; set; }
    public string? PresentationUrl { get; set; }
    public string? SourceCodeUrl { get; set; }
    public string? VideoDemoUrl { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}

/// <summary>
/// Request DTO for grading final project (Instructor only)
/// </summary>
public class FinalProjectGradeRequestDto
{
    [Required(ErrorMessage = "Grade is required")]
    [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100")]
    public decimal Grade { get; set; }

    [StringLength(2000, ErrorMessage = "Feedback cannot exceed 2000 characters")]
    public string? Feedback { get; set; }
}
