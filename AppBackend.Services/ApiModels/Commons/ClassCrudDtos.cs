using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

// Detail DTO
public class ClassDetailDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int? InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public int? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public string? SemesterCode { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string Status { get; set; } = "Not Started";
    public DateTime? StartTime { get; set; }
    public int TotalStudents { get; set; }
    public int TotalGroups { get; set; }
    public int TotalProjects { get; set; }
    public List<GroupSummaryDto>? Groups { get; set; }
    public List<StudentEnrollmentDto>? Students { get; set; }
}

// Group summary in class
public class GroupSummaryDto
{
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? LeaderName { get; set; }
    public int MemberCount { get; set; }
    public int ProjectCount { get; set; }
}

// Student enrollment in class
public class StudentEnrollmentDto
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public DateTime? EnrolledAt { get; set; }
}

// Create Request DTO
public class CreateClassRequestDto
{
    [Required(ErrorMessage = "Class name is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Class name must be between 3 and 200 characters")]
    public string ClassName { get; set; } = null!;

    [Required(ErrorMessage = "Semester ID is required")]
    public int SemesterId { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public int? InstructorId { get; set; }

    public DateTime? StartTime { get; set; }
}

// Update Request DTO
public class UpdateClassRequestDto
{
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Class name must be between 3 and 200 characters")]
    public string? ClassName { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public int? InstructorId { get; set; }

    public DateTime? StartTime { get; set; }
}

// Assign Instructor Request DTO
public class AssignInstructorRequestDto
{
    [Required(ErrorMessage = "Instructor ID is required")]
    public int InstructorId { get; set; }
}

// Change Class Status Request DTO
public class ChangeClassStatusRequestDto
{
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(Not Started|In Progress|Completed)$", 
        ErrorMessage = "Status must be 'Not Started', 'In Progress', or 'Completed'")]
    public string Status { get; set; } = null!;
}

// Change Class Status Response DTO
public class ChangeClassStatusResponseDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public string OldStatus { get; set; } = null!;
    public string NewStatus { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
    public int TotalStudents { get; set; }
    public int StudentsWithGroup { get; set; }
    public int StudentsWithoutGroup { get; set; }
    public List<string> Warnings { get; set; } = new();
}
