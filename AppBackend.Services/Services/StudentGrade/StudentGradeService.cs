using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Data;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace AppBackend.Services.Services.StudentGrade;

public class StudentGradeService : IStudentGradeService
{
    private readonly IotShowroomContext _context;
    private readonly ILogger<StudentGradeService> _logger;

    public StudentGradeService(IotShowroomContext context, ILogger<StudentGradeService> logger)
    {
        _context = context;
        _logger = logger;
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<ResultModel<StudentGradesResponseDto>> GetMyGradesAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "User not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Get all groups student belongs to
            var groupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            // Get all projects from these groups
            var projects = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                        .ThenInclude(c => c!.Semester)
                .Where(p => groupIds.Contains(p.GroupId ?? 0))
                .ToListAsync();

            var projectGrades = new List<StudentProjectGradeDto>();

            foreach (var project in projects)
            {
                var projectGrade = await BuildProjectGradeDto(project);
                projectGrades.Add(projectGrade);
            }

            var response = new StudentGradesResponseDto
            {
                StudentId = userId,
                StudentName = user.FullName,
                Email = user.Email,
                Projects = projectGrades
            };

            return new ResultModel<StudentGradesResponseDto>
            {
                IsSuccess = true,
                Message = "Grades retrieved successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student grades");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting grades: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<StudentProjectGradeDto>> GetProjectGradesAsync(int projectId, int userId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                        .ThenInclude(c => c!.Semester)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Verify user is in project group
            var isMember = project.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not a member of this project",
                    StatusCodes.Status403Forbidden
                );
            }

            var projectGrade = await BuildProjectGradeDto(project);

            return new ResultModel<StudentProjectGradeDto>
            {
                IsSuccess = true,
                Message = "Project grades retrieved successfully",
                Data = projectGrade,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project grades");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting project grades: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ProjectFeedbackResponseDto>> GetProjectFeedbackAsync(int projectId, int userId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Verify user is in project group
            var isMember = project.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not a member of this project",
                    StatusCodes.Status403Forbidden
                );
            }

            // Get proposal feedback
            ProposalFeedbackDto? proposalFeedback = null;
            var latestApproval = await _context.ProjectApprovalHistories
                .Include(h => h.Reviewer)
                .Where(h => h.Submission!.ProjectId == projectId)
                .OrderByDescending(h => h.ActedAt)
                .FirstOrDefaultAsync();

            if (latestApproval != null)
            {
                proposalFeedback = new ProposalFeedbackDto
                {
                    Status = latestApproval.Action ?? "Pending",
                    ReviewedBy = latestApproval.Reviewer?.FullName,
                    ReviewedAt = latestApproval.ActedAt,
                    Comment = latestApproval.Comment
                };
            }

            // Get milestone feedback
            var evaluations = await _context.MilestoneEvaluations
                .Include(e => e.Instructor)
                .Include(e => e.MilestoneDef)
                .Where(e => e.ProjectId == projectId)
                .ToListAsync();

            var milestoneFeedback = evaluations.Select(e => new MilestoneFeedbackDto
            {
                MilestoneId = e.MilestoneDefId,
                MilestoneTitle = e.MilestoneDef?.Title,
                Grade = e.Score,
                Feedback = e.Feedback,
                GradedBy = e.Instructor?.FullName,
                GradedAt = e.EvaluatedAt
            }).ToList();

            var response = new ProjectFeedbackResponseDto
            {
                ProjectId = projectId,
                ProjectTitle = project.Title,
                ProjectStatus = project.Status,
                ProposalFeedback = proposalFeedback,
                MilestoneFeedback = milestoneFeedback
            };

            return new ResultModel<ProjectFeedbackResponseDto>
            {
                IsSuccess = true,
                Message = "Feedback retrieved successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project feedback");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting feedback: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ProjectOverallGradeDto>> GetProjectOverallGradeAsync(int projectId, int userId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Verify user is in project group
            var isMember = project.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not a member of this project",
                    StatusCodes.Status403Forbidden
                );
            }

            // Get all milestones
            var milestones = await _context.ProjectMilestones
                .Where(m => m.ProjectId == projectId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Get all evaluations
            var evaluations = await _context.MilestoneEvaluations
                .Include(e => e.MilestoneDef)
                .Where(e => e.ProjectId == projectId)
                .ToListAsync();

            var contributions = new List<MilestoneGradeContributionDto>();
            decimal totalWeightedScore = 0;
            decimal totalWeight = 0;
            int gradedCount = 0;

            foreach (var milestone in milestones)
            {
                var evaluation = evaluations.FirstOrDefault(e => e.MilestoneDefId == milestone.MilestoneId);
                var weight = milestone.Weight ?? 0;
                var grade = evaluation?.Score ?? 0;
                var weightedScore = (grade * weight) / 100;

                if (evaluation != null)
                {
                    totalWeightedScore += weightedScore;
                    totalWeight += weight;
                    gradedCount++;
                }

                contributions.Add(new MilestoneGradeContributionDto
                {
                    MilestoneTitle = milestone.Title,
                    Weight = weight,
                    Grade = evaluation?.Score,
                    WeightedScore = weightedScore,
                    IsGraded = evaluation != null
                });
            }

            decimal? overallGrade = totalWeight > 0 ? (totalWeightedScore / totalWeight) * 100 : null;
            bool isComplete = gradedCount == milestones.Count && milestones.Count > 0;

            var response = new ProjectOverallGradeDto
            {
                ProjectId = projectId,
                ProjectTitle = project.Title,
                OverallGrade = overallGrade,
                CalculationMethod = "WeightedAverage",
                MilestoneContributions = contributions,
                IsComplete = isComplete,
                TotalMilestones = milestones.Count,
                GradedMilestones = gradedCount
            };

            return new ResultModel<ProjectOverallGradeDto>
            {
                IsSuccess = true,
                Message = "Overall grade calculated successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating overall grade");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error calculating grade: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ClassGradesReportDto>> GetClassGradesAsync(int classId, int? instructorId = null)
    {
        try
        {
            var classEntity = await _context.Classes
                .Include(c => c.Semester)
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (classEntity == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Class not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Verify instructor access if provided
            if (instructorId.HasValue && classEntity.InstructorId != instructorId.Value)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You do not have access to this class",
                    StatusCodes.Status403Forbidden
                );
            }

            // Get all students enrolled in the class
            var enrolledStudents = await _context.ClassEnrollments
                .Include(ce => ce.Student)
                .Where(ce => ce.ClassId == classId)
                .Where(ce => ce.Student != null)
                .Select(ce => ce.Student!)
                .ToListAsync();

            // Get all groups in the class
            var groupsInClass = await _context.Groups
                .Include(g => g.GroupMembers)
                    .ThenInclude(gm => gm.User)
                .Include(g => g.Projects)
                    .ThenInclude(p => p.FinalProjectSubmission)
                .Where(g => g.ClassId == classId)
                .ToListAsync();

            // Get all unique milestone names (from all projects in class)
            var milestoneNames = await _context.ProjectMilestones
                .Where(pm => pm.Project!.Group!.ClassId == classId)
                .Select(pm => pm.Title ?? "Unnamed Milestone")
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            var studentGrades = new List<StudentGradeReportDto>();

            foreach (var student in enrolledStudents)
            {
                // Find student's group in this class
                var studentGroup = groupsInClass.FirstOrDefault(g => 
                    g.GroupMembers != null && g.GroupMembers.Any(gm => gm.UserId == student.UserId));

                // Get student's project from group
                var studentProject = studentGroup?.Projects?.FirstOrDefault();

                var studentGrade = new StudentGradeReportDto
                {
                    StudentId = student.UserId,
                    StudentName = student.FullName,
                    Email = student.Email,
                    GroupId = studentGroup?.GroupId,
                    GroupName = studentGroup?.GroupName,
                    ProjectId = studentProject?.ProjectId,
                    ProjectTitle = studentProject?.Title,
                    Status = studentProject?.Status ?? "No Project"
                };

                // Get milestone grades if student has a project
                if (studentProject != null)
                {
                    var projectId = studentProject.ProjectId;
                    
                    // Get all evaluations for this project
                    var evaluations = await _context.MilestoneEvaluations
                        .Include(e => e.MilestoneDef)
                        .Where(e => e.ProjectId == projectId)
                        .ToListAsync();

                    // Get all milestones for this project
                    var projectMilestones = await _context.ProjectMilestones
                        .Where(pm => pm.ProjectId == projectId)
                        .ToListAsync();

                    // Map milestone grades
                    var milestoneGradesList = new List<decimal>();
                    foreach (var milestoneName in milestoneNames)
                    {
                        var milestone = projectMilestones.FirstOrDefault(pm => pm.Title == milestoneName);
                        if (milestone != null)
                        {
                            var evaluation = evaluations.FirstOrDefault(e => e.MilestoneDefId == milestone.MilestoneId);
                            studentGrade.MilestoneGrades[milestoneName] = evaluation?.Score;
                            if (evaluation?.Score != null)
                            {
                                milestoneGradesList.Add(evaluation.Score);
                            }
                        }
                        else
                        {
                            studentGrade.MilestoneGrades[milestoneName] = null;
                        }
                    }

                    // Calculate milestone average grade (simple average, not weighted)
                    studentGrade.MilestoneAverageGrade = milestoneGradesList.Any() 
                        ? milestoneGradesList.Average() 
                        : null;

                    // Get final submission grade (average from all graders)
                    var finalSubmission = await _context.FinalProjectSubmissions
                        .FirstOrDefaultAsync(fps => fps.ProjectId == projectId);
                    
                    studentGrade.FinalSubmissionGrade = finalSubmission?.Grade;

                    // Calculate overall grade (milestones + final submission)
                    decimal totalWeightedScore = 0;
                    decimal totalWeight = 0;

                    // Add milestone scores
                    foreach (var milestone in projectMilestones)
                    {
                        var evaluation = evaluations.FirstOrDefault(e => e.MilestoneDefId == milestone.MilestoneId);
                        if (evaluation != null && milestone.Weight.HasValue)
                        {
                            totalWeightedScore += (evaluation.Score * milestone.Weight.Value) / 100;
                            totalWeight += milestone.Weight.Value;
                        }
                    }

                    // Add final submission score if exists (weight is remaining percentage)
                    if (finalSubmission?.Grade != null)
                    {
                        decimal finalWeight = 100 - totalWeight;
                        if (finalWeight > 0)
                        {
                            totalWeightedScore += (finalSubmission.Grade.Value * finalWeight) / 100;
                            totalWeight += finalWeight;
                        }
                    }

                    studentGrade.OverallGrade = totalWeight > 0 ? (totalWeightedScore / totalWeight) * 100 : null;
                }
                else
                {
                    // No project - fill with nulls
                    foreach (var milestoneName in milestoneNames)
                    {
                        studentGrade.MilestoneGrades[milestoneName] = null;
                    }
                    studentGrade.MilestoneAverageGrade = null;
                    studentGrade.FinalSubmissionGrade = null;
                }

                studentGrades.Add(studentGrade);
            }

            // Sort by Group Name (nulls last) then by Student Name
            studentGrades = studentGrades
                .OrderBy(sg => string.IsNullOrEmpty(sg.GroupName) ? "ZZZZZ" : sg.GroupName)
                .ThenBy(sg => sg.StudentName)
                .ToList();

            var report = new ClassGradesReportDto
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                SemesterName = classEntity.Semester?.Name,
                InstructorName = classEntity.Instructor?.FullName,
                TotalStudents = enrolledStudents.Count,
                TotalGroups = groupsInClass.Count,
                StudentGrades = studentGrades,
                MilestoneNames = milestoneNames
            };

            return new ResultModel<ClassGradesReportDto>
            {
                IsSuccess = true,
                Message = "Class grades retrieved successfully",
                Data = report,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting class grades");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting class grades: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ExportClassGradesResponseDto>> ExportClassGradesToExcelAsync(
        int classId, 
        bool includeMilestoneDetails = true, 
        bool includeFeedback = false, 
        int? instructorId = null)
    {
        try
        {
            // Get class grades data
            var gradesResult = await GetClassGradesAsync(classId, instructorId);
            if (!gradesResult.IsSuccess || gradesResult.Data == null)
            {
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    "Failed to retrieve class grades",
                    StatusCodes.Status500InternalServerError
                );
            }

            var gradesData = gradesResult.Data;

            // Get all graders assigned to this class
            var classGraders = await _context.ClassGraders
                .Include(cg => cg.Instructor)
                .Where(cg => cg.ClassId == classId && cg.IsActive == true)
                .OrderBy(cg => cg.Instructor!.FullName)
                .ToListAsync();

            // Get all final submission grades from all graders with instructor info
            var allFinalSubmissionGrades = new Dictionary<int, List<(int InstructorId, string GraderName, string GraderEmail, decimal Grade)>>();
            
            foreach (var studentGrade in gradesData.StudentGrades.Where(sg => sg.ProjectId.HasValue))
            {
                var projectId = studentGrade.ProjectId!.Value;
                
                var finalSubmission = await _context.FinalProjectSubmissions
                    .FirstOrDefaultAsync(fps => fps.ProjectId == projectId);

                if (finalSubmission != null)
                {
                    var graderGrades = await _context.FinalSubmissionGrades
                        .Include(fsg => fsg.Instructor)
                        .Where(fsg => fsg.FinalSubmissionId == finalSubmission.FinalSubmissionId)
                        .Select(fsg => new
                        {
                            fsg.InstructorId,
                            GraderName = fsg.Instructor!.FullName ?? "Unknown",
                            GraderEmail = fsg.Instructor!.Email ?? "",
                            fsg.Grade
                        })
                        .ToListAsync();

                    allFinalSubmissionGrades[projectId] = graderGrades
                        .Select(g => (g.InstructorId, g.GraderName, g.GraderEmail, g.Grade))
                        .ToList();
                }
            }

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Class Grades");

            // Set up header
            int row = 1;
            int col = 1;

            // Title
            worksheet.Cells[row, col].Value = $"Class Grades Report: {gradesData.ClassName}";
            worksheet.Cells[row, col].Style.Font.Size = 16;
            worksheet.Cells[row, col].Style.Font.Bold = true;
            row++;

            // Class info
            worksheet.Cells[row, col].Value = $"Semester: {gradesData.SemesterName}";
            row++;
            worksheet.Cells[row, col].Value = $"Instructor: {gradesData.InstructorName}";
            row++;
            worksheet.Cells[row, col].Value = $"Total Students: {gradesData.TotalStudents}";
            row++;
            worksheet.Cells[row, col].Value = $"Total Groups: {gradesData.TotalGroups}";
            row++;
            
            // Show assigned graders
            if (classGraders.Any())
            {
                worksheet.Cells[row, col].Value = $"Assigned Graders: {string.Join(", ", classGraders.Select(cg => cg.Instructor?.FullName))}";
                row++;
            }
            
            worksheet.Cells[row, col].Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            row++;
            row++; // Empty row

            // Column headers
            int headerRow = row;
            col = 1;
            
            worksheet.Cells[headerRow, col++].Value = "Student ID";
            worksheet.Cells[headerRow, col++].Value = "Student Name";
            worksheet.Cells[headerRow, col++].Value = "Email";
            worksheet.Cells[headerRow, col++].Value = "Group Name";
            worksheet.Cells[headerRow, col++].Value = "Project Title";

            if (includeMilestoneDetails)
            {
                foreach (var milestoneName in gradesData.MilestoneNames)
                {
                    worksheet.Cells[headerRow, col++].Value = milestoneName;
                }
            }

            // Add Milestone Average column
            worksheet.Cells[headerRow, col++].Value = "Milestone Average";

            // Add Final Submission column (from Final_Project_Submissions table)
            worksheet.Cells[headerRow, col++].Value = "Final Submission";

            // Add columns for each grader (3 columns per grader: Name, Email, Grade)
            int graderIndex = 1;
            foreach (var grader in classGraders)
            {
                worksheet.Cells[headerRow, col++].Value = $"Grader {graderIndex} Name";
                worksheet.Cells[headerRow, col++].Value = $"Grader {graderIndex} Email";
                worksheet.Cells[headerRow, col++].Value = $"Grader {graderIndex} Grade";
                graderIndex++;
            }

            // Style header row
            using (var range = worksheet.Cells[headerRow, 1, headerRow, col - 1])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.WrapText = true;
            }

            // Data rows
            row++;
            foreach (var studentGrade in gradesData.StudentGrades)
            {
                col = 1;
                worksheet.Cells[row, col++].Value = studentGrade.StudentId;
                worksheet.Cells[row, col++].Value = studentGrade.StudentName;
                worksheet.Cells[row, col++].Value = studentGrade.Email;
                worksheet.Cells[row, col++].Value = studentGrade.GroupName ?? "No Group";
                worksheet.Cells[row, col++].Value = studentGrade.ProjectTitle ?? "No Project";

                if (includeMilestoneDetails)
                {
                    foreach (var milestoneName in gradesData.MilestoneNames)
                    {
                        if (studentGrade.MilestoneGrades.TryGetValue(milestoneName, out var grade) && grade.HasValue)
                        {
                            worksheet.Cells[row, col].Value = grade.Value;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                            
                            // Color coding for grades
                            worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            if (grade.Value >= 80)
                            {
                                worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                            }
                            else if (grade.Value >= 50)
                            {
                                worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                            }
                            else
                            {
                                worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                            }
                        }
                        else
                        {
                            worksheet.Cells[row, col].Value = "N/A";
                        }
                        col++;
                    }
                }

                // Add Milestone Average
                if (studentGrade.MilestoneAverageGrade.HasValue)
                {
                    worksheet.Cells[row, col].Value = studentGrade.MilestoneAverageGrade.Value;
                    worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                    
                    // Color coding for milestone average
                    worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    if (studentGrade.MilestoneAverageGrade.Value >= 80)
                    {
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                    }
                    else if (studentGrade.MilestoneAverageGrade.Value >= 50)
                    {
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                    }
                    else
                    {
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                    }
                }
                else
                {
                    worksheet.Cells[row, col].Value = "N/A";
                }
                col++;

                // Add Final Submission grade (from Final_Project_Submissions table)
                if (studentGrade.FinalSubmissionGrade.HasValue)
                {
                    worksheet.Cells[row, col].Value = studentGrade.FinalSubmissionGrade.Value;
                    worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                    
                    // Color coding for final submission
                    worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    if (studentGrade.FinalSubmissionGrade.Value >= 80)
                    {
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                    }
                    else if (studentGrade.FinalSubmissionGrade.Value >= 50)
                    {
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                    }
                    else
                    {
                        worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                    }
                }
                else
                {
                    worksheet.Cells[row, col].Value = "N/A";
                }
                col++;

                // Get individual grader grades for this student's project
                List<(int InstructorId, string GraderName, string GraderEmail, decimal Grade)> graderGrades = new List<(int, string, string, decimal)>();
                if (studentGrade.ProjectId.HasValue && allFinalSubmissionGrades.ContainsKey(studentGrade.ProjectId.Value))
                {
                    graderGrades = allFinalSubmissionGrades[studentGrade.ProjectId.Value];
                }

                // Add individual grader columns (Name, Email, Grade for each grader)
                foreach (var classGrader in classGraders)
                {
                    var graderInfo = graderGrades.FirstOrDefault(gg => gg.InstructorId == classGrader.InstructorId);
                    
                    if (graderInfo != default)
                    {
                        // Grader Name
                        worksheet.Cells[row, col++].Value = graderInfo.GraderName;
                        
                        // Grader Email
                        worksheet.Cells[row, col++].Value = graderInfo.GraderEmail;
                        
                        // Grader Grade
                        worksheet.Cells[row, col].Value = graderInfo.Grade;
                        worksheet.Cells[row, col].Style.Numberformat.Format = "0.00";
                        
                        // Color coding
                        worksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        if (graderInfo.Grade >= 80)
                        {
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                        }
                        else if (graderInfo.Grade >= 50)
                        {
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                        }
                        else
                        {
                            worksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                        }
                        col++;
                    }
                    else
                    {
                        // No grade from this grader
                        worksheet.Cells[row, col++].Value = "N/A";
                        worksheet.Cells[row, col++].Value = "N/A";
                        worksheet.Cells[row, col++].Value = "N/A";
                    }
                }

                row++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Add borders to all cells
            using (var range = worksheet.Cells[headerRow, 1, row - 1, col - 1])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            var fileContent = package.GetAsByteArray();
            var fileName = $"ClassGrades_{gradesData.ClassName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return new ResultModel<ExportClassGradesResponseDto>
            {
                IsSuccess = true,
                Message = "Class grades exported successfully",
                Data = new ExportClassGradesResponseDto
                {
                    FileName = fileName,
                    FileContent = fileContent,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    TotalStudents = gradesData.TotalStudents,
                    TotalProjects = gradesData.StudentGrades.Count(sg => sg.ProjectId.HasValue),
                    GeneratedAt = DateTime.Now
                },
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting class grades to Excel");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error exporting grades: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    // Helper method to build project grade DTO
    private async Task<StudentProjectGradeDto> BuildProjectGradeDto(BusinessObjects.Models.Project project)
    {
        // Get all milestones for project
        var milestones = await _context.ProjectMilestones
            .Where(m => m.ProjectId == project.ProjectId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        // Get all evaluations
        var evaluations = await _context.MilestoneEvaluations
            .Include(e => e.Instructor)
            .Where(e => e.ProjectId == project.ProjectId)
            .ToListAsync();

        // Get submissions
        var submissions = await _context.MilestoneSubmissions
            .Where(s => s.ProjectId == project.ProjectId)
            .ToListAsync();

        var milestoneGrades = new List<StudentMilestoneGradeDto>();
        decimal totalWeightedScore = 0;
        decimal totalWeight = 0;
        var breakdownScores = new Dictionary<string, decimal>();

        foreach (var milestone in milestones)
        {
            var evaluation = evaluations.FirstOrDefault(e => e.MilestoneDefId == milestone.MilestoneId);
            var submission = submissions.FirstOrDefault(s => s.MilestoneDefId == milestone.MilestoneId);

            var status = evaluation != null ? "Graded" : 
                         submission != null ? "Submitted" : "NotSubmitted";

            var milestoneGrade = new StudentMilestoneGradeDto
            {
                MilestoneId = milestone.MilestoneId,
                MilestoneTitle = milestone.Title,
                Weight = milestone.Weight,
                Grade = evaluation?.Score,
                Feedback = evaluation?.Feedback,
                GradedAt = evaluation?.EvaluatedAt,
                GradedBy = evaluation?.Instructor?.FullName,
                Status = status
            };

            milestoneGrades.Add(milestoneGrade);

            if (evaluation != null && milestone.Weight.HasValue)
            {
                var weightedScore = (evaluation.Score * milestone.Weight.Value) / 100;
                totalWeightedScore += weightedScore;
                totalWeight += milestone.Weight.Value;
                breakdownScores[$"milestone{milestone.MilestoneId}"] = weightedScore;
            }
        }

        decimal? overallGrade = totalWeight > 0 ? (totalWeightedScore / totalWeight) * 100 : null;
        decimal? projectedGrade = totalWeight < 100 && totalWeight > 0
            ? (totalWeightedScore / totalWeight) * 100
            : overallGrade;

        return new StudentProjectGradeDto
        {
            ProjectId = project.ProjectId,
            ProjectTitle = project.Title,
            ClassId = project.Group?.ClassId ?? 0,
            ClassName = project.Group?.Class?.ClassName,
            SemesterName = project.Group?.Class?.Semester?.Name,
            GroupId = project.GroupId ?? 0,
            GroupName = project.Group?.GroupName,
            ProjectStatus = DetermineProjectStatus(milestoneGrades, project.Status),
            OverallGrade = overallGrade,
            Milestones = milestoneGrades,
            GradeBreakdown = new WeightedGradeBreakdownDto
            {
                MilestoneScores = breakdownScores,
                TotalWeightedScore = totalWeightedScore,
                TotalWeight = totalWeight,
                ProjectedFinalGrade = projectedGrade
            }
        };
    }

    private string DetermineProjectStatus(List<StudentMilestoneGradeDto> milestones, string? projectStatus)
    {
        if (projectStatus == "Rejected")
            return "Failed";

        var allGraded = milestones.All(m => m.Status == "Graded");
        if (allGraded && milestones.Any())
        {
            var avgGrade = milestones.Where(m => m.Grade.HasValue).Average(m => m.Grade);
            return avgGrade >= 50 ? "Completed" : "Failed";
        }

        return "InProgress";
    }
}
