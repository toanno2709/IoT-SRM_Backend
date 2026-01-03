using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO for displaying all graders and their grades for a project (Student view)
/// </summary>
public class ProjectGradersResponseDto
{
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? ProjectStatus { get; set; }
    
    /// <summary>
    /// Average grade from all graders (null if not graded yet)
    /// </summary>
    public decimal? AverageGrade { get; set; }
    
    /// <summary>
    /// Total number of assigned graders for this class
    /// </summary>
    public int TotalGradersAssigned { get; set; }
    
    /// <summary>
    /// Number of graders who have submitted grades
    /// </summary>
    public int GradersCompleted { get; set; }
    
    /// <summary>
    /// Final submission information
    /// </summary>
    public ProjectFinalSubmissionInfoDto? FinalSubmission { get; set; }
    
    /// <summary>
    /// List of all graders and their grades
    /// </summary>
    public List<GraderGradeDto> Graders { get; set; } = new();
}

/// <summary>
/// Information about final submission
/// </summary>
public class ProjectFinalSubmissionInfoDto
{
    public int FinalSubmissionId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool HasSubmission { get; set; }
    public bool IsGraded { get; set; }
}

/// <summary>
/// Individual grader and their grade
/// </summary>
public class GraderGradeDto
{
    public int GraderId { get; set; }
    public string? GraderName { get; set; }
    public string? GraderEmail { get; set; }
    
    /// <summary>
    /// Grade given by this grader (null if not graded yet)
    /// </summary>
    public decimal? Grade { get; set; }
    
    /// <summary>
    /// Feedback from this grader
    /// </summary>
    public string? Feedback { get; set; }
    
    /// <summary>
    /// When this grader submitted their grade
    /// </summary>
    public DateTime? GradedAt { get; set; }
    
    /// <summary>
    /// Status: NotGraded, Graded
    /// </summary>
    public string Status { get; set; } = "NotGraded";
}
