using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

#region Request DTOs

/// <summary>
/// Request to create a new syllabus
/// </summary>
public class SyllabusCreateRequestDto
{
    [Required]
    public int ClassId { get; set; }

    [Required]
    [StringLength(255, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(5000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Version { get; set; }

    [StringLength(20)]
    public string? AcademicYear { get; set; }
}

/// <summary>
/// Request to update syllabus
/// </summary>
public class SyllabusUpdateRequestDto
{
    [StringLength(255, MinimumLength = 3)]
    public string? Title { get; set; }

    [StringLength(5000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Version { get; set; }

    [StringLength(20)]
    public string? AcademicYear { get; set; }

    public bool? IsActive { get; set; }
}

/// <summary>
/// Request to upload file to syllabus
/// </summary>
public class SyllabusFileUploadRequestDto
{
    [Required]
    public int SyllabusId { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FileUrl { get; set; } = string.Empty;

    [StringLength(50)]
    public string? FileType { get; set; }

    public long? FileSize { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }
}

/// <summary>
/// Request to update file info
/// </summary>
public class SyllabusFileUpdateRequestDto
{
    [StringLength(255)]
    public string? FileName { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Syllabus response DTO
/// </summary>
public class SyllabusResponseDto
{
    public int SyllabusId { get; set; }
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? AcademicYear { get; set; }
    public int CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool? IsActive { get; set; }
    public int FileCount { get; set; }
    public List<SyllabusFileDto> Files { get; set; } = new();
}

/// <summary>
/// Syllabus file DTO
/// </summary>
public class SyllabusFileDto
{
    public int FileId { get; set; }
    public int SyllabusId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public string? Description { get; set; }
    public int UploadedBy { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime? UploadedAt { get; set; }
    public int? DisplayOrder { get; set; }
}

/// <summary>
/// Syllabus list item (summary)
/// </summary>
public class SyllabusListItemDto
{
    public int SyllabusId { get; set; }
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? AcademicYear { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool? IsActive { get; set; }
    public int FileCount { get; set; }
}

#endregion
