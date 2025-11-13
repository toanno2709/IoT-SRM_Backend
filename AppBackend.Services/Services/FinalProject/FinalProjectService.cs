using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.FinalProjectRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.FinalProject;

public class FinalProjectService : IFinalProjectService
{
    private readonly IFinalProjectRepository _finalProjectRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IotShowroomContext _context;
    private readonly ILogger<FinalProjectService> _logger;

    public FinalProjectService(
        IFinalProjectRepository finalProjectRepository,
        ICloudinaryService cloudinaryService,
        IotShowroomContext context,
        ILogger<FinalProjectService> logger)
    {
        _finalProjectRepository = finalProjectRepository;
        _cloudinaryService = cloudinaryService;
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<FinalProjectSubmissionResponseDto>> SubmitFinalProjectAsync(
        int projectId,
        FinalProjectSubmissionRequestDto request,
        int userId)
    {
        try
        {
            // 1. Validate project exists and user has access
            var project = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Check if user is in the project's group
            var isMember = project.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not a member of this project's group",
                    StatusCodes.Status403Forbidden
                );
            }

            // 2. Check if already has submission
            var existingSubmission = await _finalProjectRepository.GetByProjectIdAsync(projectId);
            if (existingSubmission != null)
            {
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    "Final project already submitted. Use update endpoint to modify.",
                    StatusCodes.Status400BadRequest
                );
            }

            // 3. Create new final submission
            var submission = new FinalProjectSubmission
            {
                ProjectId = projectId,
                SubmissionNotes = request.SubmissionNotes,
                RepositoryUrl = request.RepositoryUrl,
                SubmittedBy = userId,
                SubmittedAt = DateTime.UtcNow,
                Status = "Submitted"
            };

            await _finalProjectRepository.AddAsync(submission);
            await _finalProjectRepository.SaveChangesAsync();

            // 4. Send notification to instructor
            var instructorId = project.Group?.Class?.InstructorId;
            if (instructorId.HasValue)
            {
                var notification = new BusinessObjects.Models.Notification
                {
                    UserId = instructorId.Value,
                    Title = "New Final Project Submission",
                    Message = $"Project '{project.Title}' has submitted their final deliverables",
                    Type = "final_submission",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            // 5. Map to response DTO
            var response = await MapToResponseDto(submission, project);

            return new ResultModel<FinalProjectSubmissionResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Final project submitted successfully. Remember to upload your files!",
                Data = response,
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting final project");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error submitting final project: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<FinalProjectFileUploadResponseDto>> UploadFilesAsync(
        int projectId,
        IFormFile? finalReport,
        IFormFile? presentation,
        IFormFile? sourceCode,
        IFormFile? videoDemo,
        int userId)
    {
        try
        {
            var submission = await _finalProjectRepository.GetByProjectIdWithDetailsAsync(projectId);
            if (submission == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Final submission not found. Please create submission first.",
                    StatusCodes.Status404NotFound
                );
            }

            // Validate user is member of project group
            var isMember = submission.Project?.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not authorized to upload files to this submission",
                    StatusCodes.Status403Forbidden
                );
            }

            // Check if can update
            var canUpdate = await _finalProjectRepository.CanUpdateAsync(projectId);
            if (!canUpdate)
            {
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    "Cannot update submission after deadline",
                    StatusCodes.Status400BadRequest
                );
            }

            var errors = new List<string>();
            var successCount = 0;
            var response = new FinalProjectFileUploadResponseDto();

            // Upload final report
            if (finalReport != null)
            {
                var result = await UploadFileToCloudinary(finalReport, "final_reports");
                if (result != null)
                {
                    submission.FinalReportUrl = result;
                    response.FinalReportUrl = result;
                    successCount++;
                }
                else
                {
                    errors.Add($"Failed to upload final report: {finalReport.FileName}");
                }
            }

            // Upload presentation
            if (presentation != null)
            {
                var result = await UploadFileToCloudinary(presentation, "presentations");
                if (result != null)
                {
                    submission.PresentationUrl = result;
                    response.PresentationUrl = result;
                    successCount++;
                }
                else
                {
                    errors.Add($"Failed to upload presentation: {presentation.FileName}");
                }
            }

            // Upload source code
            if (sourceCode != null)
            {
                var result = await UploadFileToCloudinary(sourceCode, "source_code");
                if (result != null)
                {
                    submission.SourceCodeUrl = result;
                    response.SourceCodeUrl = result;
                    successCount++;
                }
                else
                {
                    errors.Add($"Failed to upload source code: {sourceCode.FileName}");
                }
            }

            // Upload video demo
            if (videoDemo != null)
            {
                var result = await UploadFileToCloudinary(videoDemo, "video_demos");
                if (result != null)
                {
                    submission.VideoDemoUrl = result;
                    response.VideoDemoUrl = result;
                    successCount++;
                }
                else
                {
                    errors.Add($"Failed to upload video demo: {videoDemo.FileName}");
                }
            }

            // Update submission
            submission.LastUpdatedAt = DateTime.UtcNow;
            await _finalProjectRepository.UpdateAsync(submission);
            await _finalProjectRepository.SaveChangesAsync();

            response.SuccessCount = successCount;
            response.FailedCount = errors.Count;
            response.ErrorMessages = errors;

            return new ResultModel<FinalProjectFileUploadResponseDto>
            {
                IsSuccess = true,
                Message = $"Uploaded {successCount} file(s) successfully",
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
            _logger.LogError(ex, "Error uploading final project files");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error uploading files: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<FinalProjectSubmissionResponseDto>> GetFinalSubmissionAsync(
        int projectId,
        int userId)
    {
        try
        {
            var submission = await _finalProjectRepository.GetByProjectIdWithDetailsAsync(projectId);
            if (submission == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Final submission not found",
                    StatusCodes.Status404NotFound
                );
            }

            // ? Check if user is a member of the project group
            var isMember = submission.Project?.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            
            // ? Check if user is the instructor of the class
            var isInstructor = false;
            var classId = submission.Project?.Group?.ClassId;
            if (classId.HasValue)
            {
                var classEntity = await _context.Classes
                    .Include(c => c.Instructor)
                    .FirstOrDefaultAsync(c => c.ClassId == classId.Value);
                
                isInstructor = classEntity?.InstructorId == userId;
            }

            // ? Allow access if user is either a group member OR the instructor
            if (!isMember && !isInstructor)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not authorized to view this submission",
                    StatusCodes.Status403Forbidden
                );
            }

            var response = await MapToResponseDto(submission, submission.Project);

            return new ResultModel<FinalProjectSubmissionResponseDto>
            {
                IsSuccess = true,
                Message = "Final submission retrieved successfully",
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
            _logger.LogError(ex, "Error getting final submission");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting final submission: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<FinalProjectSubmissionResponseDto>> UpdateFinalSubmissionAsync(
        int projectId,
        FinalProjectUpdateRequestDto request,
        int userId)
    {
        try
        {
            var submission = await _finalProjectRepository.GetByProjectIdWithDetailsAsync(projectId);
            if (submission == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Final submission not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Validate access
            var isMember = submission.Project?.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not authorized to update this submission",
                    StatusCodes.Status403Forbidden
                );
            }

            // Check if can update
            var canUpdate = await _finalProjectRepository.CanUpdateAsync(projectId);
            if (!canUpdate)
            {
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    "Cannot update submission after deadline",
                    StatusCodes.Status400BadRequest
                );
            }

            // Update submission
            submission.SubmissionNotes = request.SubmissionNotes ?? submission.SubmissionNotes;
            submission.RepositoryUrl = request.RepositoryUrl ?? submission.RepositoryUrl;
            submission.LastUpdatedAt = DateTime.UtcNow;

            await _finalProjectRepository.UpdateAsync(submission);
            await _finalProjectRepository.SaveChangesAsync();

            var response = await MapToResponseDto(submission, submission.Project);

            return new ResultModel<FinalProjectSubmissionResponseDto>
            {
                IsSuccess = true,
                Message = "Final submission updated successfully",
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
            _logger.LogError(ex, "Error updating final submission");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error updating final submission: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<bool>> DeleteFileAsync(
        int projectId,
        string fileType,
        int userId)
    {
        try
        {
            var submission = await _finalProjectRepository.GetByProjectIdWithDetailsAsync(projectId);
            if (submission == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Final submission not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Validate access
            var isMember = submission.Project?.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not authorized to delete files from this submission",
                    StatusCodes.Status403Forbidden
                );
            }

            // Check if can update
            var canUpdate = await _finalProjectRepository.CanUpdateAsync(projectId);
            if (!canUpdate)
            {
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    "Cannot modify submission after deadline",
                    StatusCodes.Status400BadRequest
                );
            }

            // Determine which file to delete
            string? fileUrl = fileType.ToLower() switch
            {
                "report" => submission.FinalReportUrl,
                "presentation" => submission.PresentationUrl,
                "sourcecode" => submission.SourceCodeUrl,
                "video" => submission.VideoDemoUrl,
                _ => null
            };

            if (string.IsNullOrEmpty(fileUrl))
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "File not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Delete from Cloudinary
            try
            {
                await _cloudinaryService.DeleteFileAsync(fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to delete file from Cloudinary: {fileUrl}");
            }

            // Update submission to remove file URL
            switch (fileType.ToLower())
            {
                case "report":
                    submission.FinalReportUrl = null;
                    break;
                case "presentation":
                    submission.PresentationUrl = null;
                    break;
                case "sourcecode":
                    submission.SourceCodeUrl = null;
                    break;
                case "video":
                    submission.VideoDemoUrl = null;
                    break;
            }

            submission.LastUpdatedAt = DateTime.UtcNow;
            await _finalProjectRepository.UpdateAsync(submission);
            await _finalProjectRepository.SaveChangesAsync();

            return new ResultModel<bool>
            {
                IsSuccess = true,
                Message = "File deleted successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error deleting file: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<FinalProjectSubmissionResponseDto>> GradeFinalProjectAsync(
        int projectId,
        FinalProjectGradeRequestDto request,
        int instructorId)
    {
        try
        {
            var submission = await _finalProjectRepository.GetByProjectIdWithDetailsAsync(projectId);
            if (submission == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Final submission not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Validate instructor teaches this class
            var classId = submission.Project?.Group?.ClassId;
            var classEntity = await _context.Classes.FindAsync(classId);
            
            if (classEntity?.InstructorId != instructorId)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not authorized to grade this project",
                    StatusCodes.Status403Forbidden
                );
            }

            // Update grade
            submission.Grade = request.Grade;
            submission.Feedback = request.Feedback;
            submission.GradedBy = instructorId;
            submission.GradedAt = DateTime.UtcNow;
            submission.Status = "Graded";

            await _finalProjectRepository.UpdateAsync(submission);
            await _finalProjectRepository.SaveChangesAsync();

            // Send notification to all group members
            var groupMembers = submission.Project?.Group?.GroupMembers;
            if (groupMembers != null)
            {
                foreach (var member in groupMembers)
                {
                    var notification = new BusinessObjects.Models.Notification
                    {
                        UserId = member.UserId,
                        Title = "Final Project Graded",
                        Message = $"Your final project '{submission.Project?.Title}' has been graded: {request.Grade}/100",
                        Type = "final_graded",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();
            }

            var response = await MapToResponseDto(submission, submission.Project);

            return new ResultModel<FinalProjectSubmissionResponseDto>
            {
                IsSuccess = true,
                Message = "Final project graded successfully",
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
            _logger.LogError(ex, "Error grading final project");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error grading final project: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    // Helper methods
    private async Task<string?> UploadFileToCloudinary(IFormFile file, string folder)
    {
        try
        {
            _logger.LogInformation("Attempting to upload file: {FileName}, Size: {Size} bytes, ContentType: {ContentType}, Folder: {Folder}",
                file.FileName, file.Length, file.ContentType, folder);

            var result = await _cloudinaryService.UploadFileAsync(file, $"SWP391/{folder}");
            
            if (result != null)
            {
                _logger.LogInformation("File uploaded successfully: {FileName} -> {Url}", 
                    file.FileName, result.SecureUrl);
                return result.SecureUrl;
            }
            else
            {
                _logger.LogError("Upload result is null for file: {FileName}", file.FileName);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Cloudinary: {FileName}, ContentType: {ContentType}", 
                file.FileName, file.ContentType);
            return null;
        }
    }

    private async Task<FinalProjectSubmissionResponseDto> MapToResponseDto(
        FinalProjectSubmission submission,
        BusinessObjects.Models.Project? project)
    {
        project ??= await _context.Projects
            .Include(p => p.Group)
            .FirstOrDefaultAsync(p => p.ProjectId == submission.ProjectId);

        // Find final milestone to get deadline
        // Load all milestones for this project first, then filter in memory
        var milestones = await _context.ProjectMilestones
            .Where(m => m.ProjectId == submission.ProjectId)
            .OrderByDescending(m => m.MilestoneId)
            .ToListAsync(); // Load to client first
        
        var finalMilestone = milestones
            .FirstOrDefault(m => m.Title != null && 
                (m.Title.Contains("Final", StringComparison.OrdinalIgnoreCase) ||
                 m.Title.Contains("Submission", StringComparison.OrdinalIgnoreCase)));

        var canUpdate = await _finalProjectRepository.CanUpdateAsync(submission.ProjectId);

        return new FinalProjectSubmissionResponseDto
        {
            FinalSubmissionId = submission.FinalSubmissionId,
            ProjectId = submission.ProjectId,
            ProjectTitle = project?.Title,
            GroupName = project?.Group?.GroupName,
            FinalReportUrl = submission.FinalReportUrl,
            PresentationUrl = submission.PresentationUrl,
            SourceCodeUrl = submission.SourceCodeUrl,
            VideoDemoUrl = submission.VideoDemoUrl,
            RepositoryUrl = submission.RepositoryUrl,
            SubmissionNotes = submission.SubmissionNotes,
            SubmittedBy = submission.SubmittedBy,
            SubmittedByName = submission.SubmittedByNavigation?.FullName,
            SubmittedAt = submission.SubmittedAt,
            LastUpdatedAt = submission.LastUpdatedAt,
            Grade = submission.Grade,
            Feedback = submission.Feedback,
            GradedBy = submission.GradedBy,
            GradedByName = submission.GradedByNavigation?.FullName,
            GradedAt = submission.GradedAt,
            Status = submission.Status,
            CanUpdate = canUpdate,
            Deadline = finalMilestone?.DueDate?.ToDateTime(TimeOnly.MaxValue)
        };
    }
}
