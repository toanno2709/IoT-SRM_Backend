namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Result summary after checking milestone warnings
/// </summary>
public class MilestoneWarningResultDto
{
    public int TotalClassesChecked { get; set; }
    public int TotalInstructorsNotified { get; set; }
    public int TotalProjectsWithIncompleteWeights { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<ClassWarningDto> ClassWarnings { get; set; } = new();
}

/// <summary>
/// Warning details for a specific class
/// </summary>
public class ClassWarningDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public int ProjectsWithIncompleteWeights { get; set; }
    public List<string> ProjectNames { get; set; } = new();
}

/// <summary>
/// Project with incomplete milestone weights
/// </summary>
public class ProjectMilestoneWarningDto
{
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public decimal TotalWeightPercentage { get; set; }
    public decimal MissingWeightPercentage { get; set; }
    public int TotalMilestones { get; set; }
    public List<MilestoneWeightDto> Milestones { get; set; } = new();
}

/// <summary>
/// Individual milestone weight info
/// </summary>
public class MilestoneWeightDto
{
    public int MilestoneId { get; set; }
    public string? Title { get; set; }
    public int MilestoneOrder { get; set; }
    public decimal WeightPercentage { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsCompleted { get; set; }
}

/// <summary>
/// Complete milestone weight details for a project
/// </summary>
public class ProjectMilestoneWeightDto
{
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public decimal TotalWeightPercentage { get; set; }
    public bool IsComplete { get; set; }
    public decimal MissingWeightPercentage { get; set; }
    public int TotalMilestones { get; set; }
    public List<MilestoneWeightDto> Milestones { get; set; } = new();
    public string? WarningMessage { get; set; }
}
