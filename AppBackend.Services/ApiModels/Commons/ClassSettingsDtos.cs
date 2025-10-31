using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO để cập nhật class settings
/// </summary>
public class ClassSettingsUpdateRequestDto
{
    [Range(1, 100, ErrorMessage = "Max groups must be between 1 and 100")]
    public int? MaxGroups { get; set; }

    [Range(1, 20, ErrorMessage = "Max members per group must be between 1 and 20")]
    public int? MaxMembersPerGroup { get; set; }

    [Range(1, 20, ErrorMessage = "Min members per group must be between 1 and 20")]
    public int? MinMembersPerGroup { get; set; }
}

/// <summary>
/// Response sau khi cập nhật settings
/// </summary>
public class ClassSettingsResponseDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int? MaxGroups { get; set; }
    public int? MaxMembersPerGroup { get; set; }
    public int? MinMembersPerGroup { get; set; }
    
    // Thông tin hiện tại để compare
    public int CurrentGroupCount { get; set; }
    public int? LargestGroupSize { get; set; }
    public int? SmallestGroupSize { get; set; }
    
    // Warnings nếu có
    public List<string> Warnings { get; set; } = new();
}


