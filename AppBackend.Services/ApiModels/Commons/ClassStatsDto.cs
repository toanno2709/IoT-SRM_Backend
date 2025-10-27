using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

public class ClassStatsResponseDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int TotalStudents { get; set; }
    public int TotalProjects { get; set; }
    public int SubmittedProjects { get; set; }
    public int PendingProjects { get; set; }
    public int ApprovedProjects { get; set; }
    public int RejectedProjects { get; set; }
    public double SubmissionRate { get; set; } // Percentage
    public double AverageScore { get; set; }
    public int EvaluatedProjects { get; set; }
    public DateTime? LastSubmissionDate { get; set; }
    public List<ProjectStatsDto>? ProjectDetails { get; set; }
}

public class ProjectStatsDto
{
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public string? LeaderName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public DateTime? LastSubmissionDate { get; set; }
    public int MemberCount { get; set; }
}

