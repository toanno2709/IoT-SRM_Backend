namespace AppBackend.Services.ApiModels.Commons;

#region Request DTOs

/// <summary>
/// Request for exporting reports with filters
/// </summary>
public class ReportExportRequestDto
{
    public string ExportFormat { get; set; } = "Excel"; // "Excel" or "PDF"
    public int? SemesterId { get; set; }
    public int? ClassId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ReportType { get; set; } // "Classes", "Projects", "Milestones", "Grades", "Students", "Instructors"
}

#endregion

#region Response DTOs

/// <summary>
/// Classes summary report
/// </summary>
public class ClassesSummaryReportDto
{
    public int TotalClasses { get; set; }
    public int ActiveClasses { get; set; }
    public int ClassesWithoutInstructor { get; set; }
    public decimal AverageClassSize { get; set; }
    public List<ClassBySemesterDto> ClassesBySemester { get; set; } = new();
}

public class ClassBySemesterDto
{
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int ClassCount { get; set; }
    public int TotalStudents { get; set; }
    public int TotalGroups { get; set; }
    public int TotalProjects { get; set; }
}

/// <summary>
/// Instructors workload report
/// </summary>
public class InstructorsWorkloadReportDto
{
    public int TotalInstructors { get; set; }
    public decimal AverageClassesPerInstructor { get; set; }
    public int InstructorsWithNoClasses { get; set; }
    public List<InstructorWorkloadDto> InstructorWorkloads { get; set; } = new();
}

public class InstructorWorkloadDto
{
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public string? Email { get; set; }
    public int ClassCount { get; set; }
    public int TotalStudents { get; set; }
    public int TotalGroups { get; set; }
    public int PendingProposals { get; set; }
    public int SubmissionsToGrade { get; set; }
}

/// <summary>
/// Students distribution report
/// </summary>
public class StudentsDistributionReportDto
{
    public int TotalStudents { get; set; }
    public int StudentsInGroups { get; set; }
    public int StudentsWithoutGroups { get; set; }
    public decimal GroupParticipationRate { get; set; }
    public List<StudentsBySemesterDto> StudentsBySemester { get; set; } = new();
    public List<StudentsByClassDto> StudentsByClass { get; set; } = new();
}

public class StudentsBySemesterDto
{
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int StudentCount { get; set; }
    public int InGroups { get; set; }
    public int WithoutGroups { get; set; }
}

public class StudentsByClassDto
{
    public int ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? SemesterName { get; set; }
    public int StudentCount { get; set; }
    public int GroupCount { get; set; }
    public decimal AverageGroupSize { get; set; }
}

/// <summary>
/// Projects status report
/// </summary>
public class ProjectsStatusReportDto
{
    public int TotalProjects { get; set; }
    public int PendingProjects { get; set; }
    public int ApprovedProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int RejectedProjects { get; set; }
    public decimal CompletionRate { get; set; }
    public List<ProjectStatusDetailDto> ProjectsByStatus { get; set; } = new();
    public List<ProjectsBySemesterDto> ProjectsBySemester { get; set; } = new();
}

public class ProjectStatusDetailDto
{
    public string? Status { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class ProjectsBySemesterDto
{
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int TotalProjects { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Pending { get; set; }
}

/// <summary>
/// Milestone progress report
/// </summary>
public class MilestoneProgressReportDto
{
    public int TotalMilestones { get; set; }
    public int CompletedMilestones { get; set; }
    public int PendingMilestones { get; set; }
    public decimal OverallCompletionRate { get; set; }
    public decimal AverageGrade { get; set; }
    public List<MilestoneCompletionByTypeDto> CompletionByMilestone { get; set; } = new();
    public List<MilestoneCompletionBySemesterDto> CompletionBySemester { get; set; } = new();
}

public class MilestoneCompletionByTypeDto
{
    public string? MilestoneName { get; set; }
    public int TotalSubmissions { get; set; }
    public int GradedSubmissions { get; set; }
    public int PendingSubmissions { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageGrade { get; set; }
}

public class MilestoneCompletionBySemesterDto
{
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int TotalMilestones { get; set; }
    public int Completed { get; set; }
    public decimal CompletionRate { get; set; }
}

/// <summary>
/// Grades distribution report
/// </summary>
public class GradesDistributionReportDto
{
    public int TotalGradedProjects { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal HighestGrade { get; set; }
    public decimal LowestGrade { get; set; }
    public decimal MedianGrade { get; set; }
    public List<GradeRangeDto> GradeRanges { get; set; } = new();
    public List<GradesBySemesterDto> GradesBySemester { get; set; } = new();
    public List<TopPerformingProjectDto> TopProjects { get; set; } = new();
}

public class GradeRangeDto
{
    public string Range { get; set; } = string.Empty; // "90-100", "80-89", etc.
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class GradesBySemesterDto
{
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int GradedProjects { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal HighestGrade { get; set; }
    public decimal LowestGrade { get; set; }
}

public class TopPerformingProjectDto
{
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? GroupName { get; set; }
    public string? ClassName { get; set; }
    public decimal Grade { get; set; }
    public string? SemesterName { get; set; }
}

/// <summary>
/// Report export response
/// </summary>
public class ReportExportResponseDto
{
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? ExportFormat { get; set; }
}

#endregion
