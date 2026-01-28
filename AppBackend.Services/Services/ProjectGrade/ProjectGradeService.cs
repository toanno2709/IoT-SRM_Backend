using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Constants;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.ProjectGrade;

public class ProjectGradeService : IProjectGradeService
{
    private readonly IotShowroomContext _context;
    private readonly ILogger<ProjectGradeService> _logger;

    public ProjectGradeService(
        IotShowroomContext context,
        ILogger<ProjectGradeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<ProjectGradersResponseDto>> GetProjectGradersAsync(int projectId, int studentId)
    {
        try
        {
            _logger.LogInformation("Starting GetProjectGradersAsync for project {ProjectId}, student {StudentId}", 
                projectId, studentId);

            // Get project with all necessary relationships
            var project = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                        .ThenInclude(c => c!.ClassGraders)
                            .ThenInclude(cg => cg.Instructor)
                .Include(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
                .Include(p => p.FinalProjectSubmission)
                    .ThenInclude(fps => fps!.FinalSubmissionGrades)
                        .ThenInclude(fsg => fsg.Instructor)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", projectId);
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );
            }

            _logger.LogInformation("Project found: {ProjectTitle}, GroupId: {GroupId}", 
                project.Title, project.GroupId);

            // Defensive check: Group must exist
            if (project.Group == null)
            {
                _logger.LogError("Project {ProjectId} has no associated group", projectId);
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    "Project is not associated with a group",
                    StatusCodes.Status500InternalServerError
                );
            }

            // Check if student is a member of this project's group
            var groupMembers = project.Group.GroupMembers?.ToList() ?? new List<BusinessObjects.Models.GroupMember>();
            var isMember = groupMembers.Any(gm => gm.UserId == studentId);

            _logger.LogInformation("Student {StudentId} membership check: {IsMember}, Total members: {MemberCount}", 
                studentId, isMember, groupMembers.Count);

            if (!isMember)
            {
                _logger.LogWarning("Student {StudentId} is not a member of group {GroupId}", 
                    studentId, project.Group.GroupId);
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not a member of this project's group",
                    StatusCodes.Status403Forbidden
                );
            }

            // Get class ID
            var classId = project.Group.ClassId;
            if (classId == 0)
            {
                _logger.LogError("Group {GroupId} has no associated class", project.Group.GroupId);
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    "Project's group is not associated with a class",
                    StatusCodes.Status500InternalServerError
                );
            }

            _logger.LogInformation("Getting graders for class {ClassId}", classId);

            // Get all assigned graders for this class
            var assignedGraders = await _context.ClassGraders
                .Include(cg => cg.Instructor)
                .Where(cg => cg.ClassId == classId)
                .ToListAsync();

            _logger.LogInformation("Found {GraderCount} assigned graders for class {ClassId}", 
                assignedGraders.Count, classId);

            // Get final submission and grades (defensive)
            var finalSubmission = project.FinalProjectSubmission;
            var submissionGrades = finalSubmission?.FinalSubmissionGrades?.ToList() 
                ?? new List<BusinessObjects.Models.FinalSubmissionGrade>();

            _logger.LogInformation("Final submission exists: {HasSubmission}, Grades count: {GradeCount}", 
                finalSubmission != null, submissionGrades.Count);

            // Build grader list
            var graderDtos = new List<GraderGradeDto>();

            foreach (var grader in assignedGraders)
            {
                if (grader.Instructor == null)
                {
                    _logger.LogWarning("ClassGrader {GraderId} has null Instructor navigation property", 
                        grader.InstructorId);
                    continue; // Skip this grader if instructor data is missing
                }

                var gradeRecord = submissionGrades.FirstOrDefault(sg => sg.InstructorId == grader.InstructorId);

                graderDtos.Add(new GraderGradeDto
                {
                    GraderId = grader.InstructorId,
                    GraderName = grader.Instructor.FullName ?? "Unknown",
                    GraderEmail = grader.Instructor.Email ?? "N/A",
                    Grade = gradeRecord?.Grade,
                    Feedback = gradeRecord?.Feedback,
                    GradedAt = gradeRecord?.GradedAt,
                    Status = gradeRecord != null ? "Graded" : "NotGraded"
                });
            }

            // Sort graders: graded first, then by name
            graderDtos = graderDtos
                .OrderByDescending(g => g.Status == "Graded")
                .ThenBy(g => g.GraderName)
                .ToList();

            _logger.LogInformation("Built {GraderDtoCount} grader DTOs", graderDtos.Count);

            // Build final submission info
            ProjectFinalSubmissionInfoDto? finalSubmissionInfo = null;
            if (finalSubmission != null)
            {
                finalSubmissionInfo = new ProjectFinalSubmissionInfoDto
                {
                    FinalSubmissionId = finalSubmission.FinalSubmissionId,
                    SubmittedAt = finalSubmission.SubmittedAt,
                    HasSubmission = true,
                    IsGraded = submissionGrades.Any()
                };
            }

            // Build response
            var response = new ProjectGradersResponseDto
            {
                ProjectId = project.ProjectId,
                ProjectTitle = project.Title ?? "Untitled Project",
                GroupId = project.GroupId ?? 0,
                GroupName = project.Group.GroupName ?? "Unnamed Group",
                ProjectStatus = project.Status ?? "Unknown",
                // FIXED: Calculate average from grader grades dynamically
                // submission.Grade is for main instructor only
                AverageGrade = submissionGrades.Any() ? submissionGrades.Average(sg => sg.Grade) : null,
                TotalGradersAssigned = assignedGraders.Count,
                GradersCompleted = submissionGrades.Count,
                FinalSubmission = finalSubmissionInfo,
                Graders = graderDtos
            };

            _logger.LogInformation(
                "Successfully retrieved grader information for project {ProjectId}. Total graders: {TotalGraders}, Completed: {CompletedGraders}, Average: {Average}",
                projectId, response.TotalGradersAssigned, response.GradersCompleted, response.AverageGrade);

            return new ResultModel<ProjectGradersResponseDto>
            {
                IsSuccess = true,
                Message = "Project graders retrieved successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "AppException in GetProjectGradersAsync for project {ProjectId}: {Message}", 
                projectId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetProjectGradersAsync for project {ProjectId}", projectId);
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"An unexpected error occurred while retrieving project graders: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }
}
