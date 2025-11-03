using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO cho t?t c? ?i?m c?a student
/// </summary>
public class StudentGradesResponseDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? Email { get; set; }
    public List<StudentProjectGradeDto> Projects { get; set; } = new();
}

/// <summary>
/// DTO cho ?i?m c?a m?t project
/// </summary>
public class StudentProjectGradeDto
{
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? SemesterName { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public string ProjectStatus { get; set; } = "InProgress"; // InProgress, Completed, Failed
    public decimal? OverallGrade { get; set; } // Weighted average
    public List<StudentMilestoneGradeDto> Milestones { get; set; } = new();
    public WeightedGradeBreakdownDto GradeBreakdown { get; set; } = new();
}

/// <summary>
/// DTO cho ?i?m m?t milestone
/// </summary>
public class StudentMilestoneGradeDto
{
    public int MilestoneId { get; set; }
    public string? MilestoneTitle { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }
    public string? GradedBy { get; set; }
    public string Status { get; set; } = "NotSubmitted"; // NotSubmitted, Submitted, Graded
}

/// <summary>
/// DTO cho breakdown ?i?m có tr?ng s?
/// </summary>
public class WeightedGradeBreakdownDto
{
    public Dictionary<string, decimal> MilestoneScores { get; set; } = new();
    public decimal TotalWeightedScore { get; set; }
    public decimal TotalWeight { get; set; }
    public decimal? ProjectedFinalGrade { get; set; }
}

/// <summary>
/// DTO cho feedback c?a project
/// </summary>
public class ProjectFeedbackResponseDto
{
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public string? ProjectStatus { get; set; }
    public ProposalFeedbackDto? ProposalFeedback { get; set; }
    public List<MilestoneFeedbackDto> MilestoneFeedback { get; set; } = new();
    public string? FinalFeedback { get; set; }
}

/// <summary>
/// DTO cho proposal feedback
/// </summary>
public class ProposalFeedbackDto
{
    public string Status { get; set; } = "Pending"; // Pending, Approved, Revision, Rejected
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Comment { get; set; }
}

/// <summary>
/// DTO cho milestone feedback
/// </summary>
public class MilestoneFeedbackDto
{
    public int MilestoneId { get; set; }
    public string? MilestoneTitle { get; set; }
    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public string? GradedBy { get; set; }
    public DateTime? GradedAt { get; set; }
}

/// <summary>
/// DTO cho overall grade c?a project
/// </summary>
public class ProjectOverallGradeDto
{
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public decimal? OverallGrade { get; set; }
    public string CalculationMethod { get; set; } = "WeightedAverage";
    public List<MilestoneGradeContributionDto> MilestoneContributions { get; set; } = new();
    public bool IsComplete { get; set; }
    public int TotalMilestones { get; set; }
    public int GradedMilestones { get; set; }
}

/// <summary>
/// DTO cho contribution c?a milestone vào overall grade
/// </summary>
public class MilestoneGradeContributionDto
{
    public string? MilestoneTitle { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Grade { get; set; }
    public decimal WeightedScore { get; set; }
    public bool IsGraded { get; set; }
}
