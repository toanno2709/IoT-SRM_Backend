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
            // TODO: Implement comprehensive semester report export
            // Currently under development - requires proper data structure alignment
            
            return new ResultModel<ReportExportResponseDto>
            {
                IsSuccess = false,
                StatusCode = 501,
                Message = "Comprehensive semester report export feature is under development. Please check back later."
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

            decimal? finalScore = finalSubmission?.Grade;
            
            // Calculate overall: 40% milestone + 60% final
            decimal? overallScore = null;
            if (finalScore.HasValue)
            {
                overallScore = (totalMilestoneScore * 0.4m) + (finalScore.Value * 0.6m);
            }

            bool hasFinalSubmission = finalSubmission != null;
            bool isProjectCompleted = project?.Status == "Completed";
            bool isPassed = overallScore.HasValue && 
                           overallScore.Value >= 50 && 
                           isProjectCompleted && 
                           hasFinalSubmission;

            return new StudentPassStatusDto
            {
                StudentId = s.UserId,
                StudentName = s.FullName,
                StudentEmail = s.Email,
                StudentCode = null, // User model doesn't have StudentCode property
                ClassId = enrollment?.ClassId,
                ClassName = enrollment?.Class?.ClassName,
                GroupId = groupMember?.GroupId,
                GroupName = groupMember?.Group?.GroupName,
                ProjectId = project?.ProjectId,
                ProjectTitle = project?.Title,
                ProjectStatus = project?.Status,
                TotalMilestoneScore = Math.Round(totalMilestoneScore, 2),
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
