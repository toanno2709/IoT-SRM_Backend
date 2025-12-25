namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Admin Dashboard Overview Response
/// </summary>
public class AdminDashboardOverviewDto
{
    // T?ng quan h? th?ng
    public int TotalClasses { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalStudents { get; set; }
    public int TotalGroups { get; set; }
    public int TotalProjects { get; set; }
    
    // Quick access stats
    public int ActiveSemesterCount { get; set; }
    public int PendingApprovals { get; set; }
    public int RecentAnnouncements { get; set; }
    public int CompletedProjects { get; set; }
    
    // Recent Activities (Last 10)
    public List<AdminActivityDto> RecentActivities { get; set; } = new();
    
    // Quick Links & Alerts
    public List<SystemAlertDto> SystemAlerts { get; set; } = new();
}

/// <summary>
/// Admin System Statistics
/// </summary>
public class AdminStatisticsDto
{
    // User Statistics
    public UserStatisticsDto UserStats { get; set; } = new();
    
    // Class & Semester Statistics
    public ClassStatisticsDto ClassStats { get; set; } = new();
    
    // Project & Group Statistics
    public ProjectStatisticsDto ProjectStats { get; set; } = new();
    
    // Submission & Grading Statistics
    public AdminSubmissionStatisticsDto SubmissionStats { get; set; } = new();
    
    // System Health
    public SystemHealthDto SystemHealth { get; set; } = new();
}

/// <summary>
/// User Statistics
/// </summary>
public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveUsersLast30Days { get; set; }
    public int NewUsersThisMonth { get; set; }
}

/// <summary>
/// Class Statistics
/// </summary>
public class ClassStatisticsDto
{
    public int TotalClasses { get; set; }
    public int ActiveClasses { get; set; }
    public int TotalSemesters { get; set; }
    public int ActiveSemesters { get; set; }
    public decimal AverageClassSize { get; set; }
    public int ClassesWithoutInstructor { get; set; }
}

/// <summary>
/// Project Statistics
/// </summary>
public class ProjectStatisticsDto
{
    public int TotalProjects { get; set; }
    public int PendingProjects { get; set; }
    public int ApprovedProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int RejectedProjects { get; set; }
    public int TotalGroups { get; set; }
    public decimal AverageGroupSize { get; set; }
    public decimal ProjectCompletionRate { get; set; }
}

/// <summary>
/// Admin Submission Statistics (renamed to avoid conflict)
/// </summary>
public class AdminSubmissionStatisticsDto
{
    public int TotalSubmissions { get; set; }
    public int GradedSubmissions { get; set; }
    public int PendingGrading { get; set; }
    public int LateSubmissions { get; set; }
    public decimal AverageScore { get; set; }
    public decimal SubmissionRate { get; set; }
    public decimal OnTimeSubmissionRate { get; set; }
}

/// <summary>
/// System Health
/// </summary>
public class SystemHealthDto
{
    public int TotalAnnouncements { get; set; }
    public int ActiveAnnouncements { get; set; }
    public DateTime? LastBackupDate { get; set; }
    public int DatabaseSizeInMB { get; set; }
    public string SystemStatus { get; set; } = "Healthy";
}

/// <summary>
/// Admin Activity
/// </summary>
public class AdminActivityDto
{
    public string? ActivityType { get; set; } // "ClassCreated", "UserAdded", "ProjectApproved", etc.
    public string? Description { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime? ActivityDate { get; set; }
    public string? RelatedEntity { get; set; } // Class name, User name, etc.
}

/// <summary>
/// System Alert
/// </summary>
public class SystemAlertDto
{
    public string? AlertType { get; set; } // "Warning", "Info", "Error"
    public string? Message { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsResolved { get; set; }
}

/// <summary>
/// Classes by Semester Chart Data
/// </summary>
public class ClassesBySemesterChartDto
{
    public List<ChartDataPointDto> ChartData { get; set; } = new();
    public string ChartType { get; set; } = "Bar";
    public string Title { get; set; } = "Classes by Semester";
}

/// <summary>
/// Project Distribution Chart Data
/// </summary>
public class ProjectDistributionChartDto
{
    public List<ChartDataPointDto> ChartData { get; set; } = new();
    public string ChartType { get; set; } = "Pie";
    public string Title { get; set; } = "Project Status Distribution";
}

/// <summary>
/// Milestone Completion Chart Data
/// </summary>
public class MilestoneCompletionChartDto
{
    public List<ChartDataPointDto> ChartData { get; set; } = new();
    public string ChartType { get; set; } = "Line";
    public string Title { get; set; } = "Milestone Completion Progress";
}

/// <summary>
/// Generic Chart Data Point
/// </summary>
public class ChartDataPointDto
{
    public string? Label { get; set; }
    public decimal Value { get; set; }
    public string? Color { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}
