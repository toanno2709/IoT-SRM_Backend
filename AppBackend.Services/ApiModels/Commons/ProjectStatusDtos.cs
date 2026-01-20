using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Request DTO for instructor to update project status with comment
/// </summary>
public class UpdateProjectStatusRequestDto
{
    /// <summary>
    /// New project status (e.g., "Approved", "Rejected", "Revision Required", "In Progress")
    /// </summary>
    /// <example>Approved</example>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Comment from instructor explaining the status change
    /// Students will be able to see this comment
    /// </summary>
    /// <example>Good project proposal. Please proceed with development.</example>
    [StringLength(1000)]
    public string? Comment { get; set; }
}

/// <summary>
/// Response DTO after updating project status
/// </summary>
public class UpdateProjectStatusResponseDto
{
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string Status { get; set; } = null!;
    public string? Comment { get; set; }
    public int ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime ReviewedAt { get; set; }
}

/// <summary>
/// DTO for project status history (for students to view)
/// </summary>
public class ProjectStatusHistoryDto
{
    public int HistoryId { get; set; }
    public string Status { get; set; } = null!;
    public string? Comment { get; set; }
    public int ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime ReviewedAt { get; set; }
}

/// <summary>
/// Request DTO to search students without groups in a class
/// </summary>
public class SearchUnassignedStudentsRequestDto
{
    /// <summary>
    /// Class ID to search in
    /// </summary>
    [Required]
    public int ClassId { get; set; }

    /// <summary>
    /// Optional search query to filter by student name or email
    /// </summary>
    /// <example>nguyen</example>
    public string? Q { get; set; }
}

/// <summary>
/// Response DTO for unassigned students
/// </summary>
public class UnassignedStudentDto
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public DateTime? EnrolledAt { get; set; }
}

/// <summary>
/// Response wrapper for unassigned students list
/// </summary>
public class UnassignedStudentsResponseDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int TotalUnassignedStudents { get; set; }
    public List<UnassignedStudentDto> Students { get; set; } = new();
}

/// <summary>
/// Request DTO for student to resubmit project after rejection
/// </summary>
public class StudentResubmitProjectDto
{
    /// <summary>
    /// Optional comment from student explaining the changes made
    /// </summary>
    /// <example>We have updated the project description and components as requested.</example>
    [StringLength(1000)]
    public string? Comment { get; set; }
}
