using AppBackend.BusinessObjects.Models;
using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Data;
using AppBackend.Repositories.Repositories.MilestoneSubmissionRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.Submission;

public class SubmissionService : ISubmissionService
{
    private readonly IMilestoneSubmissionRepository _submissionRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IotShowroomContext _context;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        IMilestoneSubmissionRepository submissionRepository,
        IProjectRepository projectRepository,
        ICloudinaryService cloudinaryService,
        IotShowroomContext context,
        ILogger<SubmissionService> logger)
    {
        _submissionRepository = submissionRepository;
        _projectRepository = projectRepository;
        _cloudinaryService = cloudinaryService;
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<MilestoneSubmissionResponseDto>> SubmitMilestoneAsync(
        MilestoneSubmissionRequestDto request, 
        int userId)
    {
        try
        {
            // 1. Validate project exists and user has access
            var project = await _context.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.GroupMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId);

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

            // 2. Validate milestone exists
            var milestone = await _context.ProjectMilestones
                .FirstOrDefaultAsync(m => m.MilestoneId == request.MilestoneId);

            if (milestone == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Milestone not found",
                    StatusCodes.Status404NotFound
                );
            }

            // 3. Check if can still submit (before deadline)
            var canResubmit = await _submissionRepository.CanResubmitAsync(
                request.ProjectId, 
                request.MilestoneId
            );

            // 4. Get existing submission or create new
            var submission = await _submissionRepository.GetSubmissionByProjectAndMilestoneAsync(
                request.ProjectId,
                request.MilestoneId
            );

            if (submission == null)
            {
                // Create new submission
                submission = new MilestoneSubmission
                {
                    ProjectId = request.ProjectId,
                    MilestoneDefId = request.MilestoneId,
                    LastVersionNo = 1,
                    LastSubmittedAt = DateTime.UtcNow
                };
                await _submissionRepository.AddAsync(submission);
            }
            else
            {
                // Update existing submission (resubmit)
                if (!canResubmit && submission.LastVersionNo > 0)
                {
                    throw new AppException(
                        CommonMessageConstants.ERROR,
                        "Cannot resubmit after deadline",
                        StatusCodes.Status400BadRequest
                    );
                }

                submission.LastVersionNo = (submission.LastVersionNo ?? 0) + 1;
                submission.LastSubmittedAt = DateTime.UtcNow;
                await _submissionRepository.UpdateAsync(submission);
            }

            await _submissionRepository.SaveChangesAsync();

            // 5. Send notification to instructor
            var instructorId = project.Group?.Class?.InstructorId;
            if (instructorId.HasValue)
            {
                var notification = new AppBackend.BusinessObjects.Models.Notification
                {
                    UserId = instructorId.Value,
                    Title = $"New Submission: {milestone.Title}",
                    Message = $"Project '{project.Title}' has submitted milestone '{milestone.Title}' (Version {submission.LastVersionNo})",
                    Type = "milestone_submission",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            // 6. Map to response DTO
            var response = await MapToResponseDto(submission);
            response.CanResubmit = canResubmit;
            response.Deadline = milestone?.DueDate?.ToDateTime(TimeOnly.MinValue);

            return new ResultModel<MilestoneSubmissionResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Milestone submitted successfully (Version {submission.LastVersionNo})",
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
            _logger.LogError(ex, "Error submitting milestone");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error submitting milestone: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<MilestoneSubmissionHistoryDto>> GetSubmissionHistoryAsync(
        int projectId, 
        int milestoneDefId, 
        int userId)
    {
        try
        {
            // Validate access
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

            var isMember = project.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            if (!isMember)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not a member of this project's group",
                    StatusCodes.Status403Forbidden
                );
            }

            // Get submission
            var submission = await _submissionRepository.GetSubmissionByProjectAndMilestoneAsync(
                projectId,
                milestoneDefId
            );

            var milestone = await _context.ProjectMilestones
                .FirstOrDefaultAsync(m => m.MilestoneId == milestoneDefId);

            if (submission == null)
            {
                // No submissions yet
                return new ResultModel<MilestoneSubmissionHistoryDto>
                {
                    IsSuccess = true,
                    Message = "No submissions found for this milestone",
                    Data = new MilestoneSubmissionHistoryDto
                    {
                        MilestoneId = milestoneDefId,
                        MilestoneTitle = milestone?.Title,
                        Weight = milestone?.Weight,
                        Deadline = milestone?.DueDate?.ToDateTime(TimeOnly.MinValue),
                        TotalSubmissions = 0,
                        AllVersions = new List<MilestoneSubmissionResponseDto>()
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }

            // Get all files grouped by version
            var allFiles = await _context.SubmissionFiles
                .Include(f => f.UploadedByNavigation)
                .Where(f => f.SubmissionId == submission.SubmissionId)
                .OrderBy(f => f.VersionNo)
                .ToListAsync();

            // Create version list
            var versions = new List<MilestoneSubmissionResponseDto>();
            var versionGroups = allFiles.GroupBy(f => f.VersionNo).OrderByDescending(g => g.Key);

            foreach (var versionGroup in versionGroups)
            {
                var versionDto = await MapToResponseDto(submission);
                versionDto.Version = versionGroup.Key;
                versionDto.Files = versionGroup.Select(f => new MilestoneFileDto
                {
                    FileId = f.FileId,
                    SubmissionId = f.SubmissionId,
                    FileName = Path.GetFileName(f.FileUrl),
                    FileUrl = f.FileUrl,
                    FileSize = f.SizeBytes ?? 0,
                    FileType = f.MimeType,
                    UploadedBy = f.UploadedBy ?? 0,
                    UploadedByName = f.UploadedByNavigation?.FullName,
                    UploadedAt = f.UploadedAt
                }).ToList();

                versions.Add(versionDto);
            }

            var history = new MilestoneSubmissionHistoryDto
            {
                MilestoneId = milestoneDefId,
                MilestoneTitle = milestone?.Title,
                Weight = milestone?.Weight,
                Deadline = milestone?.DueDate?.ToDateTime(TimeOnly.MinValue),
                TotalSubmissions = submission.LastVersionNo ?? 0,
                LatestSubmission = versions.FirstOrDefault(),
                AllVersions = versions
            };

            return new ResultModel<MilestoneSubmissionHistoryDto>
            {
                IsSuccess = true,
                Message = "Submission history retrieved successfully",
                Data = history,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission history");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting submission history: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<MilestoneSubmissionResponseDto>> GetLatestSubmissionAsync(
        int projectId, 
        int milestoneDefId, 
        int userId)
    {
        try
        {
            var history = await GetSubmissionHistoryAsync(projectId, milestoneDefId, userId);
            
            if (!history.IsSuccess || history.Data?.LatestSubmission == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "No submission found",
                    StatusCodes.Status404NotFound
                );
            }

            return new ResultModel<MilestoneSubmissionResponseDto>
            {
                IsSuccess = true,
                Message = "Latest submission retrieved successfully",
                Data = history.Data.LatestSubmission,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest submission");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting latest submission: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<FileUploadResponseDto>> UploadFilesAsync(
        int submissionId, 
        List<IFormFile> files, 
        int userId)
    {
        try
        {
            var submission = await _submissionRepository.GetSubmissionWithDetailsAsync(submissionId);
            if (submission == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Submission not found",
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

            var currentVersion = submission.LastVersionNo ?? 1;
            var uploadedFiles = new List<MilestoneFileDto>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    // Upload to Cloudinary
                    var uploadResult = await _cloudinaryService.UploadFileAsync(file);

                    if (uploadResult == null || string.IsNullOrEmpty(uploadResult.SecureUrl))
                    {
                        errors.Add($"Failed to upload {file.FileName}");
                        continue;
                    }

                    // Save file record
                    var submissionFile = new SubmissionFile
                    {
                        SubmissionId = submissionId,
                        VersionNo = currentVersion,
                        FileUrl = uploadResult.SecureUrl,
                        MimeType = file.ContentType,
                        SizeBytes = file.Length,
                        UploadedBy = userId,
                        UploadedAt = DateTime.UtcNow
                    };

                    _context.SubmissionFiles.Add(submissionFile);
                    await _context.SaveChangesAsync();

                    uploadedFiles.Add(new MilestoneFileDto
                    {
                        FileId = submissionFile.FileId,
                        SubmissionId = submissionFile.SubmissionId,
                        FileName = file.FileName,
                        FileUrl = submissionFile.FileUrl,
                        FileSize = file.Length,
                        FileType = file.ContentType,
                        UploadedBy = userId,
                        UploadedAt = submissionFile.UploadedAt
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error uploading file {file.FileName}");
                    errors.Add($"Error uploading {file.FileName}: {ex.Message}");
                }
            }

            var response = new FileUploadResponseDto
            {
                UploadedFiles = uploadedFiles,
                SuccessCount = uploadedFiles.Count,
                FailedCount = errors.Count,
                ErrorMessages = errors
            };

            return new ResultModel<FileUploadResponseDto>
            {
                IsSuccess = true,
                Message = $"Uploaded {uploadedFiles.Count} of {files.Count} files successfully",
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
            _logger.LogError(ex, "Error uploading files");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error uploading files: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<List<MilestoneFileDto>>> GetSubmissionFilesAsync(
        int submissionId, 
        int? versionNo = null)
    {
        try
        {
            var files = await _submissionRepository.GetSubmissionFilesAsync(submissionId, versionNo);

            var fileDtos = files.Select(f => new MilestoneFileDto
            {
                FileId = f.FileId,
                SubmissionId = f.SubmissionId,
                FileName = Path.GetFileName(f.FileUrl),
                FileUrl = f.FileUrl,
                FileSize = f.SizeBytes ?? 0,
                FileType = f.MimeType,
                UploadedBy = f.UploadedBy ?? 0,
                UploadedByName = f.UploadedByNavigation?.FullName,
                UploadedAt = f.UploadedAt
            }).ToList();

            return new ResultModel<List<MilestoneFileDto>>
            {
                IsSuccess = true,
                Message = "Files retrieved successfully",
                Data = fileDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission files");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting submission files: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<bool>> DeleteFileAsync(int fileId, int userId)
    {
        try
        {
            var file = await _context.SubmissionFiles
                .Include(f => f.Submission)
                    .ThenInclude(s => s.Project)
                        .ThenInclude(p => p!.Group)
                            .ThenInclude(g => g!.GroupMembers)
                .FirstOrDefaultAsync(f => f.FileId == fileId);

            if (file == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "File not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Validate user is member or uploaded the file
            var isMember = file.Submission?.Project?.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            var isUploader = file.UploadedBy == userId;

            if (!isMember && !isUploader)
            {
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "You are not authorized to delete this file",
                    StatusCodes.Status403Forbidden
                );
            }

            // Delete from Cloudinary
            try
            {
                await _cloudinaryService.DeleteFileAsync(file.FileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to delete file from Cloudinary: {file.FileUrl}");
            }

            // Delete from database
            _context.SubmissionFiles.Remove(file);
            await _context.SaveChangesAsync();

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

    // Helper method to map entity to DTO
    private async Task<MilestoneSubmissionResponseDto> MapToResponseDto(MilestoneSubmission submission)
    {
        var milestone = await _context.ProjectMilestones
            .FirstOrDefaultAsync(m => m.MilestoneId == submission.MilestoneDefId);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == submission.ProjectId);

        // Get grade from MilestoneEvaluation if exists
        var evaluation = await _context.MilestoneEvaluations
            .Include(e => e.Instructor)
            .FirstOrDefaultAsync(e => e.ProjectId == submission.ProjectId 
                && e.MilestoneDefId == submission.MilestoneDefId);

        return new MilestoneSubmissionResponseDto
        {
            SubmissionId = submission.SubmissionId,
            ProjectId = submission.ProjectId,
            ProjectTitle = project?.Title,
            MilestoneId = submission.MilestoneDefId,
            MilestoneTitle = milestone?.Title,
            Version = submission.LastVersionNo ?? 1,
            SubmittedAt = submission.LastSubmittedAt ?? DateTime.UtcNow,
            Grade = evaluation?.Score,
            Feedback = evaluation?.Feedback,
            GradedBy = evaluation?.InstructorId,
            GradedByName = evaluation?.Instructor?.FullName,
            GradedAt = evaluation?.EvaluatedAt,
            Status = evaluation != null ? "Graded" : "Submitted",
            Files = new List<MilestoneFileDto>()
        };
    }
}
