using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Semester
{
    // Response DTO
    public class SemesterResponseDto
    {
        public int SemesterId { get; set; }
        public string Code { get; set; } = null!;
        public string? Name { get; set; }
        public int? Year { get; set; }
        public string? Term { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool? IsActive { get; set; }
        public int TotalClasses { get; set; }
    }

    // Create Request DTO
    public class CreateSemesterRequestDto
    {
        [Required(ErrorMessage = "Code is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 50 characters")]
        public string Code { get; set; } = null!;

        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string? Name { get; set; }

        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
        public int? Year { get; set; }

        [StringLength(50, ErrorMessage = "Term cannot exceed 50 characters")]
        public string? Term { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public bool? IsActive { get; set; }
    }

    // Update Request DTO
    public class UpdateSemesterRequestDto
    {
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string? Name { get; set; }

        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
        public int? Year { get; set; }

        [StringLength(50, ErrorMessage = "Term cannot exceed 50 characters")]
        public string? Term { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public bool? IsActive { get; set; }
    }

    // Detail DTO (with related data)
    public class SemesterDetailDto
    {
        public int SemesterId { get; set; }
        public string Code { get; set; } = null!;
        public string? Name { get; set; }
        public int? Year { get; set; }
        public string? Term { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool? IsActive { get; set; }
        public int TotalClasses { get; set; }
        public int TotalProjects { get; set; }
        public List<ClassBasicInfoDto>? Classes { get; set; }
    }

    // Basic Class Info for Semester Detail
    public class ClassBasicInfoDto
    {
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? InstructorName { get; set; }
        public int TotalStudents { get; set; }
    }
}
