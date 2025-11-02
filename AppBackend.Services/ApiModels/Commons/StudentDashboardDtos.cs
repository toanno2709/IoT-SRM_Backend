namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Student Dashboard Response DTO
/// </summary>
public class StudentDashboardResponseDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public StudentStatisticsDto Statistics { get; set; } = new();
    public List<UpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new();
    public List<RecentGradeDto> RecentGrades { get; set; } = new();
    public List<RecentNotificationDto> RecentNotifications { get; set; } = new();
}

/// <summary>
/// Student Statistics DTO
/// </summary>
public class StudentStatisticsDto
{
    public int TotalClasses { get; set; }
    public int TotalGroups { get; set; }
    public int TotalProjects { get; set; }
    public decimal? AverageGrade { get; set; }
    public int TotalSubmissions { get; set; }
    public int PendingSubmissions { get; set; }
}

/// <summary>
/// Upcoming Deadline DTO
/// </summary>
public class UpcomingDeadlineDto
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public int MilestoneId { get; set; }
    public string MilestoneTitle { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public int DaysRemaining { get; set; }
    public string Status { get; set; } = string.Empty; // NotSubmitted, Submitted, Graded
    public decimal? Weight { get; set; }
}

/// <summary>
/// Recent Grade DTO
/// </summary>
public class RecentGradeDto
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public int MilestoneId { get; set; }
    public string MilestoneTitle { get; set; } = string.Empty;
    public decimal Grade { get; set; }
    public DateTime GradedAt { get; set; }
    public string? Feedback { get; set; }
}

/// <summary>
/// Recent Notification DTO
/// </summary>
public class RecentNotificationDto
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Student Class DTO
/// </summary>
public class StudentClassDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string? SemesterName { get; set; }
    public string? InstructorName { get; set; }
    public DateTime EnrolledAt { get; set; }
    public StudentClassGroupDto? MyGroup { get; set; }
}

/// <summary>
/// Student Class Group DTO
/// </summary>
public class StudentClassGroupDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public bool IsLeader { get; set; }
    public int MemberCount { get; set; }
}

/// <summary>
/// Student Group Detail DTO
/// </summary>
public class StudentGroupDetailDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Leader or Member
    public List<GroupMemberSimpleDto> Members { get; set; } = new();
    public StudentGroupProjectDto? Project { get; set; }
}

/// <summary>
/// Group Member Simple DTO
/// </summary>
public class GroupMemberSimpleDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// Student Group Project DTO
/// </summary>
public class StudentGroupProjectDto
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// Group Invitation DTO
/// </summary>
public class GroupInvitationDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string InvitedBy { get; set; } = string.Empty;
    public DateTime InvitedAt { get; set; }
    public int NotificationId { get; set; }
}

/// <summary>
/// Group Invitations Response DTO
/// </summary>
public class GroupInvitationsResponseDto
{
    public List<GroupInvitationDto> PendingInvitations { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Reject Group Invitation Request DTO
/// </summary>
public class GroupRejectInviteDto
{
    public int GroupId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Reject Invitation Response DTO
/// </summary>
public class RejectInvitationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime RejectedAt { get; set; }
}
