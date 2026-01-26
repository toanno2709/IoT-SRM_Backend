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

            // 4. Send notification to main instructor
            var instructorId = project.Group?.Class?.InstructorId;
            if (instructorId.HasValue)
            {
                // Create Data JSON for notification
                var notificationData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    classId = project.Group?.ClassId,
                    groupId = project.GroupId,
                    projectId = project.ProjectId,
                    finalSubmissionId = submission.FinalSubmissionId
                });

                var notification = new BusinessObjects.Models.Notification
                {
                    UserId = instructorId.Value,
                    Title = "New Final Project Submission",
                    Message = $"Project '{project.Title}' has submitted their final deliverables",
                    Type = "final_submission",
                    Data = notificationData,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            // 5. Send notification to all assigned graders
            var classId = project.Group?.ClassId;
            if (classId.HasValue)
            {
                var graders = await _context.ClassGraders
                    .Where(cg => cg.ClassId == classId.Value && cg.IsActive)
                    .Include(cg => cg.Instructor)
                    .ToListAsync();

                var groupId = project.Group!.GroupId;
                
                // Create Data JSON for grader notifications
                var graderNotificationData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    classId = classId.Value,
                    groupId = groupId,
                    projectId = project.ProjectId,
                    finalSubmissionId = submission.FinalSubmissionId
                });
                
                foreach (var grader in graders)
                {
                    var graderNotification = new BusinessObjects.Models.Notification
                    {
                        UserId = grader.InstructorId,
                        Title = "New Final Project Submission to Grade",
                        Message = $"Project '{project.Title}' from {project.Group.GroupName} has submitted their final project and is ready for grading (Class ID: {classId}, Group ID: {groupId})",
                        Type = "final_submission",
                        Data = graderNotificationData,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(graderNotification);
                }
            }

            await _context.SaveChangesAsync();

            // 6. Map to response DTO
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
            _logger.LogInformation("=== GetFinalSubmissionAsync START === ProjectId: {ProjectId}, UserId: {UserId}", projectId, userId);
            
            var submission = await _finalProjectRepository.GetByProjectIdWithDetailsAsync(projectId);
            if (submission == null)
            {
                _logger.LogWarning("Final submission not found for ProjectId: {ProjectId}", projectId);
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Final submission not found",
                    StatusCodes.Status404NotFound
                );
            }

            _logger.LogInformation("Found submission {SubmissionId} for project {ProjectId}", submission.FinalSubmissionId, projectId);

            // Check if user is a member of the project group
            var isMember = submission.Project?.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            _logger.LogInformation("User {UserId} is member: {IsMember}", userId, isMember);
            
            // Check if user is the main instructor of the class
            var mainInstructorId = submission.Project?.Group?.Class?.InstructorId;
            var isMainInstructor = mainInstructorId == userId;
            _logger.LogInformation("Class InstructorId: {InstructorId}, User {UserId} is main instructor: {IsMainInstructor}", 
                mainInstructorId, userId, isMainInstructor);

            // Check if user is an assigned grader for this class
            var isAssignedGrader = submission.Project?.Group?.Class?.ClassGraders?
                .Any(cg => cg.InstructorId == userId && cg.IsActive) ?? false;
            _logger.LogInformation("User {UserId} is assigned grader: {IsAssignedGrader}", userId, isAssignedGrader);

            // Allow access if user is group member OR main instructor OR assigned grader
            if (!isMember && !isMainInstructor && !isAssignedGrader)
            {
                _logger.LogWarning("Access denied for user {UserId}. IsMember: {IsMember}, IsMainInstructor: {IsMainInstructor}, IsAssignedGrader: {IsAssignedGrader}", 
                    userId, isMember, isMainInstructor, isAssignedGrader);
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not authorized to view this submission",
                    StatusCodes.Status403Forbidden
                );
            }

            _logger.LogInformation("Access granted for user {UserId}", userId);

            var response = await MapToResponseDto(submission, submission.Project);

            _logger.LogInformation("=== GetFinalSubmissionAsync SUCCESS ===");

            return new ResultModel<FinalProjectSubmissionResponseDto>
            {
                IsSuccess = true,
                Message = "Final submission retrieved successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "AppException in GetFinalSubmissionAsync: {Message}", ex.Message);
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

    public async Task<ResultModel<string>> GetFileUrlAsync(int projectId, string fileType, int userId)
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

            // Check if user is a member of the project group
            var isMember = submission.Project?.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            
            // Check if user is the main instructor of the class
            var mainInstructorId = submission.Project?.Group?.Class?.InstructorId;
            var isMainInstructor = mainInstructorId == userId;

            // Check if user is an assigned grader for this class
            var isAssignedGrader = submission.Project?.Group?.Class?.ClassGraders?
                .Any(cg => cg.InstructorId == userId && cg.IsActive) ?? false;

            // Allow access if user is group member OR main instructor OR assigned grader
            if (!isMember && !isMainInstructor && !isAssignedGrader)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not authorized to access this file",
                    StatusCodes.Status403Forbidden
                );
            }

            // Get file URL
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

            return new ResultModel<string>
            {
                IsSuccess = true,
                Message = "File URL retrieved successfully",
                Data = fileUrl,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file URL");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting file URL: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<string>> GetFileUrlBySubmissionIdAsync(int finalSubmissionId, string fileType, int userId)
    {
        try
        {
            var submission = await _context.FinalProjectSubmissions
                .Include(s => s.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.GroupMembers)
                .Include(s => s.Project)
                    .ThenInclude(p => p.Group)
                        .ThenInclude(g => g!.Class)
                            .ThenInclude(c => c!.ClassGraders)
                .FirstOrDefaultAsync(s => s.FinalSubmissionId == finalSubmissionId);

            if (submission == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Final submission not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Check if user is the main instructor of the class
            var mainInstructorId = submission.Project?.Group?.Class?.InstructorId;
            var isMainInstructor = mainInstructorId == userId;

            // Check if user is an assigned grader
            var isAssignedGrader = submission.Project?.Group?.Class?.ClassGraders?
                .Any(cg => cg.InstructorId == userId && cg.IsActive) ?? false;

            // Allow access if user is main instructor OR assigned grader
            if (!isMainInstructor && !isAssignedGrader)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not authorized to access this file",
                    StatusCodes.Status403Forbidden
                );
            }

            // Get file URL
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

            return new ResultModel<string>
            {
                IsSuccess = true,
                Message = "File URL retrieved successfully",
                Data = fileUrl,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file URL by submission ID");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting file URL: {ex.Message}",
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
                // Create Data JSON for notification
                var notificationData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    classId = submission.Project?.Group?.ClassId,
                    groupId = submission.Project?.GroupId,
                    projectId = submission.ProjectId,
                    finalSubmissionId = submission.FinalSubmissionId
                });

                foreach (var member in groupMembers)
                {
                    var notification = new BusinessObjects.Models.Notification
                    {
                        UserId = member.UserId,
                        Title = "Final Project Graded",
                        Message = $"Your final project '{submission.Project?.Title}' has been graded: {request.Grade}/100",
                        Type = "final_graded",
                        Data = notificationData,
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
            FinalReportUrl = ConvertToDownloadableUrl(submission.FinalReportUrl),
            PresentationUrl = ConvertToDownloadableUrl(submission.PresentationUrl),
            SourceCodeUrl = ConvertToDownloadableUrl(submission.SourceCodeUrl),
            VideoDemoUrl = ConvertToDownloadableUrl(submission.VideoDemoUrl),
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

    /// <summary>
    /// Convert stored Cloudinary URL to downloadable URL with fl_attachment flag
    /// This ensures files like PDF and ZIP can be downloaded properly
    /// </summary>
    private string? ConvertToDownloadableUrl(string? cloudinaryUrl)
    {
        if (string.IsNullOrEmpty(cloudinaryUrl))
            return cloudinaryUrl;

        try
        {
            // Check if URL is from Cloudinary
            if (!cloudinaryUrl.Contains("cloudinary.com"))
                return cloudinaryUrl;

            // Check if it's a raw file (not an image)
            if (!cloudinaryUrl.Contains("/raw/upload/") && !cloudinaryUrl.Contains("/video/upload/"))
                return cloudinaryUrl;

            // Check if already has fl_attachment
            if (cloudinaryUrl.Contains("fl_attachment"))
                return cloudinaryUrl;

            var uri = new Uri(cloudinaryUrl);
            var path = uri.AbsolutePath;
            
            // Find the "upload/" part
            var uploadIndex = path.IndexOf("/upload/");
            if (uploadIndex == -1)
                return cloudinaryUrl;

            // Extract filename from the URL
            var lastSlashIndex = path.LastIndexOf('/');
            var fileName = lastSlashIndex > 0 ? path.Substring(lastSlashIndex + 1) : "download";
            
            // URL decode the filename if needed
            fileName = Uri.UnescapeDataString(fileName);
            
            // Encode the filename for URL
            var encodedFileName = Uri.EscapeDataString(fileName);
            
            // Insert the fl_attachment transformation
            var beforeUpload = path.Substring(0, uploadIndex + 8); // Include "/upload/"
            var afterUpload = path.Substring(uploadIndex + 8);
            
            // Build the new path with fl_attachment
            var newPath = $"{beforeUpload}fl_attachment:{encodedFileName}/{afterUpload}";
            
            // Reconstruct the full URL
            var downloadUrl = $"{uri.Scheme}://{uri.Host}{newPath}";
            
            return downloadUrl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not convert URL to downloadable format: {Url}", cloudinaryUrl);
            return cloudinaryUrl; // Return original URL if conversion fails
        }
    }
}
