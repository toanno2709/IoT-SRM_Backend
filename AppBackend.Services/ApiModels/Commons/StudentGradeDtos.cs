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
}

public class ProposalFeedbackDto
{
    public string Status { get; set; } = "Pending";
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Comment { get; set; }
}

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
/// DTO cho overall grade calculation
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

public class MilestoneGradeContributionDto
{
    public string? MilestoneTitle { get; set; }
    public decimal Weight { get; set; }
    public decimal? Grade { get; set; }
    public decimal WeightedScore { get; set; }
    public bool IsGraded { get; set; }
}

/// <summary>
/// Request DTO for exporting class grades to Excel
/// </summary>
public class ExportClassGradesRequestDto
{
    [Required]
    public int ClassId { get; set; }
    
    public bool IncludeMilestoneDetails { get; set; } = true;
    public bool IncludeFeedback { get; set; } = false;
}

/// <summary>
/// Response DTO for class grades export
/// </summary>
public class ExportClassGradesResponseDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public int TotalStudents { get; set; }
    public int TotalProjects { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// DTO for all students grades in a class
/// </summary>
public class ClassGradesReportDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? SemesterName { get; set; }
    public string? InstructorName { get; set; }
    public int TotalStudents { get; set; }
    public int TotalGroups { get; set; }
    public List<StudentGradeReportDto> StudentGrades { get; set; } = new();
    public List<string> MilestoneNames { get; set; } = new();
}

/// <summary>
/// DTO for individual student grade in class report
/// </summary>
public class StudentGradeReportDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? Email { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public Dictionary<string, decimal?> MilestoneGrades { get; set; } = new();
    public decimal? MilestoneAverageGrade { get; set; } // Average of all milestone grades
    public decimal? FinalSubmissionGrade { get; set; }
    public decimal? OverallGrade { get; set; }
    public string? Status { get; set; }
}
