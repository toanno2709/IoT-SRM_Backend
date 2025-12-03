using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO for instructor to get approved projects in assigned grading classes
/// </summary>
public class ApprovedProjectForGradingDto
{
    public int ProjectId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Component { get; set; }
    
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    
    // Final submission info
    public bool HasFinalSubmission { get; set; }
    public int? FinalSubmissionId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    
    // Grading status
    public bool HasMyGrade { get; set; }
    public decimal? MyGrade { get; set; }
    public decimal? AverageGrade { get; set; }
    public int TotalGradesCount { get; set; }
    public string? GradingStatus { get; set; } // "Not Graded", "Partially Graded", "Fully Graded"
}

/// <summary>
/// DTO for grading class assigned to instructor
/// </summary>
public class GradingClassDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? Description { get; set; }
    
    public int? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    
    public int? MainInstructorId { get; set; }
    public string? MainInstructorName { get; set; }
    
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Statistics
    public int TotalProjects { get; set; }
    public int ApprovedProjects { get; set; }
    public int ProjectsWithFinalSubmission { get; set; }
    public int ProjectsIGraded { get; set; }
    public int ProjectsPendingMyGrade { get; set; }
}

/// <summary>
/// Request DTO for grading final project by assigned grader
/// </summary>
public class GraderFinalProjectGradeRequestDto
{
    [Required(ErrorMessage = "Grade is required")]
    [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100")]
    public decimal Grade { get; set; }

    [StringLength(2000, ErrorMessage = "Feedback cannot exceed 2000 characters")]
    public string? Feedback { get; set; }
}

/// <summary>
/// Response DTO for grading operation
/// </summary>
public class GraderFinalProjectGradeResponseDto
{
    public int GradeId { get; set; }
    public int FinalSubmissionId { get; set; }
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public decimal Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime GradedAt { get; set; }
    
    // Average info
    public decimal? AverageGrade { get; set; }
    public int TotalGradesCount { get; set; }
    public List<InstructorGradeDto> AllGrades { get; set; } = new();
}

/// <summary>
/// Individual instructor grade info
/// </summary>
public class InstructorGradeDto
{
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public decimal Grade { get; set; }
    public DateTime GradedAt { get; set; }
}

/// <summary>
/// Detailed final submission info for grader
/// </summary>
public class GraderFinalSubmissionDetailDto
{
    public int FinalSubmissionId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public List<string> GroupMembers { get; set; } = new();
    
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    
    // Submission files
    public string? FinalReportUrl { get; set; }
    public string? PresentationUrl { get; set; }
    public string? SourceCodeUrl { get; set; }
    public string? VideoDemoUrl { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? SubmissionNotes { get; set; }
    
    public DateTime SubmittedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    
    // Grading info
    public decimal? AverageGrade { get; set; }
    public List<InstructorGradeDto> AllGrades { get; set; } = new();
    public bool HasMyGrade { get; set; }
    public decimal? MyGrade { get; set; }
    public string? MyFeedback { get; set; }
    public DateTime? MyGradedAt { get; set; }
}
