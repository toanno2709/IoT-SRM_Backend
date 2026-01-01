using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Student course history response DTO
/// </summary>
public class StudentCourseHistoryResponseDto
{
    public int HistoryId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentEmail { get; set; }
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }
    public string Status { get; set; } = "Not Started";
    public int? FinalSubmissionId { get; set; }
    public decimal? FinalGrade { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public int? EvaluatedBy { get; set; }
    public string? EvaluatedByName { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsRetake { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request to create or update student course history
/// </summary>
public class StudentCourseHistoryCreateDto
{
    [Required]
    public int StudentId { get; set; }

    public int? ClassId { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Not Started";

    public int? FinalSubmissionId { get; set; }

    [Range(0, 100)]
    public decimal? FinalGrade { get; set; }

    public int? EvaluatedBy { get; set; }

    public string? Notes { get; set; }

    public bool IsRetake { get; set; }
}

/// <summary>
/// Request to update student course history
/// </summary>
public class StudentCourseHistoryUpdateDto
{
    public int? ClassId { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    public int? FinalSubmissionId { get; set; }

    [Range(0, 100)]
    public decimal? FinalGrade { get; set; }

    public int? EvaluatedBy { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// Request to update student course status
/// </summary>
public class UpdateStudentCourseStatusDto
{
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public int? EvaluatedBy { get; set; }
}

/// <summary>
/// List students by course completion status
/// </summary>
public class StudentsByStatusResponseDto
{
    public string Status { get; set; } = null!;
    public int Count { get; set; }
    public List<StudentCourseHistoryResponseDto> Students { get; set; } = new();
}
