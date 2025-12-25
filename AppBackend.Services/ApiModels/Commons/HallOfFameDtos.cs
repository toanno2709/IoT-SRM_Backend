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
/// Leaderboard entry for top 10 projects
/// </summary>
public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectDescription { get; set; }
    public string? GroupName { get; set; }
    public decimal? FinalScore { get; set; }
    public string? SemesterName { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Note { get; set; }
    public bool IsInHallOfFame { get; set; }
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
