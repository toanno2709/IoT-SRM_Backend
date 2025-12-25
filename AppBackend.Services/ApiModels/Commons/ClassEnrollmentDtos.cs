using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Request DTO to bulk add students to a class
/// </summary>
public class BulkAddStudentsRequestDto
{
    /// <summary>
    /// Class ID to add students to
    /// </summary>
    [Required]
    public int ClassId { get; set; }

    /// <summary>
    /// Maximum number of students to add to the class
    /// </summary>
    [Required]
    [Range(1, 1000, ErrorMessage = "Max members must be between 1 and 1000")]
    public int MaxMembers { get; set; }
}

/// <summary>
/// Response DTO after bulk adding students
/// </summary>
public class BulkAddStudentsResponseDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int PreviousStudentCount { get; set; }
    public int NewStudentsAdded { get; set; }
    public int TotalStudentsNow { get; set; }
    public int StudentsNotAdded { get; set; }
    public string? Message { get; set; }
    public List<StudentEnrollmentDto> AddedStudents { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Response DTO for getting list of students in a class with group status
/// </summary>
public class ClassStudentsWithGroupResponseDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int TotalStudents { get; set; }
    public int StudentsWithGroup { get; set; }
    public int StudentsWithoutGroup { get; set; }
    public List<StudentWithGroupDto> Students { get; set; } = new();
}

/// <summary>
/// DTO for student with group information
/// </summary>
public class StudentWithGroupDto
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public DateTime? EnrolledAt { get; set; }
    public bool HasGroup { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? RoleInGroup { get; set; }
    public DateTime? JoinedGroupAt { get; set; }
}

/// <summary>
/// Response DTO for getting list of students in a class
/// </summary>
public class ClassStudentsResponseDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int TotalStudents { get; set; }
    public List<StudentEnrollmentDto> Students { get; set; } = new();
}

/// <summary>
/// Request DTO to add one student to a class
/// </summary>
public class AddStudentToClassRequestDto
{
    [Required]
    public int StudentId { get; set; }
}

/// <summary>
/// Response DTO after adding one student
/// </summary>
public class AddStudentToClassResponseDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? Email { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// DTO for student import from Excel row
/// </summary>
public class StudentImportRowDto
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Status { get; set; }
}

/// <summary>
/// DTO for import students result
/// </summary>
public class ImportStudentsResultDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<ImportStudentSuccessDto> SuccessfulStudents { get; set; } = new();
    public List<ImportStudentFailureDto> FailedStudents { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for successful import
/// </summary>
public class ImportStudentSuccessDto
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int UserId { get; set; }
}

/// <summary>
/// DTO for failed import with reason
/// </summary>
public class ImportStudentFailureDto
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty; // EMAIL_NOT_FOUND, DUPLICATE, NOT_STUDENT, ALREADY_PASSED, INVALID_STATUS
}
