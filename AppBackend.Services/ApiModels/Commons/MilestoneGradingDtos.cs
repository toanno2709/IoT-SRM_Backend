using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

public class MilestoneGradeRequestDto
{
    [Required]
    public int ProjectId { get; set; }
    
    [Required]
    public int MilestoneDefId { get; set; }
    
    [Required]
    public int InstructorId { get; set; }
    
    [Required]
    [Range(typeof(decimal), "0", "100")]
    public decimal Score { get; set; }
    
    public string? Feedback { get; set; }
    
    /// <summary>
    /// Weight ratio snapshot - lưu trọng số tại thời điểm chấm
    /// </summary>
    [Required]
    [Range(typeof(decimal), "0", "100")]
    public decimal WeightRatioSnapshot { get; set; }
}

public class MilestoneGradeResponseDto
{
    public int MeId { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int MilestoneDefId { get; set; }
    public string? MilestoneTitle { get; set; }
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public decimal WeightRatioSnapshot { get; set; }
    public decimal Score { get; set; }
    public string? Feedback { get; set; }
    public DateTime EvaluatedAt { get; set; }
}



