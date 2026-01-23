using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.ClassGrader;

public class ClassGraderService : IClassGraderService
{
    private readonly IotShowroomContext _context;
    private readonly ILogger<ClassGraderService> _logger;

    public ClassGraderService(
        IotShowroomContext context,
        ILogger<ClassGraderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<List<GradingClassDto>>> GetGradingClassesAsync(int instructorId)
    {
        try
        {
            _logger.LogInformation("Getting grading classes for instructor {InstructorId}", instructorId);

            var gradingClasses = await _context.ClassGraders
                .Where(cg => cg.InstructorId == instructorId && cg.IsActive)
                .Include(cg => cg.Class)
                    .ThenInclude(c => c.Semester)
                .Include(cg => cg.Class)
                    .ThenInclude(c => c.Instructor)
                .Include(cg => cg.Class)
                    .ThenInclude(c => c.Groups)
                        .ThenInclude(g => g.Projects)
                            .ThenInclude(p => p.FinalProjectSubmission)
                                .ThenInclude(fps => fps!.FinalSubmissionGrades)
                .ToListAsync();

            var result = new List<GradingClassDto>();

            foreach (var grader in gradingClasses)
            {
                var classEntity = grader.Class;
                
                // Get all projects in this class
                var allProjects = classEntity.Groups
                    .SelectMany(g => g.Projects)
                    .ToList();

                // Fixed: Use case-insensitive comparison in memory instead of in query
                var approvedProjects = allProjects
                    .Where(p => p.Status != null && 
                               string.Equals(p.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var projectsWithFinalSubmission = approvedProjects
                    .Where(p => p.FinalProjectSubmission != null)
                    .ToList();

                var projectsIGraded = projectsWithFinalSubmission
                    .Where(p => p.FinalProjectSubmission!.FinalSubmissionGrades
                        .Any(fsg => fsg.InstructorId == instructorId))
                    .Count();

                var projectsPendingMyGrade = projectsWithFinalSubmission
                    .Where(p => !p.FinalProjectSubmission!.FinalSubmissionGrades
                        .Any(fsg => fsg.InstructorId == instructorId))
                    .Count();

                result.Add(new GradingClassDto
                {
                    ClassId = classEntity.ClassId,
                    ClassName = classEntity.ClassName,
                    Description = classEntity.Description,
                    SemesterId = classEntity.SemesterId,
                    SemesterName = classEntity.Semester?.Name,
                    MainInstructorId = classEntity.InstructorId,
                    MainInstructorName = classEntity.Instructor?.FullName,
                    AssignedAt = grader.AssignedAt,
                    IsActive = grader.IsActive,
                    TotalProjects = allProjects.Count,
                    ApprovedProjects = approvedProjects.Count,
                    ProjectsWithFinalSubmission = projectsWithFinalSubmission.Count,
                    ProjectsIGraded = projectsIGraded,
                    ProjectsPendingMyGrade = projectsPendingMyGrade
                });
            }

            return new ResultModel<List<GradingClassDto>>
            {
                IsSuccess = true,
                Message = $"Found {result.Count} grading class(es)",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grading classes for instructor {InstructorId}", instructorId);
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting grading classes: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<List<ApprovedProjectForGradingDto>>> GetApprovedProjectsForGradingAsync(
        int classId, 
        int instructorId)
    {
        try
        {
            _logger.LogInformation("Getting approved projects in class {ClassId} for instructor {InstructorId}", 
                classId, instructorId);

            // Verify instructor is assigned to grade this class
            var isAssigned = await _context.ClassGraders
                .AnyAsync(cg => cg.ClassId == classId && 
                               cg.InstructorId == instructorId && 
                               cg.IsActive);

            if (!isAssigned)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not assigned to grade projects in this class",
                    StatusCodes.Status403Forbidden
                );
            }

            // Get all approved projects in this class
            // Fixed: Remove StringComparison parameter for EF Core translation
            var projects = await _context.Projects
                .Where(p => p.Group!.ClassId == classId && 
                           p.Status != null && 
                           p.Status.ToLower() == "approved")
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                .Include(p => p.FinalProjectSubmission)
                    .ThenInclude(fps => fps!.FinalSubmissionGrades)
                        .ThenInclude(fsg => fsg.Instructor)
                .OrderBy(p => p.Group!.GroupName)
                .ToListAsync();

            var result = projects.Select(p =>
            {
                var finalSubmission = p.FinalProjectSubmission;
                var myGrade = finalSubmission?.FinalSubmissionGrades
                    .FirstOrDefault(fsg => fsg.InstructorId == instructorId);

                var totalGrades = finalSubmission?.FinalSubmissionGrades.Count ?? 0;
                
                // Calculate average from grader grades dynamically
                decimal? averageGraderGrade = null;
                if (finalSubmission?.FinalSubmissionGrades?.Any() == true)
                {
                    averageGraderGrade = finalSubmission.FinalSubmissionGrades.Average(fsg => fsg.Grade);
                }
                
                string gradingStatus;
                
                if (finalSubmission == null)
                {
                    gradingStatus = "No Submission";
                }
                else if (totalGrades == 0)
                {
                    gradingStatus = "Not Graded";
                }
                else if (myGrade == null)
                {
                    gradingStatus = "Pending My Grade";
                }
                else
                {
                    gradingStatus = "Graded";
                }

                return new ApprovedProjectForGradingDto
                {
                    ProjectId = p.ProjectId,
                    Title = p.Title,
                    Description = p.Description,
                    Component = p.Component,
                    GroupId = p.Group!.GroupId,
                    GroupName = p.Group.GroupName,
                    ClassId = p.Group.ClassId,
                    ClassName = p.Group.Class?.ClassName,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    HasFinalSubmission = finalSubmission != null,
                    FinalSubmissionId = finalSubmission?.FinalSubmissionId,
                    SubmittedAt = finalSubmission?.SubmittedAt,
                    HasMyGrade = myGrade != null,
                    MyGrade = myGrade?.Grade,
                    AverageGrade = averageGraderGrade,
                    TotalGradesCount = totalGrades,
                    GradingStatus = gradingStatus
                };
            }).ToList();

            return new ResultModel<List<ApprovedProjectForGradingDto>>
            {
                IsSuccess = true,
                Message = $"Found {result.Count} approved project(s)",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approved projects for class {ClassId}", classId);
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting approved projects: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<GraderFinalSubmissionDetailDto>> GetFinalSubmissionForGradingAsync(
        int finalSubmissionId, 
        int instructorId)
    {
        try
        {
            _logger.LogInformation("Getting final submission {SubmissionId} for grading by instructor {InstructorId}", 
                finalSubmissionId, instructorId);

            var submission = await _context.FinalProjectSubmissions
                .Where(fps => fps.FinalSubmissionId == finalSubmissionId)
                .Include(fps => fps.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.Class)
                .Include(fps => fps.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.GroupMembers)
                            .ThenInclude(gm => gm.User)
                .Include(fps => fps.FinalSubmissionGrades)
                    .ThenInclude(fsg => fsg.Instructor)
                .FirstOrDefaultAsync();

            if (submission == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Final submission not found",
                    StatusCodes.Status404NotFound
                );
            }

            var classId = submission.Project.Group!.ClassId;

            // Verify instructor is assigned to grade this class
            var isAssigned = await _context.ClassGraders
                .AnyAsync(cg => cg.ClassId == classId && 
                               cg.InstructorId == instructorId && 
                               cg.IsActive);

            if (!isAssigned)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not assigned to grade projects in this class",
                    StatusCodes.Status403Forbidden
                );
            }

            var myGrade = submission.FinalSubmissionGrades
                .FirstOrDefault(fsg => fsg.InstructorId == instructorId);

            // Calculate average from grader grades (NOT from submission.Grade)
            // submission.Grade is reserved for main class instructor
            decimal? averageGraderGrade = null;
            if (submission.FinalSubmissionGrades.Any())
            {
                averageGraderGrade = submission.FinalSubmissionGrades.Average(fsg => fsg.Grade);
            }

            var result = new GraderFinalSubmissionDetailDto
            {
                FinalSubmissionId = submission.FinalSubmissionId,
                ProjectId = submission.ProjectId,
                ProjectTitle = submission.Project.Title,
                GroupId = submission.Project.Group.GroupId,
                GroupName = submission.Project.Group.GroupName,
                GroupMembers = submission.Project.Group.GroupMembers
                    .Select(gm => gm.User?.FullName ?? "Unknown")
                    .ToList(),
                ClassId = classId,
                ClassName = submission.Project.Group.Class?.ClassName,
                FinalReportUrl = submission.FinalReportUrl,
                PresentationUrl = submission.PresentationUrl,
                SourceCodeUrl = submission.SourceCodeUrl,
                VideoDemoUrl = submission.VideoDemoUrl,
                RepositoryUrl = submission.RepositoryUrl,
                SubmissionNotes = submission.SubmissionNotes,
                SubmittedAt = submission.SubmittedAt,
                LastUpdatedAt = submission.LastUpdatedAt,
                AverageGrade = averageGraderGrade,
                AllGrades = submission.FinalSubmissionGrades
                    .Select(fsg => new InstructorGradeDto
                    {
                        InstructorId = fsg.InstructorId,
                        InstructorName = fsg.Instructor.FullName,
                        Grade = fsg.Grade,
                        Feedback = fsg.Feedback,
                        GradedAt = fsg.GradedAt
                    })
                    .OrderByDescending(g => g.GradedAt)
                    .ToList(),
                HasMyGrade = myGrade != null,
                MyGrade = myGrade?.Grade,
                MyFeedback = myGrade?.Feedback,
                MyGradedAt = myGrade?.GradedAt
            };

            return new ResultModel<GraderFinalSubmissionDetailDto>
            {
                IsSuccess = true,
                Message = "Final submission retrieved successfully",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting final submission {SubmissionId} for grading", finalSubmissionId);
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting final submission: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<GraderFinalProjectGradeResponseDto>> GradeFinalSubmissionAsync(
        int finalSubmissionId, 
        GraderFinalProjectGradeRequestDto request, 
        int instructorId)
    {
        try
        {
            _logger.LogInformation("Instructor {InstructorId} grading final submission {SubmissionId}", 
                instructorId, finalSubmissionId);

            var submission = await _context.FinalProjectSubmissions
                .Where(fps => fps.FinalSubmissionId == finalSubmissionId)
                .Include(fps => fps.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.Class)
                .Include(fps => fps.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.GroupMembers)
                .Include(fps => fps.FinalSubmissionGrades)
                    .ThenInclude(fsg => fsg.Instructor)
                .FirstOrDefaultAsync();

            if (submission == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Final submission not found",
                    StatusCodes.Status404NotFound
                );
            }

            var classId = submission.Project.Group!.ClassId;

            // Verify instructor is assigned to grade this class
            var isAssigned = await _context.ClassGraders
                .AnyAsync(cg => cg.ClassId == classId && 
                               cg.InstructorId == instructorId && 
                               cg.IsActive);

            if (!isAssigned)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not assigned to grade projects in this class",
                    StatusCodes.Status403Forbidden
                );
            }

            // Check if instructor already graded this submission
            var existingGrade = await _context.FinalSubmissionGrades
                .FirstOrDefaultAsync(fsg => fsg.FinalSubmissionId == finalSubmissionId && 
                                            fsg.InstructorId == instructorId);

            if (existingGrade != null)
            {
                // Update existing grade
                existingGrade.Grade = request.Grade;
                existingGrade.Feedback = request.Feedback;
                existingGrade.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Updated existing grade for instructor {InstructorId} on submission {SubmissionId}", 
                    instructorId, finalSubmissionId);
            }
            else
            {
                // Create new grade
                var newGrade = new FinalSubmissionGrade
                {
                    FinalSubmissionId = finalSubmissionId,
                    InstructorId = instructorId,
                    Grade = request.Grade,
                    Feedback = request.Feedback,
                    GradedAt = DateTime.UtcNow
                };
                _context.FinalSubmissionGrades.Add(newGrade);
                
                _logger.LogInformation("Created new grade for instructor {InstructorId} on submission {SubmissionId}", 
                    instructorId, finalSubmissionId);
            }

            // FIXED: Do NOT update submission.Grade field
            // The Grade field in Final_Project_Submissions should only be set by the main class instructor
            // Grader grades are stored separately in Final_Submission_Grades table
            
            await _context.SaveChangesAsync();

            // Reload grades to get the latest data
            await _context.Entry(submission).Collection(s => s.FinalSubmissionGrades).LoadAsync();

            // Load instructor info for all grades
            foreach (var grade in submission.FinalSubmissionGrades)
            {
                await _context.Entry(grade).Reference(g => g.Instructor).LoadAsync();
            }

            var myGrade = submission.FinalSubmissionGrades
                .First(fsg => fsg.InstructorId == instructorId);

            // Calculate average from grader grades (for display purposes only)
            // This average is NOT stored in submission.Grade
            decimal? averageGraderGrade = null;
            if (submission.FinalSubmissionGrades.Any())
            {
                averageGraderGrade = submission.FinalSubmissionGrades.Average(fsg => fsg.Grade);
                _logger.LogInformation("Calculated average grader grade: {AverageGrade} from {GraderCount} grader(s)", 
                    averageGraderGrade, submission.FinalSubmissionGrades.Count);
            }

            // Send notification to group members
            var groupMembers = submission.Project.Group.GroupMembers.ToList();
            foreach (var member in groupMembers)
            {
                // Create Data JSON for notification
                var notificationData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    classId = submission.Project.Group.ClassId,
                    groupId = submission.Project.GroupId,
                    projectId = submission.ProjectId,
                    finalSubmissionId = submission.FinalSubmissionId,
                    gradeId = myGrade.GradeId,
                    instructorId = instructorId
                });

                var notification = new BusinessObjects.Models.Notification
                {
                    UserId = member.UserId,
                    Title = "Final Project Graded by Instructor",
                    Message = averageGraderGrade.HasValue 
                        ? $"Your final project '{submission.Project.Title}' has been graded by {myGrade.Instructor.FullName}. Current grader average: {averageGraderGrade.Value:F2}/100"
                        : $"Your final project '{submission.Project.Title}' has been graded by {myGrade.Instructor.FullName}.",
                    Type = "grader_grade_submitted",
                    Data = notificationData,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            var result = new GraderFinalProjectGradeResponseDto
            {
                GradeId = myGrade.GradeId,
                FinalSubmissionId = finalSubmissionId,
                InstructorId = instructorId,
                InstructorName = myGrade.Instructor.FullName,
                Grade = myGrade.Grade,
                Feedback = myGrade.Feedback,
                GradedAt = myGrade.GradedAt,
                AverageGrade = averageGraderGrade,
                TotalGradesCount = submission.FinalSubmissionGrades.Count,
                AllGrades = submission.FinalSubmissionGrades
                    .Select(fsg => new InstructorGradeDto
                    {
                        InstructorId = fsg.InstructorId,
                        InstructorName = fsg.Instructor.FullName,
                        Grade = fsg.Grade,
                        Feedback = fsg.Feedback,
                        GradedAt = fsg.GradedAt
                    })
                    .OrderByDescending(g => g.GradedAt)
                    .ToList()
            };

            return new ResultModel<GraderFinalProjectGradeResponseDto>
            {
                IsSuccess = true,
                Message = existingGrade != null ? "Grade updated successfully" : "Grade submitted successfully",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error grading final submission {SubmissionId}", finalSubmissionId);
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error grading final submission: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }
}
