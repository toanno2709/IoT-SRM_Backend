using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Constants;
using SemesterEntity = AppBackend.BusinessObjects.Models.Semester;
using ClassEntity = AppBackend.BusinessObjects.Models.Class;
using StudentCourseHistoryEntity = AppBackend.BusinessObjects.Models.StudentCourseHistory;
using GroupEntity = AppBackend.BusinessObjects.Models.Group;
using ProjectEntity = AppBackend.BusinessObjects.Models.Project;
using ClassEnrollmentEntity = AppBackend.BusinessObjects.Models.ClassEnrollment;
using ProjectMilestoneEntity = AppBackend.BusinessObjects.Models.ProjectMilestone;
using GroupMemberEntity = AppBackend.BusinessObjects.Models.GroupMember;
using MilestoneEvaluationEntity = AppBackend.BusinessObjects.Models.MilestoneEvaluation;
using FinalProjectSubmissionEntity = AppBackend.BusinessObjects.Models.FinalProjectSubmission;
using AppBackend.BusinessObjects.Models;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Repositories.Repositories.FinalProjectRepo;
using AppBackend.Repositories.Repositories.StudentCourseHistoryRepo;
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
    private readonly IStudentCourseHistoryRepository _studentCourseHistoryRepository;
    private readonly IotShowroomContext _context;

    public AdminReportService(
        IClassRepository classRepository,
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        IProjectRepository projectRepository,
        IMilestoneEvaluationRepository milestoneEvaluationRepository,
        IFinalProjectRepository finalProjectRepository,
        IStudentCourseHistoryRepository studentCourseHistoryRepository,
        IotShowroomContext context)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _projectRepository = projectRepository;
        _milestoneEvaluationRepository = milestoneEvaluationRepository;
        _finalProjectRepository = finalProjectRepository;
        _studentCourseHistoryRepository = studentCourseHistoryRepository;
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
            var instructors = await _userRepository.GetByRoleAsync(2);
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
                    PendingProposals = 0,
                    SubmissionsToGrade = 0
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
            var students = await _userRepository.GetByRoleAsync(3);
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
                    WithoutGroups = 0
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

            return new ResultModel<ReportExportResponseDto>
            {
                IsSuccess = false,
                StatusCode = 501,
                Message = "Excel export is temporarily disabled. Export methods are under development."
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
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Get comprehensive data
            var dataResult = await GetComprehensiveSemesterReportAsync(semesterId);
            if (!dataResult.IsSuccess || dataResult.Data == null)
            {
                return new ResultModel<ReportExportResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = dataResult.StatusCode,
                    Message = dataResult.Message
                };
            }

            var data = dataResult.Data;

            using var package = new ExcelPackage();

            // Sheet 1 - Semester Overview
            var sheet1 = package.Workbook.Worksheets.Add("Semester Overview");
            CreateSemesterOverviewSheet(sheet1, data.SemesterOverview);

            // Sheet 2 - Class List
            var sheet2 = package.Workbook.Worksheets.Add("Class List");
            CreateClassesSheet(sheet2, data.Classes);

            // Sheet 3 - Instructor List
            var sheet3 = package.Workbook.Worksheets.Add("Instructor List");
            CreateInstructorsSheet(sheet3, data.Instructors);

            // Sheet 4 - Student List
            var sheet4 = package.Workbook.Worksheets.Add("Student List");
            CreateStudentsSheet(sheet4, data.Students);

            // Sheet 5 - Milestone Grades
            var sheet5 = package.Workbook.Worksheets.Add("Milestone Grades");
            CreateMilestoneGradesSheet(sheet5, data.MilestoneGrades);

            // Sheet 6 - Grader Grades (changed from "Final Grades")
            var sheet6 = package.Workbook.Worksheets.Add("Grader Grades");
            CreateFinalSubmissionsSheet(sheet6, data.FinalSubmissions);

            // Sheet 7 - Pass Not Pass Status
            var sheet7 = package.Workbook.Worksheets.Add("Pass Not Pass Status");
            CreatePassStatusSheet(sheet7, data.StudentPassStatus);

            var fileBytes = package.GetAsByteArray();
            
            // Generate file name using Semester Name
            var semesterName = data.SemesterOverview?.SemesterName ?? $"Semester{semesterId}";
            // Remove invalid file name characters
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            var sanitizedSemesterName = string.Join("_", semesterName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            var fileName = $"{sanitizedSemesterName}_Reports.xlsx";

            return new ResultModel<ReportExportResponseDto>
            {
                IsSuccess = true,
                Data = new ReportExportResponseDto
                {
                    FileName = fileName,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileSizeBytes = fileBytes.Length,
                    GeneratedAt = DateTime.UtcNow,
                    ExportFormat = "Excel",
                    FileContent = fileBytes // Return byte array instead of base64
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
                Message = $"Error generating comprehensive report: {ex.Message}"
            };
        }
    }

    #region Excel Sheet Creation Methods

    private void CreateSemesterOverviewSheet(ExcelWorksheet sheet, SemesterOverviewDto? overview)
    {
        if (overview == null) return;

        // Header styling
        sheet.Cells["A1:B1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells["A1:B1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
        sheet.Cells["A1:B1"].Style.Font.Color.SetColor(Color.White);
        sheet.Cells["A1:B1"].Style.Font.Bold = true;

        // Headers
        sheet.Cells["A1"].Value = "Information";
        sheet.Cells["B1"].Value = "Value";

        // Data
        int row = 2;
        sheet.Cells[$"A{row}"].Value = "Semester Name";
        sheet.Cells[$"B{row++}"].Value = overview.SemesterName;
        
        sheet.Cells[$"A{row}"].Value = "Semester Code";
        sheet.Cells[$"B{row++}"].Value = overview.SemesterCode;
        
        sheet.Cells[$"A{row}"].Value = "Year";
        sheet.Cells[$"B{row++}"].Value = overview.Year;
        
        sheet.Cells[$"A{row}"].Value = "Term";
        sheet.Cells[$"B{row++}"].Value = overview.Term;
        
        sheet.Cells[$"A{row}"].Value = "Start Date";
        sheet.Cells[$"B{row++}"].Value = overview.StartDate?.ToString("yyyy-MM-dd");
        
        sheet.Cells[$"A{row}"].Value = "End Date";
        sheet.Cells[$"B{row++}"].Value = overview.EndDate?.ToString("yyyy-MM-dd");
        
        // Removed "Is Active" row
        
        row++;
        sheet.Cells[$"A{row}"].Value = "Total Classes";
        sheet.Cells[$"B{row++}"].Value = overview.TotalClasses;
        
        sheet.Cells[$"A{row}"].Value = "Total Students";
        sheet.Cells[$"B{row++}"].Value = overview.TotalStudents;
        
        sheet.Cells[$"A{row}"].Value = "Total Groups";
        sheet.Cells[$"B{row++}"].Value = overview.TotalGroups;
        
        sheet.Cells[$"A{row}"].Value = "Total Projects";
        sheet.Cells[$"B{row++}"].Value = overview.TotalProjects;

        // Borders
        var usedRange = sheet.Cells[1, 1, row - 1, 2];
        usedRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        usedRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        usedRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        usedRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
    }

    private void CreateClassesSheet(ExcelWorksheet sheet, List<SemesterClassDetailDto> classes)
    {
        // Header styling
        sheet.Cells["A1:H1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells["A1:H1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(112, 173, 71));
        sheet.Cells["A1:H1"].Style.Font.Color.SetColor(Color.White);
        sheet.Cells["A1:H1"].Style.Font.Bold = true;

        // Headers
        sheet.Cells["A1"].Value = "Class ID";
        sheet.Cells["B1"].Value = "Class Name";
        sheet.Cells["C1"].Value = "Instructor ID";
        sheet.Cells["D1"].Value = "Instructor Name";
        sheet.Cells["E1"].Value = "Instructor Email";
        sheet.Cells["F1"].Value = "Number of Students";
        sheet.Cells["G1"].Value = "Number of Groups";
        sheet.Cells["H1"].Value = "Number of Projects";

        // Data
        int row = 2;
        foreach (var cls in classes)
        {
            sheet.Cells[$"A{row}"].Value = cls.ClassId;
            sheet.Cells[$"B{row}"].Value = cls.ClassName;
            sheet.Cells[$"C{row}"].Value = cls.InstructorId;
            sheet.Cells[$"D{row}"].Value = cls.InstructorName;
            sheet.Cells[$"E{row}"].Value = cls.InstructorEmail;
            sheet.Cells[$"F{row}"].Value = cls.TotalStudents;
            sheet.Cells[$"G{row}"].Value = cls.TotalGroups;
            sheet.Cells[$"H{row}"].Value = cls.TotalProjects;
            row++;
        }

        // Borders
        if (row > 2)
        {
            var usedRange = sheet.Cells[1, 1, row - 1, 8];
            usedRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
    }

    private void CreateInstructorsSheet(ExcelWorksheet sheet, List<SemesterInstructorDetailDto> instructors)
    {
        // Header styling
        sheet.Cells["A1:E1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells["A1:E1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(237, 125, 49));
        sheet.Cells["A1:E1"].Style.Font.Color.SetColor(Color.White);
        sheet.Cells["A1:E1"].Style.Font.Bold = true;

        // Headers
        sheet.Cells["A1"].Value = "Instructor ID";
        sheet.Cells["B1"].Value = "Full Name";
        sheet.Cells["C1"].Value = "Email";
        sheet.Cells["D1"].Value = "Number of Students";
        sheet.Cells["E1"].Value = "Number of Projects";

        // Data
        int row = 2;
        foreach (var instructor in instructors)
        {
            sheet.Cells[$"A{row}"].Value = instructor.InstructorId;
            sheet.Cells[$"B{row}"].Value = instructor.FullName;
            sheet.Cells[$"C{row}"].Value = instructor.Email;
            sheet.Cells[$"D{row}"].Value = instructor.TotalStudents;
            sheet.Cells[$"E{row}"].Value = instructor.TotalProjects;
            row++;
        }

        // Borders
        if (row > 2)
        {
            var usedRange = sheet.Cells[1, 1, row - 1, 5];
            usedRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
    }

    private void CreateStudentsSheet(ExcelWorksheet sheet, List<SemesterStudentDetailDto> students)
    {
        // Header styling
        sheet.Cells["A1:J1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells["A1:J1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
        sheet.Cells["A1:J1"].Style.Font.Color.SetColor(Color.White);
        sheet.Cells["A1:J1"].Style.Font.Bold = true;

        // Headers
        sheet.Cells["A1"].Value = "Student ID";
        sheet.Cells["B1"].Value = "Full Name";
        sheet.Cells["C1"].Value = "Email";
        sheet.Cells["D1"].Value = "Student Code";
        sheet.Cells["E1"].Value = "Class ID";
        sheet.Cells["F1"].Value = "Class Name";
        sheet.Cells["G1"].Value = "Group ID";
        sheet.Cells["H1"].Value = "Group Name";
        sheet.Cells["I1"].Value = "Role";
        sheet.Cells["J1"].Value = "Project Name";

        // Data
        int row = 2;
        foreach (var student in students)
        {
            sheet.Cells[$"A{row}"].Value = student.StudentId;
            sheet.Cells[$"B{row}"].Value = student.FullName;
            sheet.Cells[$"C{row}"].Value = student.Email;
            sheet.Cells[$"D{row}"].Value = student.StudentCode;
            sheet.Cells[$"E{row}"].Value = student.ClassId;
            sheet.Cells[$"F{row}"].Value = student.ClassName;
            sheet.Cells[$"G{row}"].Value = student.GroupId;
            sheet.Cells[$"H{row}"].Value = student.GroupName;
            sheet.Cells[$"I{row}"].Value = student.RoleInGroup;
            sheet.Cells[$"J{row}"].Value = student.ProjectName;
            row++;
        }

        // Borders
        if (row > 2)
        {
            var usedRange = sheet.Cells[1, 1, row - 1, 10];
            usedRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
    }

    private void CreateMilestoneGradesSheet(ExcelWorksheet sheet, List<MilestoneGradeDetailDto> grades)
    {
        // Header styling
        sheet.Cells["A1:L1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells["A1:L1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 192, 0));
        sheet.Cells["A1:L1"].Style.Font.Color.SetColor(Color.Black);
        sheet.Cells["A1:L1"].Style.Font.Bold = true;

        // Headers
        sheet.Cells["A1"].Value = "Class ID";
        sheet.Cells["B1"].Value = "Class Name";
        sheet.Cells["C1"].Value = "Group ID";
        sheet.Cells["D1"].Value = "Group Name";
        sheet.Cells["E1"].Value = "Project ID";
        sheet.Cells["F1"].Value = "Project Name";
        sheet.Cells["G1"].Value = "Milestone Name";
        sheet.Cells["H1"].Value = "Weight (%)";
        sheet.Cells["I1"].Value = "Score";
        sheet.Cells["J1"].Value = "Weighted Score";
        sheet.Cells["K1"].Value = "Graded By";
        sheet.Cells["L1"].Value = "Graded Date";

        // Data
        int row = 2;
        foreach (var grade in grades)
        {
            sheet.Cells[$"A{row}"].Value = grade.ClassId;
            sheet.Cells[$"B{row}"].Value = grade.ClassName;
            sheet.Cells[$"C{row}"].Value = grade.GroupId;
            sheet.Cells[$"D{row}"].Value = grade.GroupName;
            sheet.Cells[$"E{row}"].Value = grade.ProjectId;
            sheet.Cells[$"F{row}"].Value = grade.ProjectTitle;
            sheet.Cells[$"G{row}"].Value = grade.MilestoneName;
            sheet.Cells[$"H{row}"].Value = grade.MilestoneWeight;
            sheet.Cells[$"I{row}"].Value = grade.Score;
            sheet.Cells[$"J{row}"].Value = grade.WeightedScore;
            sheet.Cells[$"K{row}"].Value = grade.GradedByInstructorName;
            sheet.Cells[$"L{row}"].Value = grade.GradedAt?.ToString("yyyy-MM-dd HH:mm");
            row++;
        }

        // Borders
        if (row > 2)
        {
            var usedRange = sheet.Cells[1, 1, row - 1, 12];
            usedRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
    }

    private void CreateFinalSubmissionsSheet(ExcelWorksheet sheet, List<FinalSubmissionDetailDto> submissions)
    {
        // Determine maximum number of graders across all submissions
        int maxGraders = submissions.Any() 
            ? submissions.Max(s => s.GraderGrades?.Count ?? 0) 
            : 0;

        // Calculate total columns: 7 base columns + (3 columns per grader)
        int baseColumns = 7; // A through G
        int totalColumns = baseColumns + (maxGraders * 3);

        // Header styling
        var headerRange = sheet.Cells[1, 1, 1, totalColumns];
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(146, 208, 80));
        headerRange.Style.Font.Color.SetColor(Color.Black);
        headerRange.Style.Font.Bold = true;

        // Base Headers (columns A-G)
        sheet.Cells["A1"].Value = "Class ID";
        sheet.Cells["B1"].Value = "Class Name";
        sheet.Cells["C1"].Value = "Group ID";
        sheet.Cells["D1"].Value = "Group Name";
        sheet.Cells["E1"].Value = "Project ID";
        sheet.Cells["F1"].Value = "Project Name";
        sheet.Cells["G1"].Value = "Average Grade";

        // Dynamic grader columns (H onwards)
        int currentColumn = 8; // Column H
        for (int i = 1; i <= maxGraders; i++)
        {
            sheet.Cells[1, currentColumn].Value = $"Grader {i} Name";
            sheet.Cells[1, currentColumn + 1].Value = $"Grader {i} Email";
            sheet.Cells[1, currentColumn + 2].Value = $"Grader {i} Grade";
            currentColumn += 3;
        }

        // Data
        int row = 2;
        foreach (var submission in submissions)
        {
            sheet.Cells[$"A{row}"].Value = submission.ClassId;
            sheet.Cells[$"B{row}"].Value = submission.ClassName;
            sheet.Cells[$"C{row}"].Value = submission.GroupId;
            sheet.Cells[$"D{row}"].Value = submission.GroupName;
            sheet.Cells[$"E{row}"].Value = submission.ProjectId;
            sheet.Cells[$"F{row}"].Value = submission.ProjectTitle;
            sheet.Cells[$"G{row}"].Value = submission.AverageGrade;

            // Fill grader information
            currentColumn = 8; // Start from column H
            if (submission.GraderGrades != null && submission.GraderGrades.Any())
            {
                // Sort graders by GradedAt to maintain consistent order
                var sortedGraders = submission.GraderGrades.OrderBy(g => g.GradedAt).ToList();
                
                foreach (var grader in sortedGraders)
                {
                    sheet.Cells[row, currentColumn].Value = grader.GraderName;
                    sheet.Cells[row, currentColumn + 1].Value = grader.GraderEmail;
                    sheet.Cells[row, currentColumn + 2].Value = grader.Grade;
                    currentColumn += 3;
                }
            }

            row++;
        }

        // Borders
        if (row > 2)
        {
            var usedRange = sheet.Cells[1, 1, row - 1, totalColumns];
            usedRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
    }

    private void CreatePassStatusSheet(ExcelWorksheet sheet, List<StudentPassStatusDto> statuses)
    {
        // Header styling
        sheet.Cells["A1:J1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells["A1:J1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
        sheet.Cells["A1:J1"].Style.Font.Color.SetColor(Color.White);
        sheet.Cells["A1:J1"].Style.Font.Bold = true;

        // Headers - removed "Final Submitted" and "Project Status", added "Average Grader Score"
        sheet.Cells["A1"].Value = "Student ID";
        sheet.Cells["B1"].Value = "Full Name";
        sheet.Cells["C1"].Value = "Email";
        sheet.Cells["D1"].Value = "Class";
        sheet.Cells["E1"].Value = "Group";
        sheet.Cells["F1"].Value = "Project";
        sheet.Cells["G1"].Value = "Milestone Score";
        sheet.Cells["H1"].Value = "Average Grader Score";
        sheet.Cells["I1"].Value = "Overall Score";
        sheet.Cells["J1"].Value = "RESULT";

        // Data
        int row = 2;
        foreach (var status in statuses)
        {
            sheet.Cells[$"A{row}"].Value = status.StudentId;
            sheet.Cells[$"B{row}"].Value = status.StudentName;
            sheet.Cells[$"C{row}"].Value = status.StudentEmail;
            sheet.Cells[$"D{row}"].Value = status.ClassName;
            sheet.Cells[$"E{row}"].Value = status.GroupName;
            sheet.Cells[$"F{row}"].Value = status.ProjectTitle;
            sheet.Cells[$"G{row}"].Value = status.TotalMilestoneScore;
            sheet.Cells[$"H{row}"].Value = status.AverageGraderScore;
            sheet.Cells[$"I{row}"].Value = status.OverallScore;
            sheet.Cells[$"J{row}"].Value = status.PassStatus;

            // Highlight PASS/NOT PASS based on correct logic
            if (status.IsPassed)
            {
                sheet.Cells[$"J{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[$"J{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(146, 208, 80)); // Green
                sheet.Cells[$"J{row}"].Style.Font.Bold = true;
            }
            else
            {
                sheet.Cells[$"J{row}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[$"J{row}"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 0, 0)); // Red
                sheet.Cells[$"J{row}"].Style.Font.Color.SetColor(Color.White);
                sheet.Cells[$"J{row}"].Style.Font.Bold = true;
            }

            row++;
        }

        // Borders
        if (row > 2)
        {
            var usedRange = sheet.Cells[1, 1, row - 1, 10];
            usedRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            usedRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
    }

    #endregion

    public async Task<ResultModel<ComprehensiveSemesterReportDto>> GetComprehensiveSemesterReportAsync(int semesterId)
    {
        try
        {
            // Verify semester exists
            var semester = await _context.Semesters
                .FirstOrDefaultAsync(s => s.SemesterId == semesterId);

            if (semester == null)
            {
                return new ResultModel<ComprehensiveSemesterReportDto>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Message = "Semester not found"
                };
            }

            // Get all classes in semester
            var classes = await _context.Classes
                .Include(c => c.Instructor)
                .Include(c => c.ClassEnrollments)
                .Include(c => c.Groups)
                    .ThenInclude(g => g.Projects)
                .Where(c => c.SemesterId == semesterId)
                .ToListAsync();

            var classIds = classes.Select(c => c.ClassId).ToList();

            // Get all groups in semester
            var groups = await _context.Groups
                .Include(g => g.GroupMembers)
                    .ThenInclude(gm => gm.User)
                .Include(g => g.Projects)
                .Include(g => g.Class)
                .Where(g => classIds.Contains(g.ClassId))
                .ToListAsync();

            var groupIds = groups.Select(g => g.GroupId).ToList();

            // Get all projects
            var projects = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                .Include(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
                        .ThenInclude(gm => gm.User)
                .Where(p => p.Group != null && classIds.Contains(p.Group.ClassId))
                .ToListAsync();

            var projectIds = projects.Select(p => p.ProjectId).ToList();

            // Get all milestone evaluations
            var milestoneEvaluations = await _context.MilestoneEvaluations
                .Include(me => me.MilestoneDef)
                .Include(me => me.Instructor)
                .Include(me => me.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.Class)
                .Include(me => me.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.GroupMembers)
                            .ThenInclude(gm => gm.User)
                .Where(me => projectIds.Contains(me.ProjectId))
                .ToListAsync();

            // Get all final submissions with grades
            var finalSubmissions = await _context.FinalProjectSubmissions
                .Include(fs => fs.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.Class)
                .Include(fs => fs.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.GroupMembers)
                            .ThenInclude(gm => gm.User)
                .Include(fs => fs.FinalSubmissionGrades)
                    .ThenInclude(fsg => fsg.Instructor)
                .Where(fs => projectIds.Contains(fs.ProjectId))
                .ToListAsync();

            // Get all students in semester
            var studentIds = await _context.ClassEnrollments
                .Where(ce => ce.ClassId.HasValue && classIds.Contains(ce.ClassId.Value) && ce.StudentId.HasValue)
                .Select(ce => ce.StudentId.Value)
                .Distinct()
                .ToListAsync();

            var students = await _context.Users
                .Where(u => studentIds.Contains(u.UserId))
                .ToListAsync();

            // Get all instructors
            var instructorIds = classes
                .Where(c => c.InstructorId.HasValue)
                .Select(c => c.InstructorId!.Value)
                .Distinct()
                .ToList();

            var instructors = await _context.Users
                .Where(u => instructorIds.Contains(u.UserId))
                .ToListAsync();

            // Build report
            var report = new ComprehensiveSemesterReportDto
            {
                SemesterOverview = new SemesterOverviewDto
                {
                    SemesterId = semester.SemesterId,
                    SemesterName = semester.Name,
                    SemesterCode = semester.Code,
                    Year = semester.Year,
                    Term = semester.Term,
                    StartDate = semester.StartDate.HasValue ? semester.StartDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    EndDate = semester.EndDate.HasValue ? semester.EndDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    IsActive = semester.IsActive ?? false,
                    TotalClasses = classes.Count,
                    TotalStudents = students.Count,
                    TotalGroups = groups.Count,
                    TotalProjects = projects.Count
                },

                Classes = classes.Select(c => new SemesterClassDetailDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    InstructorId = c.InstructorId,
                    InstructorName = c.Instructor?.FullName,
                    InstructorEmail = c.Instructor?.Email,
                    TotalStudents = c.ClassEnrollments?.Count ?? 0,
                    TotalGroups = c.Groups?.Count ?? 0,
                    TotalProjects = c.Groups?.Sum(g => g.Projects?.Count ?? 0) ?? 0,
                    CreatedAt = c.CreatedAt
                }).ToList(),

                Instructors = instructors.Select(i => new SemesterInstructorDetailDto
                {
                    InstructorId = i.UserId,
                    FullName = i.FullName,
                    Email = i.Email,
                    Classes = classes.Where(c => c.InstructorId == i.UserId)
                        .Select(c => c.ClassName ?? "").ToList(),
                    TotalStudents = classes.Where(c => c.InstructorId == i.UserId)
                        .Sum(c => c.ClassEnrollments?.Count ?? 0),
                    TotalProjects = classes.Where(c => c.InstructorId == i.UserId)
                        .Sum(c => c.Groups?.Sum(g => g.Projects?.Count ?? 0) ?? 0)
                }).ToList(),

                Students = students.Select(s =>
                {
                    var enrollment = _context.ClassEnrollments
                        .Include(ce => ce.Class)
                        .FirstOrDefault(ce => ce.StudentId.HasValue && 
                                             ce.StudentId.Value == s.UserId && 
                                             ce.ClassId.HasValue && 
                                             classIds.Contains(ce.ClassId.Value));

                    var groupMember = _context.GroupMembers
                        .Include(gm => gm.Group)
                            .ThenInclude(g => g.Projects)
                        .FirstOrDefault(gm => gm.UserId == s.UserId && 
                            gm.Group != null && classIds.Contains(gm.Group.ClassId));

                    var project = groupMember?.Group?.Projects?.FirstOrDefault();

                    return new SemesterStudentDetailDto
                    {
                        StudentId = s.UserId,
                        FullName = s.FullName,
                        Email = s.Email,
                        StudentCode = null, // User model doesn't have StudentCode property
                        ClassId = enrollment?.ClassId,
                        ClassName = enrollment?.Class?.ClassName,
                        GroupId = groupMember?.GroupId,
                        GroupName = groupMember?.Group?.GroupName,
                        RoleInGroup = groupMember?.RoleInGroup == "Leader" ? "Leader" : "Member",
                        ProjectId = project?.ProjectId,
                        ProjectName = project?.Title
                    };
                }).ToList(),

            Groups = groups.Select(g => new SemesterGroupDetailDto
            {
                GroupId = g.GroupId,
                GroupName = g.GroupName,
                ClassId = g.ClassId,
                ClassName = g.Class?.ClassName,
                MemberCount = g.GroupMembers?.Count ?? 0,
                Members = g.GroupMembers?.Select(gm => gm.User?.FullName ?? "").ToList() ?? new List<string>(),
                LeaderName = g.GroupMembers?.FirstOrDefault(gm => gm.RoleInGroup == "Leader")?.User?.FullName,
                ProjectId = g.Projects?.FirstOrDefault()?.ProjectId,
                ProjectName = g.Projects?.FirstOrDefault()?.Title,
                CreatedAt = g.CreatedAt
            }).ToList(),

            Projects = projects.Select(p => new SemesterProjectDetailDto
            {
                ProjectId = p.ProjectId,
                ProjectTitle = p.Title,
                Description = p.Description,
                Component = p.Component,
                Status = p.Status,
                GroupId = p.GroupId ?? 0,
                GroupName = p.Group?.GroupName,
                ClassId = p.Group?.ClassId ?? 0,
                ClassName = p.Group?.Class?.ClassName,
                CreatedAt = p.CreatedAt
            }).ToList(),

            MilestoneGrades = milestoneEvaluations.Select(me => new MilestoneGradeDetailDto
            {
                ProjectId = me.ProjectId,
                ProjectTitle = me.Project?.Title,
                GroupId = me.Project?.GroupId ?? 0,
                GroupName = me.Project?.Group?.GroupName,
                ClassId = me.Project?.Group?.ClassId ?? 0,
                ClassName = me.Project?.Group?.Class?.ClassName,
                MilestoneId = me.MilestoneDefId,
                MilestoneName = me.MilestoneDef?.Title,
                MilestoneWeight = me.MilestoneDef?.Weight,
                Score = me.Score,
                WeightedScore = me.Score * (me.MilestoneDef?.Weight ?? 0) / 100,
                GradedByInstructorId = me.InstructorId,
                GradedByInstructorName = me.Instructor?.FullName,
                GradedAt = me.EvaluatedAt,
                Feedback = me.Feedback,
                Students = me.Project?.Group?.GroupMembers?.Select(gm => new MilestoneGradeStudentDto
                {
                    StudentId = gm.UserId,
                    StudentName = gm.User?.FullName,
                    StudentEmail = gm.User?.Email,
                    RoleInGroup = gm.RoleInGroup == "Leader" ? "Leader" : "Member"
                }).ToList() ?? new List<MilestoneGradeStudentDto>()
            }).ToList(),

            FinalSubmissions = finalSubmissions.Select(fs => new FinalSubmissionDetailDto
            {
                FinalSubmissionId = fs.FinalSubmissionId,
                ProjectId = fs.ProjectId,
                ProjectTitle = fs.Project?.Title,
                GroupId = fs.Project?.GroupId ?? 0,
                GroupName = fs.Project?.Group?.GroupName,
                ClassId = fs.Project?.Group?.ClassId ?? 0,
                ClassName = fs.Project?.Group?.Class?.ClassName,
                SubmittedAt = fs.SubmittedAt,
                SubmissionUrl = fs.FinalReportUrl,
                Description = fs.SubmissionNotes,
                GraderGrades = fs.FinalSubmissionGrades?.Select(fsg => new FinalSubmissionGraderDto
                {
                    GraderId = fsg.InstructorId,
                    GraderName = fsg.Instructor?.FullName,
                    GraderEmail = fsg.Instructor?.Email,
                    Grade = fsg.Grade,
                    Feedback = fsg.Feedback,
                    GradedAt = fsg.GradedAt
                }).ToList() ?? new List<FinalSubmissionGraderDto>(),
                AverageGrade = fs.Grade,
                Students = fs.Project?.Group?.GroupMembers?.Select(gm => new FinalSubmissionStudentDto
                {
                    StudentId = gm.UserId,
                    StudentName = gm.User?.FullName,
                    StudentEmail = gm.User?.Email,
                    RoleInGroup = gm.RoleInGroup == "Leader" ? "Leader" : "Member"
                }).ToList() ?? new List<FinalSubmissionStudentDto>()
            }).ToList()
        };

        // Calculate pass/not pass status for each student
        report.StudentPassStatus = students.Select(s =>
        {
            var groupMember = _context.GroupMembers
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.Projects)
                .FirstOrDefaultAsync(gm => gm.UserId == s.UserId && 
                    gm.Group != null && 
                    classIds.Contains(gm.Group.ClassId)).Result;

            var project = groupMember?.Group?.Projects?.FirstOrDefault();
            
            var enrollment = _context.ClassEnrollments
                .Include(ce => ce.Class)
                .FirstOrDefaultAsync(ce => ce.StudentId.HasValue && 
                                             ce.StudentId.Value == s.UserId && 
                                             ce.ClassId.HasValue && 
                                             classIds.Contains(ce.ClassId.Value)).Result;

            decimal totalMilestoneScore = 0;
            if (project != null)
            {
                var projectMilestones = milestoneEvaluations
                    .Where(me => me.ProjectId == project.ProjectId)
                    .ToList();
                
                totalMilestoneScore = projectMilestones
                    .Sum(me => me.Score * (me.MilestoneDef?.Weight ?? 0) / 100);
            }

            var finalSubmission = project != null 
                ? finalSubmissions.FirstOrDefault(fs => fs.ProjectId == project.ProjectId)
                : null;

            // Get average grader score from graders
            decimal? averageGraderScore = null;
            if (finalSubmission?.FinalSubmissionGrades?.Any() == true)
            {
                averageGraderScore = finalSubmission.FinalSubmissionGrades.Average(fsg => fsg.Grade);
            }

            decimal? finalScore = finalSubmission?.Grade;
            
            // Calculate overall: 40% milestone + 60% final
            decimal? overallScore = null;
            if (finalScore.HasValue)
            {
                overallScore = (totalMilestoneScore * 0.4m) + (finalScore.Value * 0.6m);
            }

            bool hasFinalSubmission = finalSubmission != null;
            bool isProjectCompleted = project?.Status == "Completed";
            
            // Fix PASS logic: check overallScore >= 50 AND project completed AND has final submission
            bool isPassed = overallScore.HasValue && 
                           overallScore.Value >= 50 && 
                           isProjectCompleted && 
                           hasFinalSubmission;

            return new StudentPassStatusDto
            {
                StudentId = s.UserId,
                StudentName = s.FullName,
                StudentEmail = s.Email,
                StudentCode = null,
                ClassId = enrollment?.ClassId,
                ClassName = enrollment?.Class?.ClassName,
                GroupId = groupMember?.GroupId,
                GroupName = groupMember?.Group?.GroupName,
                ProjectId = project?.ProjectId,
                ProjectTitle = project?.Title,
                TotalMilestoneScore = Math.Round(totalMilestoneScore, 2),
                AverageGraderScore = averageGraderScore.HasValue ? Math.Round(averageGraderScore.Value, 2) : null,
                FinalScore = finalScore.HasValue ? Math.Round(finalScore.Value, 2) : null,
                OverallScore = overallScore.HasValue ? Math.Round(overallScore.Value, 2) : null,
                HasFinalSubmission = hasFinalSubmission,
                IsProjectCompleted = isProjectCompleted,
                IsPassed = isPassed,
                PassStatus = isPassed ? "PASS" : "NOT PASS"
            };
        }).ToList();

        return new ResultModel<ComprehensiveSemesterReportDto>
        {
            IsSuccess = true,
            Data = report,
            Message = CommonMessageConstants.GET_SUCCESS
        };
        }
        catch (Exception ex)
        {
            return new ResultModel<ComprehensiveSemesterReportDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error generating comprehensive semester report: {ex.Message}"
            };
        }
    }
}
