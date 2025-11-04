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
        
        /// <summary>
        /// Start date in ISO 8601 format (YYYY-MM-DD). Example: "2025-06-01"
        /// </summary>
        /// <example>2025-06-01</example>
        public DateOnly? StartDate { get; set; }
        
        /// <summary>
        /// End date in ISO 8601 format (YYYY-MM-DD). Example: "2025-08-15"
        /// </summary>
        /// <example>2025-08-15</example>
        public DateOnly? EndDate { get; set; }
        
        public bool? IsActive { get; set; }
        public int TotalClasses { get; set; }
    }

    // Create Request DTO
    public class CreateSemesterRequestDto
    {
        /// <summary>
        /// Semester code (unique identifier)
        /// </summary>
        /// <example>SU25</example>
        [Required(ErrorMessage = "Code is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 50 characters")]
        public string Code { get; set; } = null!;

        /// <summary>
        /// Semester name
        /// </summary>
        /// <example>Summer 2025</example>
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string? Name { get; set; }

        /// <summary>
        /// Academic year
        /// </summary>
        /// <example>2025</example>
        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
        public int? Year { get; set; }

        /// <summary>
        /// Semester term (Spring, Summer, Fall, Winter)
        /// </summary>
        /// <example>Summer</example>
        [StringLength(50, ErrorMessage = "Term cannot exceed 50 characters")]
        public string? Term { get; set; }

        /// <summary>
        /// Semester start date in ISO 8601 format (YYYY-MM-DD). Example: "2025-06-01"
        /// </summary>
        /// <example>2025-06-01</example>
        public DateOnly? StartDate { get; set; }

        /// <summary>
        /// Semester end date in ISO 8601 format (YYYY-MM-DD). Example: "2025-08-15"
        /// Must be after StartDate
        /// </summary>
        /// <example>2025-08-15</example>
        public DateOnly? EndDate { get; set; }

        /// <summary>
        /// Whether this semester is active. Only one semester can be active at a time
        /// </summary>
        /// <example>true</example>
        public bool? IsActive { get; set; }
    }

    // Update Request DTO
    public class UpdateSemesterRequestDto
    {
        /// <summary>
        /// Semester name
        /// </summary>
        /// <example>Summer 2025 - Updated</example>
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string? Name { get; set; }

        /// <summary>
        /// Academic year
        /// </summary>
        /// <example>2025</example>
        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
        public int? Year { get; set; }

        /// <summary>
        /// Semester term (Spring, Summer, Fall, Winter)
        /// </summary>
        /// <example>Summer</example>
        [StringLength(50, ErrorMessage = "Term cannot exceed 50 characters")]
        public string? Term { get; set; }

        /// <summary>
        /// Semester start date in ISO 8601 format (YYYY-MM-DD). Example: "2025-06-05"
        /// </summary>
        /// <example>2025-06-05</example>
        public DateOnly? StartDate { get; set; }

        /// <summary>
        /// Semester end date in ISO 8601 format (YYYY-MM-DD). Example: "2025-08-20"
        /// Must be after StartDate
        /// </summary>
        /// <example>2025-08-20</example>
        public DateOnly? EndDate { get; set; }

        /// <summary>
        /// Whether this semester is active. Only one semester can be active at a time
        /// </summary>
        /// <example>true</example>
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
        
        /// <summary>
        /// Start date in ISO 8601 format (YYYY-MM-DD). Example: "2025-06-01"
        /// </summary>
        /// <example>2025-06-01</example>
        public DateOnly? StartDate { get; set; }
        
        /// <summary>
        /// End date in ISO 8601 format (YYYY-MM-DD). Example: "2025-08-15"
        /// </summary>
        /// <example>2025-08-15</example>
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
