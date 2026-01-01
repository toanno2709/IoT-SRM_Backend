using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO ?? t?o simulation m?i
/// </summary>
public class SimulationCreateRequestDto
{
    [Required(ErrorMessage = "Project ID is required")]
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 255 characters")]
    public string Title { get; set; } = null!;

    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Wokwi Project URL cannot exceed 500 characters")]
    public string? WokwiProjectUrl { get; set; }

    [StringLength(255, ErrorMessage = "Wokwi Project ID cannot exceed 255 characters")]
    public string? WokwiProjectId { get; set; }
}

/// <summary>
/// DTO ?? c?p nh?t simulation
/// </summary>
public class SimulationUpdateRequestDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 255 characters")]
    public string Title { get; set; } = null!;

    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Wokwi Project URL cannot exceed 500 characters")]
    public string? WokwiProjectUrl { get; set; }

    [StringLength(255, ErrorMessage = "Wokwi Project ID cannot exceed 255 characters")]
    public string? WokwiProjectId { get; set; }

    [RegularExpression("^(draft|submitted|graded)$", ErrorMessage = "Status must be one of: draft, submitted, graded")]
    public string? Status { get; set; }
}

/// <summary>
/// Response DTO cho simulation
/// </summary>
public class SimulationResponseDto
{
    public int SimulationId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public string? GroupName { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = "draft";
    public string? WokwiProjectUrl { get; set; }
    public string? WokwiProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response sau khi t?o simulation
/// </summary>
public class SimulationCreateResponseDto
{
    public int SimulationId { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public string Status { get; set; } = "draft";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO ?? thay ??i status c?a simulation
/// </summary>
public class SimulationStatusUpdateDto
{
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(draft|submitted|graded)$", ErrorMessage = "Status must be one of: draft, submitted, graded")]
    public string Status { get; set; } = null!;
}
