using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

public class ClassResponseDto
{
    public int ClassId { get; set; }
    
    [Required]
    [StringLength(255)]
    public string ClassName { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int? InstructorId { get; set; }
    
    public string? InstructorName { get; set; }
    
    public int? SemesterId { get; set; }
    
    public string? SemesterName { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    
    public int TotalStudents { get; set; }
    
    public int TotalProjects { get; set; }
}




