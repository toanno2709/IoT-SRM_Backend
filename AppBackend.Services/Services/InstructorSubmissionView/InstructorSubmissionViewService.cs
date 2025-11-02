using AppBackend.Repositories.Repositories.MilestoneSubmissionRepo;
using AppBackend.Repositories.Repositories.ProjectMilestoneRepo;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.MilestoneEvaluationRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.InstructorSubmissionView;

public class InstructorSubmissionViewService : IInstructorSubmissionViewService
{
    private readonly IMilestoneSubmissionRepository _submissionRepository;
    private readonly IProjectMilestoneRepository _milestoneRepository;
    private readonly IClassRepository _classRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMilestoneEvaluationRepository _evaluationRepository;

    public InstructorSubmissionViewService(
        IMilestoneSubmissionRepository submissionRepository,
        IProjectMilestoneRepository milestoneRepository,
        IClassRepository classRepository,
        IProjectRepository projectRepository,
        IGroupRepository groupRepository,
        IMilestoneEvaluationRepository evaluationRepository)
    {
        _submissionRepository = submissionRepository;
        _milestoneRepository = milestoneRepository;
        _classRepository = classRepository;
        _projectRepository = projectRepository;
        _groupRepository = groupRepository;
        _evaluationRepository = evaluationRepository;
    }

    public async Task<ResultModel<List<InstructorSubmissionViewDto>>> GetSubmissionsByMilestoneAsync(
        int milestoneId, 
        int instructorId, 
        SubmissionFilterDto? filter = null)
    {
        try
        {
            // Get milestone details
            var milestone = await _milestoneRepository.GetByIdAsync(milestoneId);
            if (milestone == null)
            {
                return new ResultModel<List<InstructorSubmissionViewDto>>
                {
                    IsSuccess = false,
                    Message = "Milestone not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Get all submissions for this milestone
            var submissions = await _submissionRepository.FindAsync(s => s.MilestoneDefId == milestoneId);
            
            var result = new List<InstructorSubmissionViewDto>();

            foreach (var submission in submissions)
            {
                // Load related entities
                var project = await _projectRepository.GetByIdAsync(submission.ProjectId);
                if (project == null || !project.GroupId.HasValue) continue;

                var group = await _groupRepository.GetByIdAsync(project.GroupId.Value);
                if (group == null) continue;

                // Verify instructor owns this class
                var classEntity = await _classRepository.GetByIdAsync(group.ClassId);
                if (classEntity?.InstructorId != instructorId) continue;

                // Get evaluation (grade)
                var evaluation = await _evaluationRepository.GetByProjectMilestoneInstructorAsync(
                    submission.ProjectId, 
                    submission.MilestoneDefId, 
                    instructorId);

                // Get submitted by user info
                var submittedByUser = submission.SubmissionFiles?.FirstOrDefault()?.UploadedByNavigation;

                // Check if late submission - convert DateOnly to DateTime for comparison
                DateTime? dueDateTime = milestone.DueDate.HasValue 
                    ? milestone.DueDate.Value.ToDateTime(TimeOnly.MinValue) 
                    : null;
                    
                var isLate = dueDateTime.HasValue && 
                           submission.LastSubmittedAt.HasValue && 
                           submission.LastSubmittedAt.Value > dueDateTime.Value;

                int? daysLate = null;
                if (isLate && dueDateTime.HasValue && submission.LastSubmittedAt.HasValue)
                {
                    daysLate = (int)(submission.LastSubmittedAt.Value - dueDateTime.Value).TotalDays;
                }

                // Determine status based on evaluation and submission
                string status = "Pending";
                if (evaluation != null)
                {
                    status = "Graded";
                }
                else if (submission.LastSubmittedAt.HasValue)
                {
                    status = "Submitted";
                }

                var dto = new InstructorSubmissionViewDto
                {
                    SubmissionId = submission.SubmissionId,
                    ProjectId = submission.ProjectId,
                    ProjectTitle = project.Title,
                    GroupId = group.GroupId,
                    GroupName = group.GroupName,
                    MilestoneDefId = submission.MilestoneDefId,
                    MilestoneTitle = milestone.Title,
                    VersionNo = submission.LastVersionNo ?? 0,
                    Status = status,
                    SubmittedAt = submission.LastSubmittedAt,
                    LastSubmittedAt = submission.LastSubmittedAt,
                    FileCount = submission.SubmissionFiles?.Count ?? 0,
                    IsGraded = evaluation != null,
                    Grade = evaluation?.Score,
                    SubmittedBy = submittedByUser?.FullName,
                    SubmittedByUserId = submittedByUser?.UserId,
                    IsLateSubmission = isLate,
                    DaysLate = daysLate,
                    CanGrade = status == "Submitted" || status == "Graded"
                };

                result.Add(dto);
            }

            // Apply filters
            if (filter != null)
            {
                if (filter.IsGraded.HasValue)
                {
                    result = result.Where(s => s.IsGraded == filter.IsGraded.Value).ToList();
                }

                if (filter.IsLate.HasValue)
                {
                    result = result.Where(s => s.IsLateSubmission == filter.IsLate.Value).ToList();
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    result = result.Where(s => s.Status == filter.Status).ToList();
                }

                // Apply sorting
                result = ApplySorting(result, filter.SortBy, filter.SortOrder);
            }
            else
            {
                // Default sorting: by submission date (newest first)
                result = result.OrderByDescending(s => s.LastSubmittedAt ?? s.SubmittedAt).ToList();
            }

            return new ResultModel<List<InstructorSubmissionViewDto>>
            {
                IsSuccess = true,
                Message = $"Found {result.Count} submissions for milestone",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<InstructorSubmissionViewDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving submissions: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<ClassSubmissionOverviewDto>> GetClassSubmissionsAsync(
        int classId, 
        int instructorId, 
        SubmissionFilterDto? filter = null)
    {
        try
        {
            // Verify instructor owns this class
            var classEntity = await _classRepository.GetByIdAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<ClassSubmissionOverviewDto>
                {
                    IsSuccess = false,
                    Message = "Class not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (classEntity.InstructorId != instructorId)
            {
                return new ResultModel<ClassSubmissionOverviewDto>
                {
                    IsSuccess = false,
                    Message = "You are not authorized to view this class",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Get all projects in class
            var groups = await _groupRepository.GetGroupsByClassAsync(classId);
            var allProjects = new List<BusinessObjects.Models.Project>();
            foreach (var group in groups)
            {
                var projects = await _projectRepository.FindAsync(p => p.GroupId == group.GroupId);
                allProjects.AddRange(projects.Where(p => p.GroupId.HasValue));
            }

            // Get all milestones for these projects
            var milestoneGroups = new List<MilestoneSubmissionGroupDto>();
            var allSubmissions = new List<InstructorSubmissionViewDto>();

            // Get unique milestones from first project (assuming all projects have same milestones)
            if (allProjects.Any())
            {
                var firstProject = allProjects.First();
                var milestones = await _milestoneRepository.GetByProjectAsync(firstProject.ProjectId);

                foreach (var milestone in milestones.OrderBy(m => m.DueDate))
                {
                    // Apply milestone filter if specified
                    if (filter?.MilestoneDefId.HasValue == true && 
                        milestone.MilestoneId != filter.MilestoneDefId.Value)
                    {
                        continue;
                    }

                    var submissionsResult = await GetSubmissionsByMilestoneAsync(
                        milestone.MilestoneId, 
                        instructorId, 
                        filter);

                    if (submissionsResult.IsSuccess && submissionsResult.Data != null)
                    {
                        var submissions = submissionsResult.Data;
                        allSubmissions.AddRange(submissions);

                        // Convert DateOnly to DateTime for DTO
                        DateTime? dueDateTimeForDto = milestone.DueDate.HasValue 
                            ? milestone.DueDate.Value.ToDateTime(TimeOnly.MinValue) 
                            : null;

                        var milestoneGroup = new MilestoneSubmissionGroupDto
                        {
                            MilestoneDefId = milestone.MilestoneId,
                            MilestoneTitle = milestone.Title,
                            Deadline = dueDateTimeForDto,
                            Weight = milestone.Weight,
                            TotalGroups = allProjects.Count,
                            SubmittedGroups = submissions.Count,
                            GradedGroups = submissions.Count(s => s.IsGraded),
                            Submissions = submissions
                        };

                        milestoneGroups.Add(milestoneGroup);
                    }
                }
            }

            // Calculate statistics
            var statistics = new SubmissionStatisticsDto
            {
                TotalProjects = allProjects.Count,
                TotalSubmissions = allSubmissions.Count,
                PendingGrading = allSubmissions.Count(s => !s.IsGraded && s.CanGrade),
                GradedSubmissions = allSubmissions.Count(s => s.IsGraded),
                SubmissionRate = allProjects.Count > 0 
                    ? (decimal)allSubmissions.Count / (allProjects.Count * milestoneGroups.Count) * 100 
                    : 0,
                GradingProgress = allSubmissions.Count > 0 
                    ? (decimal)allSubmissions.Count(s => s.IsGraded) / allSubmissions.Count * 100 
                    : 0
            };

            var overview = new ClassSubmissionOverviewDto
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                MilestoneGroups = milestoneGroups,
                Statistics = statistics
            };

            return new ResultModel<ClassSubmissionOverviewDto>
            {
                IsSuccess = true,
                Message = "Class submission overview retrieved successfully",
                Data = overview,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassSubmissionOverviewDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving class submissions: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<InstructorSubmissionFilesDto>> GetSubmissionFilesAsync(
        int submissionId, 
        int instructorId)
    {
        try
        {
            // Get submission with all related data
            var submission = await _submissionRepository.GetSubmissionWithDetailsAsync(submissionId);
            if (submission == null)
            {
                return new ResultModel<InstructorSubmissionFilesDto>
                {
                    IsSuccess = false,
                    Message = "Submission not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Verify instructor owns this class
            var classId = submission.Project?.Group?.ClassId;
            if (classId == null)
            {
                return new ResultModel<InstructorSubmissionFilesDto>
                {
                    IsSuccess = false,
                    Message = "Invalid submission data",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var classEntity = await _classRepository.GetByIdAsync(classId.Value);
            if (classEntity?.InstructorId != instructorId)
            {
                return new ResultModel<InstructorSubmissionFilesDto>
                {
                    IsSuccess = false,
                    Message = "You are not authorized to view this submission",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Get evaluation
            var evaluation = await _evaluationRepository.GetByProjectMilestoneInstructorAsync(
                submission.ProjectId, 
                submission.MilestoneDefId, 
                instructorId);

            // Determine status
            string status = "Pending";
            if (evaluation != null)
            {
                status = "Graded";
            }
            else if (submission.LastSubmittedAt.HasValue)
            {
                status = "Submitted";
            }

            // Map files - extract filename from URL
            var files = submission.SubmissionFiles?.Select(f => new SubmissionFileDetailDto
            {
                FileId = f.FileId,
                FileName = GetFileNameFromUrl(f.FileUrl),
                FileUrl = f.FileUrl,
                FileSize = f.SizeBytes,
                FileSizeFormatted = FormatFileSize(f.SizeBytes),
                FileType = GetFileExtension(GetFileNameFromUrl(f.FileUrl)),
                UploadedAt = f.UploadedAt,
                UploadedBy = f.UploadedByNavigation?.FullName,
                UploadedByUserId = f.UploadedBy
            }).ToList() ?? new List<SubmissionFileDetailDto>();

            var result = new InstructorSubmissionFilesDto
            {
                SubmissionId = submission.SubmissionId,
                ProjectId = submission.ProjectId,
                ProjectTitle = submission.Project?.Title,
                GroupName = submission.Project?.Group?.GroupName,
                MilestoneDefId = submission.MilestoneDefId,
                MilestoneTitle = submission.MilestoneDef?.Title,
                VersionNo = submission.LastVersionNo ?? 0,
                SubmittedAt = submission.LastSubmittedAt,
                Status = status,
                IsGraded = evaluation != null,
                Grade = evaluation?.Score,
                Feedback = evaluation?.Feedback,
                Files = files
            };

            return new ResultModel<InstructorSubmissionFilesDto>
            {
                IsSuccess = true,
                Message = "Submission files retrieved successfully",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<InstructorSubmissionFilesDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving submission files: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<List<InstructorSubmissionViewDto>>> GetPendingGradingSubmissionsAsync(
        int instructorId, 
        int? classId = null)
    {
        try
        {
            var allSubmissions = new List<InstructorSubmissionViewDto>();

            // Get classes for instructor
            var classes = classId.HasValue
                ? (await _classRepository.GetByIdAsync(classId.Value) is var c && c != null ? new List<BusinessObjects.Models.Class> { c } : new List<BusinessObjects.Models.Class>())
                : await _classRepository.GetAssignedClassesAsync(instructorId);

            foreach (var classEntity in classes)
            {
                if (classEntity == null) continue;

                // Get all groups in class
                var groups = await _groupRepository.GetGroupsByClassAsync(classEntity.ClassId);

                foreach (var group in groups)
                {
                    // Get projects
                    var projects = await _projectRepository.FindAsync(p => p.GroupId == group.GroupId);

                    foreach (var project in projects.Where(p => p.GroupId.HasValue))
                    {
                        // Get submissions
                        var submissions = await _submissionRepository.GetSubmissionsByProjectAsync(project.ProjectId);

                        foreach (var submission in submissions)
                        {
                            // Check if graded
                            var evaluation = await _evaluationRepository.GetByProjectMilestoneInstructorAsync(
                                submission.ProjectId,
                                submission.MilestoneDefId,
                                instructorId);

                            // Determine status
                            string status = "Pending";
                            if (evaluation != null)
                            {
                                status = "Graded";
                            }
                            else if (submission.LastSubmittedAt.HasValue)
                            {
                                status = "Submitted";
                            }

                            if (evaluation == null && (status == "Submitted" || status == "Graded"))
                            {
                                var milestone = await _milestoneRepository.GetByIdAsync(submission.MilestoneDefId);
                                var submittedByUser = submission.SubmissionFiles?.FirstOrDefault()?.UploadedByNavigation;

                                // Check if late submission - convert DateOnly to DateTime
                                DateTime? dueDateTime = milestone?.DueDate.HasValue == true
                                    ? milestone.DueDate.Value.ToDateTime(TimeOnly.MinValue) 
                                    : null;

                                var isLate = dueDateTime.HasValue &&
                                           submission.LastSubmittedAt.HasValue &&
                                           submission.LastSubmittedAt.Value > dueDateTime.Value;

                                var dto = new InstructorSubmissionViewDto
                                {
                                    SubmissionId = submission.SubmissionId,
                                    ProjectId = submission.ProjectId,
                                    ProjectTitle = project.Title,
                                    GroupId = group.GroupId,
                                    GroupName = group.GroupName,
                                    MilestoneDefId = submission.MilestoneDefId,
                                    MilestoneTitle = milestone?.Title,
                                    VersionNo = submission.LastVersionNo ?? 0,
                                    Status = status,
                                    SubmittedAt = submission.LastSubmittedAt,
                                    LastSubmittedAt = submission.LastSubmittedAt,
                                    FileCount = submission.SubmissionFiles?.Count ?? 0,
                                    IsGraded = false,
                                    SubmittedBy = submittedByUser?.FullName,
                                    SubmittedByUserId = submittedByUser?.UserId,
                                    IsLateSubmission = isLate,
                                    CanGrade = true
                                };

                                allSubmissions.Add(dto);
                            }
                        }
                    }
                }
            }

            // Sort by submission date (oldest first for grading priority)
            allSubmissions = allSubmissions
                .OrderBy(s => s.LastSubmittedAt ?? s.SubmittedAt)
                .ToList();

            return new ResultModel<List<InstructorSubmissionViewDto>>
            {
                IsSuccess = true,
                Message = $"Found {allSubmissions.Count} submissions pending grading",
                Data = allSubmissions,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<InstructorSubmissionViewDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving pending submissions: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    private List<InstructorSubmissionViewDto> ApplySorting(
        List<InstructorSubmissionViewDto> submissions, 
        string? sortBy, 
        string? sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "submittedat" => isDescending
                ? submissions.OrderByDescending(s => s.LastSubmittedAt ?? s.SubmittedAt).ToList()
                : submissions.OrderBy(s => s.LastSubmittedAt ?? s.SubmittedAt).ToList(),
            
            "groupname" => isDescending
                ? submissions.OrderByDescending(s => s.GroupName).ToList()
                : submissions.OrderBy(s => s.GroupName).ToList(),
            
            "grade" => isDescending
                ? submissions.OrderByDescending(s => s.Grade).ToList()
                : submissions.OrderBy(s => s.Grade).ToList(),
            
            _ => submissions.OrderByDescending(s => s.LastSubmittedAt ?? s.SubmittedAt).ToList()
        };
    }

    private string FormatFileSize(long? bytes)
    {
        if (bytes == null) return "Unknown";

        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes.Value;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private string GetFileNameFromUrl(string? fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return "unknown";
        
        try
        {
            var uri = new Uri(fileUrl);
            return System.IO.Path.GetFileName(uri.LocalPath);
        }
        catch
        {
            // If URL parsing fails, try to get last segment
            var lastSlash = fileUrl.LastIndexOf('/');
            if (lastSlash >= 0 && lastSlash < fileUrl.Length - 1)
            {
                return fileUrl.Substring(lastSlash + 1);
            }
            return "unknown";
        }
    }

    private string? GetFileExtension(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        
        var extension = System.IO.Path.GetExtension(fileName);
        return string.IsNullOrEmpty(extension) ? null : extension.TrimStart('.');
    }
}
