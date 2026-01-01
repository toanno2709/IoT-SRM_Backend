using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Constants;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Repositories.Repositories.FinalProjectRepo;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace AppBackend.Services.Services.AdminReport;

public class AdminReportService : IAdminReportService
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IMilestoneEvaluationRepository _milestoneEvaluationRepository;
    private readonly IFinalProjectRepository _finalProjectRepository;
    private readonly IotShowroomContext _context;

    public AdminReportService(
        IClassRepository classRepository,
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        IProjectRepository projectRepository,
        IMilestoneEvaluationRepository milestoneEvaluationRepository,
        IFinalProjectRepository finalProjectRepository,
        IotShowroomContext context)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _projectRepository = projectRepository;
        _milestoneEvaluationRepository = milestoneEvaluationRepository;
        _finalProjectRepository = finalProjectRepository;
        _context = context;
    }

    public async Task<ResultModel<ClassesSummaryReportDto>> GetClassesSummaryAsync(int? semesterId = null)
    {
        try
        {
            var classes = await _classRepository.GetAllAsync();
            
            if (semesterId.HasValue)
            {
                classes = classes.Where(c => c.SemesterId == semesterId.Value).ToList();
            }

            var totalClasses = classes.Count();
            var activeClasses = classes.Count(c => c.Semester?.IsActive == true);
            var classesWithoutInstructor = classes.Count(c => c.InstructorId == null);

            var classesBySemester = classes
                .GroupBy(c => new { c.SemesterId, c.Semester?.Name })
                .Select(g => new ClassBySemesterDto
                {
                    SemesterId = g.Key.SemesterId ?? 0,
                    SemesterName = g.Key.Name,
                    ClassCount = g.Count(),
                    TotalStudents = g.Sum(c => c.ClassEnrollments?.Count ?? 0),
                    TotalGroups = g.Sum(c => c.Groups?.Count ?? 0),
                    TotalProjects = g.Sum(c => c.Groups?.Sum(gr => gr.Projects?.Count ?? 0) ?? 0)
                })
                .OrderByDescending(x => x.SemesterId)
                .ToList();

            var totalStudents = classes.Sum(c => c.ClassEnrollments?.Count ?? 0);
            var averageClassSize = totalClasses > 0 ? (decimal)totalStudents / totalClasses : 0;

            var report = new ClassesSummaryReportDto
            {
                TotalClasses = totalClasses,
                ActiveClasses = activeClasses,
                ClassesWithoutInstructor = classesWithoutInstructor,
                AverageClassSize = Math.Round(averageClassSize, 2),
                ClassesBySemester = classesBySemester
            };

            return new ResultModel<ClassesSummaryReportDto>
            {
                IsSuccess = true,
                Data = report,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassesSummaryReportDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error generating classes summary: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<InstructorsWorkloadReportDto>> GetInstructorsWorkloadAsync(int? semesterId = null)
    {
        try
        {
            var instructors = await _userRepository.GetByRoleAsync(2); // Role 2 = Instructor
            var classes = await _classRepository.GetAllAsync();

            if (semesterId.HasValue)
            {
                classes = classes.Where(c => c.SemesterId == semesterId.Value).ToList();
            }

            var totalInstructors = instructors.Count();
            var instructorsWithClasses = classes.Where(c => c.InstructorId.HasValue)
                .Select(c => c.InstructorId!.Value)
                .Distinct()
                .Count();
            var instructorsWithNoClasses = totalInstructors - instructorsWithClasses;

            var instructorWorkloads = instructors.Select(instructor =>
            {
                var instructorClasses = classes.Where(c => c.InstructorId == instructor.UserId).ToList();
                var totalStudents = instructorClasses.Sum(c => c.ClassEnrollments?.Count ?? 0);
                var totalGroups = instructorClasses.Sum(c => c.Groups?.Count ?? 0);

                return new InstructorWorkloadDto
                {
                    InstructorId = instructor.UserId,
                    InstructorName = instructor.FullName,
                    Email = instructor.Email,
                    ClassCount = instructorClasses.Count,
                    TotalStudents = totalStudents,
                    TotalGroups = totalGroups,
                    PendingProposals = 0, // TODO: Calculate from proposals
                    SubmissionsToGrade = 0 // TODO: Calculate from submissions
                };
            }).OrderByDescending(x => x.ClassCount).ToList();

            var averageClassesPerInstructor = totalInstructors > 0 
                ? (decimal)classes.Count(c => c.InstructorId.HasValue) / totalInstructors 
                : 0;

            var report = new InstructorsWorkloadReportDto
            {
                TotalInstructors = totalInstructors,
                AverageClassesPerInstructor = Math.Round(averageClassesPerInstructor, 2),
                InstructorsWithNoClasses = instructorsWithNoClasses,
                InstructorWorkloads = instructorWorkloads
            };

            return new ResultModel<InstructorsWorkloadReportDto>
            {
                IsSuccess = true,
                Data = report,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<InstructorsWorkloadReportDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error generating instructors workload: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<StudentsDistributionReportDto>> GetStudentsDistributionAsync(int? semesterId = null)
    {
        try
        {
            var students = await _userRepository.GetByRoleAsync(3); // Role 3 = Student
            var classes = await _classRepository.GetAllAsync();
            var groups = await _groupRepository.GetAllAsync();

            if (semesterId.HasValue)
            {
                classes = classes.Where(c => c.SemesterId == semesterId.Value).ToList();
                var classIds = classes.Select(c => c.ClassId).ToHashSet();
                groups = groups.Where(g => classIds.Contains(g.ClassId)).ToList();
            }

            var totalStudents = students.Count();
            var studentsInGroups = groups.Sum(g => g.GroupMembers?.Count ?? 0);
            var studentsWithoutGroups = totalStudents - studentsInGroups;
            var groupParticipationRate = totalStudents > 0 
                ? (decimal)studentsInGroups / totalStudents * 100 
                : 0;

            var studentsBySemester = classes
                .GroupBy(c => new { c.SemesterId, c.Semester?.Name })
                .Select(g => new StudentsBySemesterDto
                {
                    SemesterId = g.Key.SemesterId ?? 0,
                    SemesterName = g.Key.Name,
                    StudentCount = g.Sum(c => c.ClassEnrollments?.Count ?? 0),
                    InGroups = g.Sum(c => c.Groups?.Sum(gr => gr.GroupMembers?.Count ?? 0) ?? 0),
                    WithoutGroups = 0 // Will be calculated
                })
                .ToList();

            foreach (var item in studentsBySemester)
            {
                item.WithoutGroups = item.StudentCount - item.InGroups;
            }

            var studentsByClass = classes.Select(c => new StudentsByClassDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                SemesterName = c.Semester?.Name,
                StudentCount = c.ClassEnrollments?.Count ?? 0,
                GroupCount = c.Groups?.Count ?? 0,
                AverageGroupSize = c.Groups?.Count > 0 
                    ? Math.Round((decimal)(c.Groups.Sum(g => g.GroupMembers?.Count ?? 0)) / c.Groups.Count, 2)
                    : 0
            }).ToList();

            var report = new StudentsDistributionReportDto
            {
                TotalStudents = totalStudents,
                StudentsInGroups = studentsInGroups,
                StudentsWithoutGroups = studentsWithoutGroups,
                GroupParticipationRate = Math.Round(groupParticipationRate, 2),
                StudentsBySemester = studentsBySemester,
                StudentsByClass = studentsByClass
            };

            return new ResultModel<StudentsDistributionReportDto>
            {
                IsSuccess = true,
                Data = report,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<StudentsDistributionReportDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error generating students distribution: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<ProjectsStatusReportDto>> GetProjectsStatusAsync(int? semesterId = null)
    {
        try
        {
            var projects = await _projectRepository.GetAllAsync();

            if (semesterId.HasValue)
            {
                var classesForFilter = await _classRepository.GetAllAsync();
                var classIds = classesForFilter.Where(c => c.SemesterId == semesterId.Value)
                    .Select(c => c.ClassId)
                    .ToHashSet();
                
                projects = projects.Where(p => p.Group != null && classIds.Contains(p.Group.ClassId)).ToList();
            }

            var totalProjects = projects.Count();
            var pendingProjects = projects.Count(p => p.Status == "Pending");
            var approvedProjects = projects.Count(p => p.Status == "Approved");
            var completedProjects = projects.Count(p => p.Status == "Completed");
            var rejectedProjects = projects.Count(p => p.Status == "Rejected");

            var completionRate = totalProjects > 0 
                ? (decimal)completedProjects / totalProjects * 100 
                : 0;

            var projectsByStatus = new List<ProjectStatusDetailDto>
            {
                new() { Status = "Pending", Count = pendingProjects, Percentage = totalProjects > 0 ? Math.Round((decimal)pendingProjects / totalProjects * 100, 2) : 0 },
                new() { Status = "Approved", Count = approvedProjects, Percentage = totalProjects > 0 ? Math.Round((decimal)approvedProjects / totalProjects * 100, 2) : 0 },
                new() { Status = "Completed", Count = completedProjects, Percentage = totalProjects > 0 ? Math.Round((decimal)completedProjects / totalProjects * 100, 2) : 0 },
                new() { Status = "Rejected", Count = rejectedProjects, Percentage = totalProjects > 0 ? Math.Round((decimal)rejectedProjects / totalProjects * 100, 2) : 0 }
            };

            var classes = await _classRepository.GetAllAsync();
            var projectsBySemester = classes
                .GroupBy(c => new { c.SemesterId, c.Semester?.Name })
                .Select(g =>
                {
                    var semesterProjects = projects.Where(p => p.Group != null && 
                        g.Select(c => c.ClassId).Contains(p.Group.ClassId)).ToList();
                    
                    return new ProjectsBySemesterDto
                    {
                        SemesterId = g.Key.SemesterId ?? 0,
                        SemesterName = g.Key.Name,
                        TotalProjects = semesterProjects.Count,
                        Completed = semesterProjects.Count(p => p.Status == "Completed"),
                        InProgress = semesterProjects.Count(p => p.Status == "Approved"),
                        Pending = semesterProjects.Count(p => p.Status == "Pending")
                    };
                })
                .OrderByDescending(x => x.SemesterId)
                .ToList();

            var report = new ProjectsStatusReportDto
            {
                TotalProjects = totalProjects,
                PendingProjects = pendingProjects,
                ApprovedProjects = approvedProjects,
                CompletedProjects = completedProjects,
                RejectedProjects = rejectedProjects,
                CompletionRate = Math.Round(completionRate, 2),
                ProjectsByStatus = projectsByStatus,
                ProjectsBySemester = projectsBySemester
            };

            return new ResultModel<ProjectsStatusReportDto>
            {
                IsSuccess = true,
                Data = report,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ProjectsStatusReportDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error generating projects status: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<MilestoneProgressReportDto>> GetMilestoneProgressAsync(int? semesterId = null)
    {
        try
        {
            var evaluations = await _milestoneEvaluationRepository.GetAllAsync();

            if (semesterId.HasValue)
            {
                var classes = await _classRepository.GetAllAsync();
                var classIds = classes.Where(c => c.SemesterId == semesterId.Value)
                    .Select(c => c.ClassId)
                    .ToHashSet();
                
                evaluations = evaluations.Where(e => e.Project?.Group != null && 
                    classIds.Contains(e.Project.Group.ClassId)).ToList();
            }

            var totalMilestones = evaluations.Count();
            var completedMilestones = evaluations.Count(e => e.Score > 0);
            var pendingMilestones = totalMilestones - completedMilestones;
            var overallCompletionRate = totalMilestones > 0 
                ? (decimal)completedMilestones / totalMilestones * 100 
                : 0;
            var averageGrade = evaluations.Where(e => e.Score > 0).Any()
                ? evaluations.Where(e => e.Score > 0).Average(e => e.Score)
                : 0;

            var completionByMilestone = evaluations
                .GroupBy(e => e.MilestoneDef?.Title ?? "Unknown")
                .Select(g =>
                {
                    var graded = g.Count(e => e.Score > 0);
                    var total = g.Count();
                    
                    return new MilestoneCompletionByTypeDto
                    {
                        MilestoneName = g.Key,
                        TotalSubmissions = total,
                        GradedSubmissions = graded,
                        PendingSubmissions = total - graded,
                        CompletionRate = total > 0 ? Math.Round((decimal)graded / total * 100, 2) : 0,
                        AverageGrade = g.Where(e => e.Score > 0).Any() 
                            ? Math.Round(g.Where(e => e.Score > 0).Average(e => e.Score), 2)
                            : 0
                    };
                })
                .ToList();

            // TODO: Implement completion by semester when semester info is available in evaluations
            var completionBySemester = new List<MilestoneCompletionBySemesterDto>();

            var report = new MilestoneProgressReportDto
            {
                TotalMilestones = totalMilestones,
                CompletedMilestones = completedMilestones,
                PendingMilestones = pendingMilestones,
                OverallCompletionRate = Math.Round(overallCompletionRate, 2),
                AverageGrade = Math.Round(averageGrade, 2),
                CompletionByMilestone = completionByMilestone,
                CompletionBySemester = completionBySemester
            };

            return new ResultModel<MilestoneProgressReportDto>
            {
                IsSuccess = true,
                Data = report,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<MilestoneProgressReportDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error generating milestone progress: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<GradesDistributionReportDto>> GetGradesDistributionAsync(int? semesterId = null)
    {
        try
        {
            var finalSubmissions = await _finalProjectRepository.GetAllAsync();

            if (semesterId.HasValue)
            {
                var classes = await _classRepository.GetAllAsync();
                var classIds = classes.Where(c => c.SemesterId == semesterId.Value)
                    .Select(c => c.ClassId)
                    .ToHashSet();
                
                finalSubmissions = finalSubmissions.Where(f => f.Project?.Group != null && 
                    classIds.Contains(f.Project.Group.ClassId)).ToList();
            }

            var gradedProjects = finalSubmissions.Where(f => f.Grade.HasValue).ToList();
            var totalGradedProjects = gradedProjects.Count;

            if (totalGradedProjects == 0)
            {
                return new ResultModel<GradesDistributionReportDto>
                {
                    IsSuccess = true,
                    Data = new GradesDistributionReportDto
                    {
                        TotalGradedProjects = 0,
                        GradeRanges = new List<GradeRangeDto>(),
                        GradesBySemester = new List<GradesBySemesterDto>(),
                        TopProjects = new List<TopPerformingProjectDto>()
                    },
                    Message = "No graded projects found"
                };
            }

            var grades = gradedProjects.Select(f => f.Grade!.Value).ToList();
            var averageGrade = grades.Average();
            var highestGrade = grades.Max();
            var lowestGrade = grades.Min();
            var sortedGrades = grades.OrderBy(g => g).ToList();
            var medianGrade = sortedGrades.Count % 2 == 0
                ? (sortedGrades[sortedGrades.Count / 2 - 1] + sortedGrades[sortedGrades.Count / 2]) / 2
                : sortedGrades[sortedGrades.Count / 2];

            var gradeRanges = new List<GradeRangeDto>
            {
                new() { Range = "90-100", Count = grades.Count(g => g >= 90 && g <= 100), Percentage = 0 },
                new() { Range = "80-89", Count = grades.Count(g => g >= 80 && g < 90), Percentage = 0 },
                new() { Range = "70-79", Count = grades.Count(g => g >= 70 && g < 80), Percentage = 0 },
                new() { Range = "60-69", Count = grades.Count(g => g >= 60 && g < 70), Percentage = 0 },
                new() { Range = "50-59", Count = grades.Count(g => g >= 50 && g < 60), Percentage = 0 },
                new() { Range = "0-49", Count = grades.Count(g => g < 50), Percentage = 0 }
            };

            foreach (var range in gradeRanges)
            {
                range.Percentage = Math.Round((decimal)range.Count / totalGradedProjects * 100, 2);
            }

            // TODO: Implement grades by semester
            var gradesBySemester = new List<GradesBySemesterDto>();

            var topProjects = gradedProjects
                .OrderByDescending(f => f.Grade)
                .Take(10)
                .Select(f => new TopPerformingProjectDto
                {
                    ProjectId = f.ProjectId,
                    ProjectName = f.Project?.Title,
                    GroupName = f.Project?.Group?.GroupName,
                    ClassName = f.Project?.Group?.Class?.ClassName,
                    Grade = f.Grade!.Value,
                    SemesterName = f.Project?.Group?.Class?.Semester?.Name
                })
                .ToList();

            var report = new GradesDistributionReportDto
            {
                TotalGradedProjects = totalGradedProjects,
                AverageGrade = Math.Round(averageGrade, 2),
                HighestGrade = highestGrade,
                LowestGrade = lowestGrade,
                MedianGrade = Math.Round(medianGrade, 2),
                GradeRanges = gradeRanges,
                GradesBySemester = gradesBySemester,
                TopProjects = topProjects
            };

            return new ResultModel<GradesDistributionReportDto>
            {
                IsSuccess = true,
                Data = report,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<GradesDistributionReportDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error generating grades distribution: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<ReportExportResponseDto>> ExportReportAsync(ReportExportRequestDto request)
    {
        try
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            if (request.ExportFormat?.ToLower() == "pdf")
            {
                return new ResultModel<ReportExportResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 501,
                    Message = "PDF export not yet implemented. Please use Excel format."
                };
            }

            // Generate Excel file based on report type
            byte[] excelData;
            string fileName;

            switch (request.ReportType?.ToLower())
            {
                case "classes":
                    var classesReport = await GetClassesSummaryAsync(request.SemesterId);
                    if (!classesReport.IsSuccess || classesReport.Data == null)
                        return new ResultModel<ReportExportResponseDto>
                        {
                            IsSuccess = false,
                            StatusCode = 500,
                            Message = "Failed to generate classes report"
                        };
                    excelData = GenerateClassesExcel(classesReport.Data);
                    fileName = $"Classes_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    break;

                case "instructors":
                    var instructorsReport = await GetInstructorsWorkloadAsync(request.SemesterId);
                    if (!instructorsReport.IsSuccess || instructorsReport.Data == null)
                        return new ResultModel<ReportExportResponseDto>
                        {
                            IsSuccess = false,
                            StatusCode = 500,
                            Message = "Failed to generate instructors report"
                        };
                    excelData = GenerateInstructorsExcel(instructorsReport.Data);
                    fileName = $"Instructors_Workload_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    break;

                case "students":
                    var studentsReport = await GetStudentsDistributionAsync(request.SemesterId);
                    if (!studentsReport.IsSuccess || studentsReport.Data == null)
                        return new ResultModel<ReportExportResponseDto>
                        {
                            IsSuccess = false,
                            StatusCode = 500,
                            Message = "Failed to generate students report"
                        };
                    excelData = GenerateStudentsExcel(studentsReport.Data);
                    fileName = $"Students_Distribution_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    break;

                case "projects":
                    var projectsReport = await GetProjectsStatusAsync(request.SemesterId);
                    if (!projectsReport.IsSuccess || projectsReport.Data == null)
                        return new ResultModel<ReportExportResponseDto>
                        {
                            IsSuccess = false,
                            StatusCode = 500,
                            Message = "Failed to generate projects report"
                        };
                    excelData = GenerateProjectsExcel(projectsReport.Data);
                    fileName = $"Projects_Status_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    break;

                case "milestones":
                    var milestonesReport = await GetMilestoneProgressAsync(request.SemesterId);
                    if (!milestonesReport.IsSuccess || milestonesReport.Data == null)
                        return new ResultModel<ReportExportResponseDto>
                        {
                            IsSuccess = false,
                            StatusCode = 500,
                            Message = "Failed to generate milestones report"
                        };
                    excelData = GenerateMilestonesExcel(milestonesReport.Data);
                    fileName = $"Milestone_Progress_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    break;

                case "grades":
                    var gradesReport = await GetGradesDistributionAsync(request.SemesterId);
                    if (!gradesReport.IsSuccess || gradesReport.Data == null)
                        return new ResultModel<ReportExportResponseDto>
                        {
                            IsSuccess = false,
                            StatusCode = 500,
                            Message = "Failed to generate grades report"
                        };
                    excelData = GenerateGradesExcel(gradesReport.Data);
                    fileName = $"Grades_Distribution_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    break;

                default:
                    return new ResultModel<ReportExportResponseDto>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Message = "Invalid report type. Valid types: Classes, Instructors, Students, Projects, Milestones, Grades"
                    };
            }

            // Convert to base64 for transmission
            var base64Data = Convert.ToBase64String(excelData);

            return new ResultModel<ReportExportResponseDto>
            {
                IsSuccess = true,
                Data = new ReportExportResponseDto
                {
                    FileName = fileName,
                    FileUrl = $"data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,{base64Data}",
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileSizeBytes = excelData.Length,
                    GeneratedAt = DateTime.UtcNow,
                    ExportFormat = "Excel"
                },
                Message = "Report exported successfully"
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ReportExportResponseDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error exporting report: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<ReportExportResponseDto>> ExportComprehensiveSemesterReportAsync(int semesterId)
    {
        try
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Get semester info
            var semester = await _context.Semesters
                .FirstOrDefaultAsync(s => s.SemesterId == semesterId);

            if (semester == null)
            {
                return new ResultModel<ReportExportResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Message = "Semester not found"
                };
            }

            // Generate comprehensive Excel file
            var excelData = await GenerateComprehensiveSemesterExcel(semesterId, semester);
            var fileName = $"Semester_Report_{semester.Code}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            // Convert to base64 for transmission
            var base64Data = Convert.ToBase64String(excelData);

            return new ResultModel<ReportExportResponseDto>
            {
                IsSuccess = true,
                Data = new ReportExportResponseDto
                {
                    FileName = fileName,
                    FileUrl = $"data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,{base64Data}",
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileSizeBytes = excelData.Length,
                    GeneratedAt = DateTime.UtcNow,
                    ExportFormat = "Excel"
                },
                Message = "Comprehensive semester report exported successfully"
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ReportExportResponseDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error exporting comprehensive semester report: {ex.Message}"
            };
        }
    }

    private async Task<byte[]> GenerateComprehensiveSemesterExcel(int semesterId, AppBackend.BusinessObjects.Models.Semester semester)
    {
        using var package = new ExcelPackage();

        // Sheet 1: T?ng quan k? h?c (Semester Overview)
        await CreateSemesterOverviewSheet(package, semesterId, semester);

        // Sheet 2: Danh sách l?p h?c (Classes)
        await CreateClassesSheet(package, semesterId);

        // Sheet 3: Danh sách giáo viên (Instructors)
        await CreateInstructorsSheet(package, semesterId);

        // Sheet 4: Danh sách sinh viên (Students)
        await CreateStudentsSheet(package, semesterId);

        // Sheet 5: ?i?m theo Milestone (Milestone Grades)
        await CreateMilestoneGradesSheet(package, semesterId);

        // Sheet 6: ?i?m cu?i k? (Final Grades)
        await CreateFinalGradesSheet(package, semesterId);

        // Sheet 7: Tr?ng thái Pass/Not Pass
        await CreatePassStatusSheet(package, semesterId);

        return package.GetAsByteArray();
    }

    private async Task CreateSemesterOverviewSheet(ExcelPackage package, int semesterId, AppBackend.BusinessObjects.Models.Semester semester)
    {
        var worksheet = package.Workbook.Worksheets.Add("T?ng Quan K? H?c");

        // Header
        worksheet.Cells["A1:F1"].Merge = true;
        worksheet.Cells["A1"].Value = $"BÁO CÁO T?NG QUAN K? H?C: {semester.Name}";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

        int row = 3;

        // Semester info
        worksheet.Cells[$"A{row}"].Value = "Mã k? h?c:";
        worksheet.Cells[$"B{row}"].Value = semester.Code;
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[$"A{row}"].Value = "Tên k? h?c:";
        worksheet.Cells[$"B{row}"].Value = semester.Name;
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[$"A{row}"].Value = "N?m h?c:";
        worksheet.Cells[$"B{row}"].Value = semester.Year;
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[$"A{row}"].Value = "H?c k?:";
        worksheet.Cells[$"B{row}"].Value = semester.Term;
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[$"A{row}"].Value = "Ngày b?t ??u:";
        worksheet.Cells[$"B{row}"].Value = semester.StartDate?.ToString("dd/MM/yyyy");
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[$"A{row}"].Value = "Ngày k?t thúc:";
        worksheet.Cells[$"B{row}"].Value = semester.EndDate?.ToString("dd/MM/yyyy");
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[$"A{row}"].Value = "Tr?ng thái:";
        worksheet.Cells[$"B{row}"].Value = semester.IsActive == true ? "?ang ho?t ??ng" : "Không ho?t ??ng";
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row += 2;

        // Statistics
        var classes = await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.ClassEnrollments)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
            .Where(c => c.SemesterId == semesterId)
            .ToListAsync();

        var totalClasses = classes.Count;
        var totalStudents = classes.Sum(c => c.ClassEnrollments.Count);
        var totalGroups = classes.Sum(c => c.Groups.Count);
        var totalProjects = classes.Sum(c => c.Groups.Sum(g => g.Projects.Count));

        worksheet.Cells[$"A{row}"].Value = "TH?NG KÊ T?NG QUAN";
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"A{row}"].Style.Font.Size = 14;
        row++;

        worksheet.Cells[$"A{row}"].Value = "T?ng s? l?p:";
        worksheet.Cells[$"B{row}"].Value = totalClasses;
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[$"A{row}"].Value = "T?ng s? sinh viên:";
        worksheet.Cells[$"B{row}"].Value = totalStudents;
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[$"A{row}"].Value = "T?ng s? nhóm:";
        worksheet.Cells[$"B{row}"].Value = totalGroups;
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[$"A{row}"].Value = "T?ng s? d? án:";
        worksheet.Cells[$"B{row}"].Value = totalProjects;
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
    }

    private async Task CreateClassesSheet(ExcelPackage package, int semesterId)
    {
        var worksheet = package.Workbook.Worksheets.Add("Danh Sách L?p");

        // Header
        worksheet.Cells["A1:H1"].Merge = true;
        worksheet.Cells["A1"].Value = "DANH SÁCH CÁC L?P H?C";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);

        // Column headers
        int row = 3;
        worksheet.Cells[$"A{row}"].Value = "ID L?p";
        worksheet.Cells[$"B{row}"].Value = "Tên L?p";
        worksheet.Cells[$"C{row}"].Value = "Gi?ng Viên";
        worksheet.Cells[$"D{row}"].Value = "Email GV";
        worksheet.Cells[$"E{row}"].Value = "S? Sinh Viên";
        worksheet.Cells[$"F{row}"].Value = "S? Nhóm";
        worksheet.Cells[$"G{row}"].Value = "S? D? Án";
        worksheet.Cells[$"H{row}"].Value = "Tr?ng Thái";

        worksheet.Cells[$"A{row}:H{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"A{row}:H{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[$"A{row}:H{row}"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        worksheet.Cells[$"A{row}:H{row}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

        row++;

        var classes = await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.ClassEnrollments)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
            .Where(c => c.SemesterId == semesterId)
            .OrderBy(c => c.ClassName)
            .ToListAsync();

        foreach (var cls in classes)
        {
            worksheet.Cells[$"A{row}"].Value = cls.ClassId;
            worksheet.Cells[$"B{row}"].Value = cls.ClassName;
            worksheet.Cells[$"C{row}"].Value = cls.Instructor?.FullName ?? "Ch?a có";
            worksheet.Cells[$"D{row}"].Value = cls.Instructor?.Email ?? "";
            worksheet.Cells[$"E{row}"].Value = cls.ClassEnrollments.Count;
            worksheet.Cells[$"F{row}"].Value = cls.Groups.Count;
            worksheet.Cells[$"G{row}"].Value = cls.Groups.Sum(g => g.Projects.Count);
            worksheet.Cells[$"H{row}"].Value = cls.Status;
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
    }

    private async Task CreateInstructorsSheet(ExcelPackage package, int semesterId)
    {
        var worksheet = package.Workbook.Worksheets.Add("Danh Sách Gi?ng Viên");

        // Header
        worksheet.Cells["A1:F1"].Merge = true;
        worksheet.Cells["A1"].Value = "DANH SÁCH GI?NG VIÊN";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);

        // Column headers
        int row = 3;
        worksheet.Cells[$"A{row}"].Value = "ID GV";
        worksheet.Cells[$"B{row}"].Value = "H? Tên";
        worksheet.Cells[$"C{row}"].Value = "Email";
        worksheet.Cells[$"D{row}"].Value = "Các L?p Ph? Trách";
        worksheet.Cells[$"E{row}"].Value = "S? Sinh Viên";
        worksheet.Cells[$"F{row}"].Value = "S? D? Án";

        worksheet.Cells[$"A{row}:F{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"A{row}:F{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[$"A{row}:F{row}"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        row++;

        var instructors = await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.ClassEnrollments)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
            .Where(c => c.SemesterId == semesterId && c.InstructorId != null)
            .GroupBy(c => new { c.InstructorId, c.Instructor!.FullName, c.Instructor.Email })
            .Select(g => new
            {
                InstructorId = g.Key.InstructorId,
                InstructorName = g.Key.FullName,
                Email = g.Key.Email,
                Classes = string.Join(", ", g.Select(c => c.ClassName)),
                TotalStudents = g.Sum(c => c.ClassEnrollments.Count),
                TotalProjects = g.Sum(c => c.Groups.Sum(gr => gr.Projects.Count))
            })
            .ToListAsync();

        foreach (var instructor in instructors)
        {
            worksheet.Cells[$"A{row}"].Value = instructor.InstructorId;
            worksheet.Cells[$"B{row}"].Value = instructor.InstructorName;
            worksheet.Cells[$"C{row}"].Value = instructor.Email;
            worksheet.Cells[$"D{row}"].Value = instructor.Classes;
            worksheet.Cells[$"E{row}"].Value = instructor.TotalStudents;
            worksheet.Cells[$"F{row}"].Value = instructor.TotalProjects;
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
    }

    private async Task CreateStudentsSheet(ExcelPackage package, int semesterId)
    {
        var worksheet = package.Workbook.Worksheets.Add("Danh Sách Sinh Viên");

        // Header
        worksheet.Cells["A1:G1"].Merge = true;
        worksheet.Cells["A1"].Value = "DANH SÁCH SINH VIÊN";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);

        // Column headers
        int row = 3;
        worksheet.Cells[$"A{row}"].Value = "ID SV";
        worksheet.Cells[$"B{row}"].Value = "H? Tên";
        worksheet.Cells[$"C{row}"].Value = "Email";
        worksheet.Cells[$"D{row}"].Value = "L?p";
        worksheet.Cells[$"E{row}"].Value = "Nhóm";
        worksheet.Cells[$"F{row}"].Value = "D? Án";
        worksheet.Cells[$"G{row}"].Value = "Vai Trò";

        worksheet.Cells[$"A{row}:G{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"A{row}:G{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[$"A{row}:G{row}"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        row++;

        var students = await _context.ClassEnrollments
            .Include(ce => ce.Student)
            .Include(ce => ce.Class)
            .Where(ce => ce.Class!.SemesterId == semesterId)
            .OrderBy(ce => ce.Class!.ClassName)
            .ThenBy(ce => ce.Student!.FullName)
            .ToListAsync();

        foreach (var enrollment in students)
        {
            var student = enrollment.Student;
            var classEntity = enrollment.Class;

            // Tìm nhóm và d? án c?a sinh viên
            var groupMember = await _context.GroupMembers
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.Projects)
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.Leader)
                .FirstOrDefaultAsync(gm => gm.UserId == student!.UserId && 
                                          gm.Group!.ClassId == classEntity!.ClassId);

            worksheet.Cells[$"A{row}"].Value = student!.UserId;
            worksheet.Cells[$"B{row}"].Value = student.FullName;
            worksheet.Cells[$"C{row}"].Value = student.Email;
            worksheet.Cells[$"D{row}"].Value = classEntity!.ClassName;
            worksheet.Cells[$"E{row}"].Value = groupMember?.Group?.GroupName ?? "Ch?a có nhóm";
            worksheet.Cells[$"F{row}"].Value = groupMember?.Group?.Projects.FirstOrDefault()?.Title ?? "Ch?a có d? án";
            
            string role = "Thành viên";
            if (groupMember != null && groupMember.Group?.LeaderId == student.UserId)
            {
                role = "Nhóm tr??ng";
            }
            worksheet.Cells[$"G{row}"].Value = role;
            
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
    }

    private async Task CreateMilestoneGradesSheet(ExcelPackage package, int semesterId)
    {
        var worksheet = package.Workbook.Worksheets.Add("?i?m Milestone");

        // Header
        worksheet.Cells["A1:J1"].Merge = true;
        worksheet.Cells["A1"].Value = "?I?M ?ÁNH GIÁ THEO MILESTONE";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);

        // Column headers
        int row = 3;
        worksheet.Cells[$"A{row}"].Value = "L?p";
        worksheet.Cells[$"B{row}"].Value = "Nhóm";
        worksheet.Cells[$"C{row}"].Value = "D? Án";
        worksheet.Cells[$"D{row}"].Value = "Sinh Viên";
        worksheet.Cells[$"E{row}"].Value = "Email SV";
        worksheet.Cells[$"F{row}"].Value = "Milestone";
        worksheet.Cells[$"G{row}"].Value = "Tr?ng S? (%)";
        worksheet.Cells[$"H{row}"].Value = "?i?m";
        worksheet.Cells[$"I{row}"].Value = "Gi?ng Viên Ch?m";
        worksheet.Cells[$"J{row}"].Value = "Ngày Ch?m";

        worksheet.Cells[$"A{row}:J{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"A{row}:J{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[$"A{row}:J{row}"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        row++;

        var milestoneGrades = await _context.MilestoneEvaluations
            .Include(me => me.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g.Class)
            .Include(me => me.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g.GroupMembers)
                        .ThenInclude(gm => gm.User)
            .Include(me => me.MilestoneDef)
            .Include(me => me.Instructor)
            .Where(me => me.Project.Group!.Class!.SemesterId == semesterId)
            .OrderBy(me => me.Project.Group!.Class!.ClassName)
            .ThenBy(me => me.Project.Group!.GroupName)
            .ThenBy(me => me.MilestoneDef.Title)
            .ToListAsync();

        foreach (var grade in milestoneGrades)
        {
            var project = grade.Project;
            var group = project.Group;
            var classEntity = group!.Class;

            // M?i milestone grade s? ???c hi?n th? cho t?ng thành viên trong nhóm
            foreach (var member in group.GroupMembers)
            {
                worksheet.Cells[$"A{row}"].Value = classEntity!.ClassName;
                worksheet.Cells[$"B{row}"].Value = group.GroupName;
                worksheet.Cells[$"C{row}"].Value = project.Title;
                worksheet.Cells[$"D{row}"].Value = member.User?.FullName;
                worksheet.Cells[$"E{row}"].Value = member.User?.Email;
                worksheet.Cells[$"F{row}"].Value = grade.MilestoneDef.Title;
                worksheet.Cells[$"G{row}"].Value = grade.WeightRatioSnapshot;
                worksheet.Cells[$"H{row}"].Value = grade.Score;
                worksheet.Cells[$"I{row}"].Value = grade.Instructor.FullName;
                worksheet.Cells[$"J{row}"].Value = grade.EvaluatedAt.ToString("dd/MM/yyyy HH:mm");
                row++;
            }
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
    }

    private async Task CreateFinalGradesSheet(ExcelPackage package, int semesterId)
    {
        var worksheet = package.Workbook.Worksheets.Add("?i?m Cu?i K?");

        // Header
        worksheet.Cells["A1:L1"].Merge = true;
        worksheet.Cells["A1"].Value = "?I?M CU?I K? (FINAL PROJECT)";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightSeaGreen);

        // Column headers
        int row = 3;
        worksheet.Cells[$"A{row}"].Value = "L?p";
        worksheet.Cells[$"B{row}"].Value = "Nhóm";
        worksheet.Cells[$"C{row}"].Value = "D? Án";
        worksheet.Cells[$"D{row}"].Value = "Sinh Viên";
        worksheet.Cells[$"E{row}"].Value = "Email SV";
        worksheet.Cells[$"F{row}"].Value = "Grader 1";
        worksheet.Cells[$"G{row}"].Value = "?i?m Grader 1";
        worksheet.Cells[$"H{row}"].Value = "Grader 2";
        worksheet.Cells[$"I{row}"].Value = "?i?m Grader 2";
        worksheet.Cells[$"J{row}"].Value = "?i?m Trung Bình";
        worksheet.Cells[$"K{row}"].Value = "Ngày N?p";
        worksheet.Cells[$"L{row}"].Value = "Tr?ng Thái";

        worksheet.Cells[$"A{row}:L{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"A{row}:L{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[$"A{row}:L{row}"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        row++;

        var finalSubmissions = await _context.FinalProjectSubmissions
            .Include(fs => fs.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g.Class)
            .Include(fs => fs.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g.GroupMembers)
                        .ThenInclude(gm => gm.User)
            .Include(fs => fs.FinalSubmissionGrades)
                .ThenInclude(fsg => fsg.Instructor)
            .Where(fs => fs.Project.Group!.Class!.SemesterId == semesterId)
            .OrderBy(fs => fs.Project.Group!.Class!.ClassName)
            .ThenBy(fs => fs.Project.Group!.GroupName)
            .ToListAsync();

        foreach (var submission in finalSubmissions)
        {
            var project = submission.Project;
            var group = project.Group;
            var classEntity = group!.Class;

            var grader1 = submission.FinalSubmissionGrades.FirstOrDefault();
            var grader2 = submission.FinalSubmissionGrades.Skip(1).FirstOrDefault();

            // M?i final submission hi?n th? cho t?ng thành viên trong nhóm
            foreach (var member in group.GroupMembers)
            {
                worksheet.Cells[$"A{row}"].Value = classEntity!.ClassName;
                worksheet.Cells[$"B{row}"].Value = group.GroupName;
                worksheet.Cells[$"C{row}"].Value = project.Title;
                worksheet.Cells[$"D{row}"].Value = member.User?.FullName;
                worksheet.Cells[$"E{row}"].Value = member.User?.Email;
                worksheet.Cells[$"F{row}"].Value = grader1?.Instructor?.FullName ?? "";
                worksheet.Cells[$"G{row}"].Value = grader1?.Grade.ToString() ?? "";
                worksheet.Cells[$"H{row}"].Value = grader2?.Instructor?.FullName ?? "";
                worksheet.Cells[$"I{row}"].Value = grader2?.Grade.ToString() ?? "";
                worksheet.Cells[$"J{row}"].Value = submission.Grade?.ToString() ?? "";
                worksheet.Cells[$"K{row}"].Value = submission.SubmittedAt.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cells[$"L{row}"].Value = submission.Status;
                row++;
            }
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
    }

    private async Task CreatePassStatusSheet(ExcelPackage package, int semesterId)
    {
        var worksheet = package.Workbook.Worksheets.Add("Tr?ng Thái Pass-Not Pass");

        // Header
        worksheet.Cells["A1:K1"].Merge = true;
        worksheet.Cells["A1"].Value = "TR?NG THÁI PASS / NOT PASS";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightGoldenrodYellow);

        // Column headers
        int row = 3;
        worksheet.Cells[$"A{row}"].Value = "L?p";
        worksheet.Cells[$"B{row}"].Value = "Sinh Viên";
        worksheet.Cells[$"C{row}"].Value = "Email";
        worksheet.Cells[$"D{row}"].Value = "Nhóm";
        worksheet.Cells[$"E{row}"].Value = "D? Án";
        worksheet.Cells[$"F{row}"].Value = "T?ng ?i?m Milestone";
        worksheet.Cells[$"G{row}"].Value = "?i?m Final";
        worksheet.Cells[$"H{row}"].Value = "?i?m T?ng K?t";
        worksheet.Cells[$"I{row}"].Value = "Tr?ng Thái D? Án";
        worksheet.Cells[$"J{row}"].Value = "?ã N?p Final";
        worksheet.Cells[$"K{row}"].Value = "K?t Qu?";

        worksheet.Cells[$"A{row}:K{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"A{row}:K{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[$"A{row}:K{row}"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        row++;

        var students = await _context.ClassEnrollments
            .Include(ce => ce.Student)
            .Include(ce => ce.Class)
            .Where(ce => ce.Class!.SemesterId == semesterId)
            .OrderBy(ce => ce.Class!.ClassName)
            .ThenBy(ce => ce.Student!.FullName)
            .ToListAsync();

        foreach (var enrollment in students)
        {
            var student = enrollment.Student;
            var classEntity = enrollment.Class;

            // Tìm nhóm và d? án
            var groupMember = await _context.GroupMembers
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.Projects)
                .FirstOrDefaultAsync(gm => gm.UserId == student!.UserId && 
                                          gm.Group!.ClassId == classEntity!.ClassId);

            var project = groupMember?.Group?.Projects.FirstOrDefault();

            decimal? totalMilestoneScore = null;
            decimal? finalScore = null;
            decimal? totalScore = null;
            string hasSubmittedFinal = "Ch?a n?p";
            string result = "NOT PASS";

            if (project != null)
            {
                // Tính t?ng ?i?m milestone (weighted average)
                var milestoneGrades = await _context.MilestoneEvaluations
                    .Where(me => me.ProjectId == project.ProjectId)
                    .ToListAsync();

                if (milestoneGrades.Any())
                {
                    totalMilestoneScore = milestoneGrades.Sum(me => me.Score * me.WeightRatioSnapshot / 100);
                }

                // L?y ?i?m final
                var finalSubmission = await _context.FinalProjectSubmissions
                    .FirstOrDefaultAsync(fs => fs.ProjectId == project.ProjectId);

                if (finalSubmission != null)
                {
                    hasSubmittedFinal = "?ã n?p";
                    finalScore = finalSubmission.Grade;

                    // Tính ?i?m t?ng k?t (gi? s? milestone 40%, final 60%)
                    if (totalMilestoneScore.HasValue && finalScore.HasValue)
                    {
                        totalScore = totalMilestoneScore.Value * 0.4m + finalScore.Value * 0.6m;

                        // ?i?u ki?n pass: ?i?m t?ng >= 50 và project status = Completed
                        if (totalScore >= 50 && project.Status == "Completed")
                        {
                            result = "PASS";
                        }
                    }
                }
            }

            worksheet.Cells[$"A{row}"].Value = classEntity!.ClassName;
            worksheet.Cells[$"B{row}"].Value = student!.FullName;
            worksheet.Cells[$"C{row}"].Value = student.Email;
            worksheet.Cells[$"D{row}"].Value = groupMember?.Group?.GroupName ?? "Ch?a có nhóm";
            worksheet.Cells[$"E{row}"].Value = project?.Title ?? "Ch?a có d? án";
            worksheet.Cells[$"F{row}"].Value = totalMilestoneScore?.ToString("F2") ?? "";
            worksheet.Cells[$"G{row}"].Value = finalScore?.ToString("F2") ?? "";
            worksheet.Cells[$"H{row}"].Value = totalScore?.ToString("F2") ?? "";
            worksheet.Cells[$"I{row}"].Value = project?.Status ?? "";
            worksheet.Cells[$"J{row}"].Value = hasSubmittedFinal;
            worksheet.Cells[$"K{row}"].Value = result;

            // Màu s?c cho k?t qu?
            if (result == "PASS")
            {
                worksheet.Cells[$"K{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[$"K{row}"].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                worksheet.Cells[$"K{row}"].Style.Font.Bold = true;
            }
            else if (result == "NOT PASS" && project != null)
            {
                worksheet.Cells[$"K{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[$"K{row}"].Style.Fill.BackgroundColor.SetColor(Color.LightPink);
                worksheet.Cells[$"K{row}"].Style.Font.Bold = true;
            }

            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
    }

    #region Excel Generation Methods for Old Export API

    private byte[] GenerateClassesExcel(ClassesSummaryReportDto report)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Classes Summary");

        // Header
        worksheet.Cells["A1:D1"].Merge = true;
        worksheet.Cells["A1"].Value = "CLASSES SUMMARY REPORT";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Summary statistics
        worksheet.Cells["A3"].Value = "Total Classes:";
        worksheet.Cells["B3"].Value = report.TotalClasses;
        worksheet.Cells["A4"].Value = "Active Classes:";
        worksheet.Cells["B4"].Value = report.ActiveClasses;
        worksheet.Cells["A5"].Value = "Classes Without Instructor:";
        worksheet.Cells["B5"].Value = report.ClassesWithoutInstructor;
        worksheet.Cells["A6"].Value = "Average Class Size:";
        worksheet.Cells["B6"].Value = report.AverageClassSize;

        worksheet.Cells["A3:A6"].Style.Font.Bold = true;

        // Classes by semester table
        worksheet.Cells["A8"].Value = "Classes by Semester";
        worksheet.Cells["A8"].Style.Font.Bold = true;
        worksheet.Cells["A8"].Style.Font.Size = 14;

        worksheet.Cells["A10"].Value = "Semester";
        worksheet.Cells["B10"].Value = "Class Count";
        worksheet.Cells["C10"].Value = "Total Students";
        worksheet.Cells["D10"].Value = "Total Groups";
        worksheet.Cells["E10"].Value = "Total Projects";
        worksheet.Cells["A10:E10"].Style.Font.Bold = true;
        worksheet.Cells["A10:E10"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A10:E10"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        int row = 11;
        foreach (var item in report.ClassesBySemester)
        {
            worksheet.Cells[row, 1].Value = item.SemesterName;
            worksheet.Cells[row, 2].Value = item.ClassCount;
            worksheet.Cells[row, 3].Value = item.TotalStudents;
            worksheet.Cells[row, 4].Value = item.TotalGroups;
            worksheet.Cells[row, 5].Value = item.TotalProjects;
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    private byte[] GenerateInstructorsExcel(InstructorsWorkloadReportDto report)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Instructors Workload");

        // Header
        worksheet.Cells["A1:G1"].Merge = true;
        worksheet.Cells["A1"].Value = "INSTRUCTORS WORKLOAD REPORT";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Summary
        worksheet.Cells["A3"].Value = "Total Instructors:";
        worksheet.Cells["B3"].Value = report.TotalInstructors;
        worksheet.Cells["A4"].Value = "Average Classes Per Instructor:";
        worksheet.Cells["B4"].Value = report.AverageClassesPerInstructor;
        worksheet.Cells["A5"].Value = "Instructors Without Classes:";
        worksheet.Cells["B5"].Value = report.InstructorsWithNoClasses;
        worksheet.Cells["A3:A5"].Style.Font.Bold = true;

        // Workload table
        worksheet.Cells["A7"].Value = "Instructor Workload Details";
        worksheet.Cells["A7"].Style.Font.Bold = true;
        worksheet.Cells["A7"].Style.Font.Size = 14;

        worksheet.Cells["A9"].Value = "Instructor Name";
        worksheet.Cells["B9"].Value = "Email";
        worksheet.Cells["C9"].Value = "Classes";
        worksheet.Cells["D9"].Value = "Students";
        worksheet.Cells["E9"].Value = "Groups";
        worksheet.Cells["F9"].Value = "Pending Proposals";
        worksheet.Cells["G9"].Value = "Submissions to Grade";
        worksheet.Cells["A9:G9"].Style.Font.Bold = true;
        worksheet.Cells["A9:G9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A9:G9"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        int row = 10;
        foreach (var item in report.InstructorWorkloads)
        {
            worksheet.Cells[row, 1].Value = item.InstructorName;
            worksheet.Cells[row, 2].Value = item.Email;
            worksheet.Cells[row, 3].Value = item.ClassCount;
            worksheet.Cells[row, 4].Value = item.TotalStudents;
            worksheet.Cells[row, 5].Value = item.TotalGroups;
            worksheet.Cells[row, 6].Value = item.PendingProposals;
            worksheet.Cells[row, 7].Value = item.SubmissionsToGrade;
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    private byte[] GenerateStudentsExcel(StudentsDistributionReportDto report)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Students Distribution");

        // Header
        worksheet.Cells["A1"].Value = "STUDENTS DISTRIBUTION REPORT";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;

        // Summary
        worksheet.Cells["A3"].Value = "Total Students:";
        worksheet.Cells["B3"].Value = report.TotalStudents;
        worksheet.Cells["A4"].Value = "Students in Groups:";
        worksheet.Cells["B4"].Value = report.StudentsInGroups;
        worksheet.Cells["A5"].Value = "Students Without Groups:";
        worksheet.Cells["B5"].Value = report.StudentsWithoutGroups;
        worksheet.Cells["A6"].Value = "Group Participation Rate:";
        worksheet.Cells["B6"].Value = $"{report.GroupParticipationRate}%";
        worksheet.Cells["A3:A6"].Style.Font.Bold = true;

        // Students by class
        worksheet.Cells["A8"].Value = "Students by Class";
        worksheet.Cells["A8"].Style.Font.Bold = true;
        worksheet.Cells["A8"].Style.Font.Size = 14;

        worksheet.Cells["A10"].Value = "Class Name";
        worksheet.Cells["B10"].Value = "Semester";
        worksheet.Cells["C10"].Value = "Student Count";
        worksheet.Cells["D10"].Value = "Group Count";
        worksheet.Cells["E10"].Value = "Avg Group Size";
        worksheet.Cells["A10:E10"].Style.Font.Bold = true;
        worksheet.Cells["A10:E10"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A10:E10"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        int row = 11;
        foreach (var item in report.StudentsByClass)
        {
            worksheet.Cells[row, 1].Value = item.ClassName;
            worksheet.Cells[row, 2].Value = item.SemesterName;
            worksheet.Cells[row, 3].Value = item.StudentCount;
            worksheet.Cells[row, 4].Value = item.GroupCount;
            worksheet.Cells[row, 5].Value = item.AverageGroupSize;
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    private byte[] GenerateProjectsExcel(ProjectsStatusReportDto report)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Projects Status");

        // Header
        worksheet.Cells["A1"].Value = "PROJECTS STATUS REPORT";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;

        // Summary
        worksheet.Cells["A3"].Value = "Total Projects:";
        worksheet.Cells["B3"].Value = report.TotalProjects;
        worksheet.Cells["A4"].Value = "Pending:";
        worksheet.Cells["B4"].Value = report.PendingProjects;
        worksheet.Cells["A5"].Value = "Approved:";
        worksheet.Cells["B5"].Value = report.ApprovedProjects;
        worksheet.Cells["A6"].Value = "Completed:";
        worksheet.Cells["B6"].Value = report.CompletedProjects;
        worksheet.Cells["A7"].Value = "Rejected:";
        worksheet.Cells["B7"].Value = report.RejectedProjects;
        worksheet.Cells["A8"].Value = "Completion Rate:";
        worksheet.Cells["B8"].Value = $"{report.CompletionRate}%";
        worksheet.Cells["A3:A8"].Style.Font.Bold = true;

        // Status breakdown
        worksheet.Cells["A10"].Value = "Status Distribution";
        worksheet.Cells["A10"].Style.Font.Bold = true;
        worksheet.Cells["A10"].Style.Font.Size = 14;

        worksheet.Cells["A12"].Value = "Status";
        worksheet.Cells["B12"].Value = "Count";
        worksheet.Cells["C12"].Value = "Percentage";
        worksheet.Cells["A12:C12"].Style.Font.Bold = true;
        worksheet.Cells["A12:C12"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A12:C12"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        int row = 13;
        foreach (var item in report.ProjectsByStatus)
        {
            worksheet.Cells[row, 1].Value = item.Status;
            worksheet.Cells[row, 2].Value = item.Count;
            worksheet.Cells[row, 3].Value = $"{item.Percentage}%";
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    private byte[] GenerateMilestonesExcel(MilestoneProgressReportDto report)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Milestone Progress");

        // Header
        worksheet.Cells["A1"].Value = "MILESTONE PROGRESS REPORT";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;

        // Summary
        worksheet.Cells["A3"].Value = "Total Milestones:";
        worksheet.Cells["B3"].Value = report.TotalMilestones;
        worksheet.Cells["A4"].Value = "Completed:";
        worksheet.Cells["B4"].Value = report.CompletedMilestones;
        worksheet.Cells["A5"].Value = "Pending:";
        worksheet.Cells["B5"].Value = report.PendingMilestones;
        worksheet.Cells["A6"].Value = "Completion Rate:";
        worksheet.Cells["B6"].Value = $"{report.OverallCompletionRate}%";
        worksheet.Cells["A7"].Value = "Average Grade:";
        worksheet.Cells["B7"].Value = report.AverageGrade;
        worksheet.Cells["A3:A7"].Style.Font.Bold = true;

        // Completion by milestone
        worksheet.Cells["A9"].Value = "Completion by Milestone Type";
        worksheet.Cells["A9"].Style.Font.Bold = true;
        worksheet.Cells["A9"].Style.Font.Size = 14;

        worksheet.Cells["A11"].Value = "Milestone";
        worksheet.Cells["B11"].Value = "Total";
        worksheet.Cells["C11"].Value = "Graded";
        worksheet.Cells["D11"].Value = "Pending";
        worksheet.Cells["E11"].Value = "Rate %";
        worksheet.Cells["F11"].Value = "Avg Grade";
        worksheet.Cells["A11:F11"].Style.Font.Bold = true;
        worksheet.Cells["A11:F11"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A11:F11"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        int row = 12;
        foreach (var item in report.CompletionByMilestone)
        {
            worksheet.Cells[row, 1].Value = item.MilestoneName;
            worksheet.Cells[row, 2].Value = item.TotalSubmissions;
            worksheet.Cells[row, 3].Value = item.GradedSubmissions;
            worksheet.Cells[row, 4].Value = item.PendingSubmissions;
            worksheet.Cells[row, 5].Value = $"{item.CompletionRate}%";
            worksheet.Cells[row, 6].Value = item.AverageGrade;
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    private byte[] GenerateGradesExcel(GradesDistributionReportDto report)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Grades Distribution");

        // Header
        worksheet.Cells["A1"].Value = "GRADES DISTRIBUTION REPORT";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;

        // Summary
        worksheet.Cells["A3"].Value = "Total Graded Projects:";
        worksheet.Cells["B3"].Value = report.TotalGradedProjects;
        worksheet.Cells["A4"].Value = "Average Grade:";
        worksheet.Cells["B4"].Value = report.AverageGrade;
        worksheet.Cells["A5"].Value = "Highest Grade:";
        worksheet.Cells["B5"].Value = report.HighestGrade;
        worksheet.Cells["A6"].Value = "Lowest Grade:";
        worksheet.Cells["B6"].Value = report.LowestGrade;
        worksheet.Cells["A7"].Value = "Median Grade:";
        worksheet.Cells["B7"].Value = report.MedianGrade;
        worksheet.Cells["A3:A7"].Style.Font.Bold = true;

        // Grade ranges
        worksheet.Cells["A9"].Value = "Grade Distribution";
        worksheet.Cells["A9"].Style.Font.Bold = true;
        worksheet.Cells["A9"].Style.Font.Size = 14;

        worksheet.Cells["A11"].Value = "Range";
        worksheet.Cells["B11"].Value = "Count";
        worksheet.Cells["C11"].Value = "Percentage";
        worksheet.Cells["A11:C11"].Style.Font.Bold = true;
        worksheet.Cells["A11:C11"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A11:C11"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        int row = 12;
        foreach (var item in report.GradeRanges)
        {
            worksheet.Cells[row, 1].Value = item.Range;
            worksheet.Cells[row, 2].Value = item.Count;
            worksheet.Cells[row, 3].Value = $"{item.Percentage}%";
            row++;
        }

        // Top projects
        row += 2;
        worksheet.Cells[row, 1].Value = "Top Performing Projects";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 1].Style.Font.Size = 14;

        row += 2;
        worksheet.Cells[row, 1].Value = "Project Name";
        worksheet.Cells[row, 2].Value = "Group";
        worksheet.Cells[row, 3].Value = "Class";
        worksheet.Cells[row, 4].Value = "Grade";
        worksheet.Cells[row, 5].Value = "Semester";
        worksheet.Cells[$"A{row}:E{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"A{row}:E{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[$"A{row}:E{row}"].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        row++;
        foreach (var item in report.TopProjects)
        {
            worksheet.Cells[row, 1].Value = item.ProjectName;
            worksheet.Cells[row, 2].Value = item.GroupName;
            worksheet.Cells[row, 3].Value = item.ClassName;
            worksheet.Cells[row, 4].Value = item.Grade;
            worksheet.Cells[row, 5].Value = item.SemesterName;
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    #endregion
}
