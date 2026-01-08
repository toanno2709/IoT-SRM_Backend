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
    public int? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public string? Status { get; set; }
    public int? FinalSubmissionId { get; set; }
    public decimal? FinalGrade { get; set; }
    public decimal? AverageGradeFromOtherInstructors { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool? IsRetake { get; set; }
    public bool? IsCurrent { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request to create or update student course history
/// </summary>
public class StudentCourseHistoryCreateDto
{
    [Required]
    public int StudentId { get; set; }

    public int? SemesterId { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    public int? FinalSubmissionId { get; set; }

    [Range(0, 100)]
    public decimal? FinalGrade { get; set; }

    [Range(0, 100)]
    public decimal? AverageGradeFromOtherInstructors { get; set; }

    public string? Notes { get; set; }

    public bool? IsRetake { get; set; }
}

/// <summary>
/// Request to update student course history
/// </summary>
public class StudentCourseHistoryUpdateDto
{
    public int? SemesterId { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    public int? FinalSubmissionId { get; set; }

    [Range(0, 100)]
    public decimal? FinalGrade { get; set; }

    [Range(0, 100)]
    public decimal? AverageGradeFromOtherInstructors { get; set; }

    public string? Notes { get; set; }

    public bool? IsRetake { get; set; }

    public bool? IsCurrent { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? EvaluatedAt { get; set; }
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

/// <summary>
/// Result of manually updating IsCurrent flags
/// </summary>
public class UpdateCurrentFlagsResultDto
{
    public int TotalRecordsChecked { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsUnchanged { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string Message { get; set; } = null!;
    public List<CurrentFlagUpdateDetailDto> UpdateDetails { get; set; } = new();
}

/// <summary>
/// Detail of a single IsCurrent flag update
/// </summary>
public class CurrentFlagUpdateDetailDto
{
    public int HistoryId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public bool PreviousIsCurrent { get; set; }
    public bool NewIsCurrent { get; set; }
    public string Reason { get; set; } = null!;
}
