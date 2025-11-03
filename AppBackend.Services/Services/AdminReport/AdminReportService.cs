using AppBackend.BusinessObjects.Constants;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Repositories.Repositories.FinalProjectRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.AdminReport;

public class AdminReportService : IAdminReportService
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IMilestoneEvaluationRepository _milestoneEvaluationRepository;
    private readonly IFinalProjectRepository _finalProjectRepository;

    public AdminReportService(
        IClassRepository classRepository,
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        IProjectRepository projectRepository,
        IMilestoneEvaluationRepository milestoneEvaluationRepository,
        IFinalProjectRepository finalProjectRepository)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _projectRepository = projectRepository;
        _milestoneEvaluationRepository = milestoneEvaluationRepository;
        _finalProjectRepository = finalProjectRepository;
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
            // TODO: Implement actual export functionality
            // This would require libraries like EPPlus for Excel or iTextSharp for PDF
            
            await Task.CompletedTask; // Placeholder to avoid warning
            
            return new ResultModel<ReportExportResponseDto>
            {
                IsSuccess = false,
                StatusCode = 501,
                Message = "Export functionality not yet implemented. This would require EPPlus (Excel) or iTextSharp (PDF) libraries."
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
}
