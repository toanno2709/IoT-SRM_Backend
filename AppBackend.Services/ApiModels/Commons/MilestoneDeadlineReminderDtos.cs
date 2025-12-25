namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Result summary after checking milestone deadlines
/// </summary>
public class MilestoneDeadlineReminderResultDto
{
    public int TotalMilestonesChecked { get; set; }
    public int TotalStudentsNotified { get; set; }
    public int TotalReminders7Days { get; set; }
    public int TotalReminders3Days { get; set; }
    public int TotalReminders1Day { get; set; }
    public int TotalOverdueReminders { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<MilestoneReminderDetailDto> ReminderDetails { get; set; } = new();
}

/// <summary>
/// Details of reminders sent for a specific milestone
/// </summary>
public class MilestoneReminderDetailDto
{
    public int MilestoneId { get; set; }
    public string? MilestoneTitle { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? ClassName { get; set; }
    public DateOnly? DueDate { get; set; }
    public int DaysUntilDue { get; set; }
    public string ReminderType { get; set; } = string.Empty; // "7days", "3days", "1day", "overdue"
    public int StudentsNotified { get; set; }
    public List<string> StudentNames { get; set; } = new();
}

/// <summary>
/// Upcoming milestone for a student
/// </summary>
public class StudentUpcomingMilestoneDto
{
    public int MilestoneId { get; set; }
    public string? MilestoneTitle { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public DateOnly? DueDate { get; set; }
    public int DaysUntilDue { get; set; }
    public decimal? WeightPercentage { get; set; }
    public bool HasSubmitted { get; set; }
    public DateTime? LastSubmittedAt { get; set; }
    public string UrgencyLevel { get; set; } = string.Empty; // "low", "medium", "high", "critical"
}

/// <summary>
/// Overdue milestone for a student
/// </summary>
public class StudentOverdueMilestoneDto
{
    public int MilestoneId { get; set; }
    public string? MilestoneTitle { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public DateOnly? DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public decimal? WeightPercentage { get; set; }
    public bool HasSubmitted { get; set; }
    public DateTime? LastSubmittedAt { get; set; }
}

/// <summary>
/// Result of checking deadlines for a specific class
/// </summary>
public class ClassDeadlineCheckResultDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public int TotalMilestonesChecked { get; set; }
    public int TotalStudentsNotified { get; set; }
    public int TotalReminders { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<MilestoneReminderDetailDto> ReminderDetails { get; set; } = new();
}
