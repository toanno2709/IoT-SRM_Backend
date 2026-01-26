namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Comprehensive project information for semester-wide view
/// Includes all project details, simulations, grades, and feedback
/// </summary>
public class ComprehensiveProjectDto
{
    // Project basic info
    public int ProjectId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Component { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Group info
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public int? LeaderId { get; set; }
    public string? LeaderName { get; set; }
    public List<ProjectMemberSimpleDto> Members { get; set; } = new();
    
    // Class info
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? InstructorName { get; set; }
    
    // IoT Simulations
    public List<SimulationInfoDto> Simulations { get; set; } = new();
    
    // Final submission info
    public FinalSubmissionInfoDto? FinalSubmission { get; set; }
    
    // Grader grades
    public List<GraderGradeInfoDto> GraderGrades { get; set; } = new();
    public decimal? AverageGraderGrade { get; set; }
}

/// <summary>
/// Simple member information
/// </summary>
public class ProjectMemberSimpleDto
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? StudentCode { get; set; }
    public string? RoleInGroup { get; set; }
}

/// <summary>
/// Simulation information for project
/// </summary>
public class SimulationInfoDto
{
    public int SimulationId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? WokwiProjectUrl { get; set; }
    public string? WokwiProjectId { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Final submission information
/// </summary>
public class FinalSubmissionInfoDto
{
    public int FinalSubmissionId { get; set; }
    public string? FinalReportUrl { get; set; }
    public string? PresentationUrl { get; set; }
    public string? SourceCodeUrl { get; set; }
    public string? VideoDemoUrl { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? SubmissionNotes { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    
    // Main instructor grade
    public decimal? InstructorGrade { get; set; }
    public string? InstructorFeedback { get; set; }
    public string? GradedByInstructorName { get; set; }
    public DateTime? InstructorGradedAt { get; set; }
}

/// <summary>
/// Grader grade information
/// </summary>
public class GraderGradeInfoDto
{
    public int GradeId { get; set; }
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorEmail { get; set; }
    public decimal Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime GradedAt { get; set; }
}

/// <summary>
/// Response for semester projects query
/// </summary>
public class SemesterProjectsResponseDto
{
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public string? SemesterCode { get; set; }
    public int TotalProjects { get; set; }
    public List<ComprehensiveProjectDto> Projects { get; set; } = new();
}
