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
