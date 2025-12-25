using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Request DTO for assigning a grader to a class
/// </summary>
public class AssignClassGraderRequestDto
{
    [Required(ErrorMessage = "Class ID is required")]
    public int ClassId { get; set; }

    [Required(ErrorMessage = "Instructor ID is required")]
    public int InstructorId { get; set; }
}

/// <summary>
/// Request DTO for bulk assigning graders to a class
/// </summary>
public class BulkAssignGradersRequestDto
{
    [Required(ErrorMessage = "Class ID is required")]
    public int ClassId { get; set; }

    [Required(ErrorMessage = "At least one instructor ID is required")]
    [MinLength(1, ErrorMessage = "At least one instructor ID is required")]
    public List<int> InstructorIds { get; set; } = new();
}

/// <summary>
/// Detailed class grader assignment information
/// </summary>
public class ClassGraderDetailDto
{
    public int GraderId { get; set; }
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? ClassDescription { get; set; }
    
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorEmail { get; set; }
    
    public DateTime AssignedAt { get; set; }
    public int? AssignedBy { get; set; }
    public string? AssignedByName { get; set; }
    
    public bool IsActive { get; set; }
    
    // Statistics
    public int TotalFinalSubmissions { get; set; }
    public int GradedByThisInstructor { get; set; }
    public int PendingGrades { get; set; }
}

/// <summary>
/// Summary of grader assignments with statistics
/// </summary>
public class ClassGraderSummaryDto
{
    public int GraderId { get; set; }
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorEmail { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime AssignedAt { get; set; }
    
    // Grading workload
    public int TotalApprovedProjects { get; set; }
    public int TotalFinalSubmissions { get; set; }
    public int GradedCount { get; set; }
    public int PendingCount { get; set; }
    public decimal CompletionPercentage { get; set; }
}

/// <summary>
/// Comprehensive grading statistics for a class
/// </summary>
public class ClassGradingStatisticsDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    
    // Grader information
    public int TotalAssignedGraders { get; set; }
    public int ActiveGraders { get; set; }
    public List<GraderWorkloadDto> GraderWorkloads { get; set; } = new();
    
    // Project statistics
    public int TotalProjects { get; set; }
    public int ApprovedProjects { get; set; }
    public int ProjectsWithFinalSubmission { get; set; }
    
    // Grading statistics
    public int TotalGradesSubmitted { get; set; }
    public int FullyGradedProjects { get; set; } // Graded by all assigned graders
    public int PartiallyGradedProjects { get; set; } // Graded by some graders
    public int UngradedProjects { get; set; } // Not graded by any grader
    
    public decimal AverageGrade { get; set; }
    public decimal? HighestGrade { get; set; }
    public decimal? LowestGrade { get; set; }
    
    // Progress
    public decimal GradingCompletionPercentage { get; set; }
}

/// <summary>
/// Individual grader workload information
/// </summary>
public class GraderWorkloadDto
{
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorEmail { get; set; }
    public bool IsActive { get; set; }
    
    public int TotalAssignedSubmissions { get; set; }
    public int GradedCount { get; set; }
    public int PendingCount { get; set; }
    public decimal CompletionPercentage { get; set; }
    public decimal? AverageGradeGiven { get; set; }
}

/// <summary>
/// Response for bulk assign graders operation
/// </summary>
public class BulkAssignGradersResponseDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TotalAttempted { get; set; }
    
    public List<BulkAssignResultDto> Results { get; set; } = new();
}

/// <summary>
/// Individual result for bulk assign operation
/// </summary>
public class BulkAssignResultDto
{
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? GraderId { get; set; } // If successful
}
