using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

public class ProjectMilestoneCreateRequestDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? Weight { get; set; }
}

public class ProjectMilestoneUpdateRequestDto
{
    [Required]
    public int MilestoneId { get; set; }

    [StringLength(255)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    public string? Status { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? Weight { get; set; }
}

public class ProjectMilestoneResponseDto
{
    public int MilestoneId { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal? Weight { get; set; }
}

/// <summary>
/// Request DTO for creating milestones for all approved projects in a class
/// </summary>
public class BulkCreateMilestoneRequestDto
{
    [Required]
    public int ClassId { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    [Required]
    [Range(typeof(decimal), "0", "100")]
    public decimal Weight { get; set; }
}

/// <summary>
/// Response DTO for bulk milestone creation
/// </summary>
public class BulkCreateMilestoneResponseDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int TotalProjectsProcessed { get; set; }
    public int TotalMilestonesCreated { get; set; }
    public List<BulkMilestoneCreationDetailDto> CreatedMilestones { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Details of a milestone created during bulk operation
/// </summary>
public class BulkMilestoneCreationDetailDto
{
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public int MilestoneId { get; set; }
    public string MilestoneTitle { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}


