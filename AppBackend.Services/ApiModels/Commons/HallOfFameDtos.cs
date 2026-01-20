using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

#region Request DTOs

/// <summary>
/// Request to nominate a project to Hall of Fame
/// </summary>
public class HallOfFameNominateRequestDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int SemesterId { get; set; }

    public int? Rank { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }
}

/// <summary>
/// Request to update Hall of Fame entry
/// </summary>
public class HallOfFameUpdateRequestDto
{
    public int? Rank { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Hall of Fame entry response
/// </summary>
public class HallOfFameResponseDto
{
    public int HofId { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? GroupName { get; set; }
    public int? NominatedBy { get; set; }
    public string? NominatedByName { get; set; }
    public DateTime? NominatedAt { get; set; }
    public int? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int? Rank { get; set; }
    public string? Note { get; set; }
    public decimal? FinalScore { get; set; }
}

/// <summary>
/// Milestone grade information for leaderboard
/// </summary>
public class LeaderboardMilestoneDto
{
    public int MilestoneId { get; set; }
    public string? MilestoneName { get; set; }
    public decimal? Weight { get; set; }
    public decimal Score { get; set; }
    public decimal WeightedScore { get; set; }
}

/// <summary>
/// Grader grade information for leaderboard
/// </summary>
public class LeaderboardGraderDto
{
    public int GraderId { get; set; }
    public string? GraderName { get; set; }
    public string? GraderEmail { get; set; }
    public decimal Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }
}

/// <summary>
/// Final submission information for leaderboard
/// </summary>
public class LeaderboardFinalSubmissionDto
{
    public int FinalSubmissionId { get; set; }
    public decimal? AverageGrade { get; set; }
    public string? FinalReportUrl { get; set; }
    public string? PresentationUrl { get; set; }
    public string? DemoVideoUrl { get; set; }
    public string? SubmissionNotes { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public List<LeaderboardGraderDto> GraderGrades { get; set; } = new();
}

/// <summary>
/// Leaderboard entry for top 10 projects with comprehensive information
/// </summary>
public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectDescription { get; set; }
    public string? ProjectComponent { get; set; }
    public string? GroupName { get; set; }
    public decimal? FinalScore { get; set; }
    public string? SemesterName { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Note { get; set; }
    public bool IsInHallOfFame { get; set; }
    
    // Comprehensive information
    public List<LeaderboardMilestoneDto> Milestones { get; set; } = new();
    public LeaderboardFinalSubmissionDto? FinalSubmission { get; set; }
    public string? SimulatorLink { get; set; }
}

/// <summary>
/// Leaderboard response with top 10 projects
/// </summary>
public class LeaderboardResponseDto
{
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int TotalProjects { get; set; }
    public List<LeaderboardEntryDto> TopProjects { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

#endregion
