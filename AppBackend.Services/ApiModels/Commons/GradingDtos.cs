using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons;

public class EvaluationDetailDto
{
    public int? DetailId { get; set; }
    [Required]
    public int RubricId { get; set; }
    [Range(typeof(decimal), "0", "100")]
    public decimal Score { get; set; }
}

public class GradeSubmissionRequestDto
{
    [Required]
    public int ProjectId { get; set; }
    [Required]
    public int InstructorId { get; set; }
    [Range(typeof(decimal), "0", "100")]
    public decimal TotalScore { get; set; }
    public string? Feedback { get; set; }
    public List<EvaluationDetailDto>? Details { get; set; }
}

public class GradeSubmissionResponseDto
{
    public int EvaluationId { get; set; }
    public int ProjectId { get; set; }
    public int InstructorId { get; set; }
    public decimal TotalScore { get; set; }
    public string? Feedback { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public List<EvaluationDetailDto>? Details { get; set; }
}



