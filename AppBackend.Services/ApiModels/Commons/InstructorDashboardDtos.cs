namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Dashboard overview cho Instructor
/// </summary>
public class InstructorDashboardResponseDto
{
    // Tổng quan
    public int TotalClasses { get; set; }
    public int TotalGroups { get; set; }
    public int TotalProjects { get; set; }
    public int TotalStudents { get; set; }

    // Công việc cần làm
    public int PendingProposals { get; set; }
    public int SubmissionsToGrade { get; set; }
    public int RecentAnnouncements { get; set; }

    // Lớp học gần đây
    public List<RecentClassDto> RecentClasses { get; set; } = new();

    // Hoạt động gần đây
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

public class RecentClassDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? SemesterName { get; set; }
    public int TotalStudents { get; set; }
    public int TotalGroups { get; set; }
    public int TotalProjects { get; set; }
    public int PendingProposals { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class RecentActivityDto
{
    public string? ActivityType { get; set; } // "Proposal", "Submission", "Grading", "Announcement"
    public string? Description { get; set; }
    public string? RelatedClass { get; set; }
    public string? RelatedGroup { get; set; }
    public DateTime? ActivityDate { get; set; }
}



