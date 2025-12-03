using System;
using System.Collections.Generic;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO for instructor to view all submissions for a milestone
/// </summary>
public class InstructorSubmissionViewDto
{
    public int SubmissionId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public int MilestoneDefId { get; set; }
    public string? MilestoneTitle { get; set; }
    public int VersionNo { get; set; }
    public string? Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? LastSubmittedAt { get; set; }
    public int FileCount { get; set; }
    public bool IsGraded { get; set; }
    public decimal? Grade { get; set; }
    public string? SubmittedBy { get; set; }
    public int? SubmittedByUserId { get; set; }
    
    // Additional info for instructor
    public bool IsLateSubmission { get; set; }
    public int? DaysLate { get; set; }
    public bool CanGrade { get; set; }
}

/// <summary>
/// DTO for class-wide submission overview
/// </summary>
public class ClassSubmissionOverviewDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public List<MilestoneSubmissionGroupDto> MilestoneGroups { get; set; } = new();
    public SubmissionStatisticsDto Statistics { get; set; } = new();
}

/// <summary>
/// Groups submissions by milestone
/// </summary>
public class MilestoneSubmissionGroupDto
{
    public int MilestoneDefId { get; set; }
    public string? MilestoneTitle { get; set; }
    public DateTime? Deadline { get; set; }
    public decimal? Weight { get; set; }
    public int TotalGroups { get; set; }
    public int SubmittedGroups { get; set; }
    public int GradedGroups { get; set; }
    public List<InstructorSubmissionViewDto> Submissions { get; set; } = new();
}

/// <summary>
/// Statistics for submission overview
/// </summary>
public class SubmissionStatisticsDto
{
    public int TotalProjects { get; set; }
    public int TotalSubmissions { get; set; }
    public int PendingGrading { get; set; }
    public int GradedSubmissions { get; set; }
    public decimal SubmissionRate { get; set; }
    public decimal GradingProgress { get; set; }
}

/// <summary>
/// DTO for viewing submission files (instructor view)
/// </summary>
public class InstructorSubmissionFilesDto
{
    public int SubmissionId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public string? GroupName { get; set; }
    public int MilestoneDefId { get; set; }
    public string? MilestoneTitle { get; set; }
    public int VersionNo { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? Status { get; set; }
    public bool IsGraded { get; set; }
    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public List<SubmissionFileDetailDto> Files { get; set; } = new();
}

/// <summary>
/// Detailed file information for instructor
/// </summary>
public class SubmissionFileDetailDto
{
    public int FileId { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public long? FileSize { get; set; }
    public string? FileSizeFormatted { get; set; }
    public string? FileType { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
    public int? UploadedByUserId { get; set; }
}

/// <summary>
/// Query parameters for filtering submissions
/// </summary>
public class SubmissionFilterDto
{
    public int? MilestoneDefId { get; set; }
    public string? Status { get; set; } // "Submitted", "Graded", "Late"
    public bool? IsGraded { get; set; }
    public bool? IsLate { get; set; }
    public string? SortBy { get; set; } // "SubmittedAt", "GroupName", "Grade"
    public string? SortOrder { get; set; } // "asc", "desc"
}
