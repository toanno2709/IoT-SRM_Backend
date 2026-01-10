using AppBackend.BusinessObjects.Dtos.Project;
using AppBackend.BusinessObjects.Models;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Data;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.Project
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IotShowroomContext _db;

        public ProjectService(IProjectRepository projectRepository, IotShowroomContext db)
        {
            _projectRepository = projectRepository;
            _db = db;
        }

        // 1. Create project (leader OR instructor of the class)
        public async Task<ProjectCreateResultDto> CreateProjectAsync(ProjectCreateDto dto, int creatorUserId)
        {
            // Kiểm tra group tồn tại
            var group = await _db.Groups
                .Include(g => g.Class)
                .FirstOrDefaultAsync(g => g.GroupId == dto.GroupId);

            if (group == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Group not found",
                    StatusCodes.Status404NotFound
                );

            // Lấy thông tin user
            var user = await _db.Users.FindAsync(creatorUserId);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "User not found",
                    StatusCodes.Status404NotFound
                );

            // Kiểm tra quyền: phải là group leader HOẶC instructor của class
            bool isGroupLeader = group.LeaderId == creatorUserId;
            bool isInstructor = user.RoleId == 2 && group.Class?.InstructorId == creatorUserId;

            if (!isGroupLeader && !isInstructor)
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "Only group leader or class instructor can create project",
                    StatusCodes.Status403Forbidden
                );

            // Check class configuration for project creation deadline (only for students, not instructors)
            if (isGroupLeader && !isInstructor && group.Class != null)
            {
                var classConfig = await _db.ClassConfigurations
                    .FirstOrDefaultAsync(c => c.ClassId == group.Class.ClassId);

                if (classConfig != null && !classConfig.IsProjectCreationAllowed())
                {
                    throw new AppException(
                        CommonMessageConstants.FORBIDDEN,
                        $"Project creation deadline has passed on {classConfig.ProjectCreationDeadline:yyyy-MM-dd HH:mm}. Cannot create new projects.",
                        StatusCodes.Status403Forbidden
                    );
                }
            }

            // Kiểm tra group đã có project chưa
            var existing = await _db.Projects.FirstOrDefaultAsync(p => p.GroupId == dto.GroupId);
            if (existing != null)
                throw new AppException(
                    CommonMessageConstants.EXISTED,
                    "This group already has a project",
                    StatusCodes.Status409Conflict
                );

            // Tạo project
            var project = new AppBackend.BusinessObjects.Models.Project
            {
                GroupId = dto.GroupId,
                Title = dto.Title,
                Description = dto.Description,
                Component = dto.Component,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            // Gửi thông báo đến instructor (chỉ khi được tạo bởi student)
            if (isGroupLeader && !isInstructor)
            {
                AppBackend.BusinessObjects.Models.User? instructor = null;
                var instructorId = group.Class?.InstructorId;
                if (instructorId.HasValue)
                {
                    instructor = await _db.Users
                        .FirstOrDefaultAsync(u => u.UserId == instructorId.Value && u.RoleId == 2);
                }

                if (instructor != null)
                {
                    // Create Data JSON for notification
                    var notificationData = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        classId = group.ClassId,
                        groupId = group.GroupId,
                        projectId = project.ProjectId
                    });

                    var note = new AppBackend.BusinessObjects.Models.Notification
                    {
                        UserId = instructor.UserId,
                        Title = $"New project submitted by group {group.GroupName}",
                        Message = $"Group '{group.GroupName}' has submitted a new project: '{dto.Title}'",
                        Type = "project_submitted",
                        Data = notificationData,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Notifications.Add(note);
                    await _db.SaveChangesAsync();
                }
            }

            return new ProjectCreateResultDto(project.ProjectId, project.Title, project.Status);
        }

        // 2. Update project (leader OR instructor of the class)
        public async Task UpdateProjectAsync(ProjectUpdateDto dto)
        {
            var project = await _db.Projects
                .Include(p => p.Group)
                    .ThenInclude(g => g!.Class)
                .FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);

            if (project == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );

            // Lấy thông tin user
            var user = await _db.Users.FindAsync(dto.RequesterUserId);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "User not found",
                    StatusCodes.Status404NotFound
                );

            // Kiểm tra quyền: phải là group leader HOẶC instructor của class
            bool isGroupLeader = project.Group?.LeaderId == dto.RequesterUserId;
            bool isInstructor = user.RoleId == 2 && project.Group?.Class?.InstructorId == dto.RequesterUserId;

            if (!isGroupLeader && !isInstructor)
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "Only group leader or class instructor can update project",
                    StatusCodes.Status403Forbidden
                );

            // Cập nhật project
            if (!string.IsNullOrWhiteSpace(dto.Title)) project.Title = dto.Title;
            if (dto.Description != null) project.Description = dto.Description;
            if (dto.Component != null) project.Component = dto.Component;
            project.UpdatedAt = DateTime.UtcNow;

            _db.Projects.Update(project);
            await _db.SaveChangesAsync();
        }

        // 3. Get projects by group (returns list)
        public async Task<ResultModel<List<ProjectDetailDto>>> GetProjectsByGroupAsync(int groupId)
        {
            try
            {
                var projects = await _db.Projects
                    .Include(p => p.Group!).ThenInclude(g => g.GroupMembers).ThenInclude(gm => gm.User)
                    .Where(p => p.GroupId == groupId)
                    .ToListAsync();

                if (!projects.Any())
                {
                    return new ResultModel<List<ProjectDetailDto>>
                    {
                        IsSuccess = true,
                        ResponseCode = CommonMessageConstants.SUCCESS,
                        Message = "No projects found for this group",
                        Data = new List<ProjectDetailDto>(),
                        StatusCode = StatusCodes.Status200OK
                    };
                }

                var projectDtos = projects.Select(project => new ProjectDetailDto
                {
                    ProjectId = project.ProjectId,
                    Title = project.Title,
                    Description = project.Description,
                    Component = project.Component,
                    Status = project.Status,
                    GroupId = project.GroupId ?? 0,
                    GroupName = project.Group?.GroupName,
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt,
                    MemberNames = project.Group?.GroupMembers
                        .Select(m => m.User?.FullName ?? $"User#{m.UserId}")
                        .ToList() ?? new List<string>()
                }).ToList();

                return new ResultModel<List<ProjectDetailDto>>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Projects retrieved successfully",
                    Data = projectDtos,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<List<ProjectDetailDto>>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Error retrieving projects: {ex.Message}",
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        // 4. Change status (Instructor only)
        public async Task ChangeStatusAsync(ProjectStatusDto dto)
        {
            try
            {
                var project = await _db.Projects
                    .Include(p => p.Group)
                        .ThenInclude(g => g!.GroupMembers)
                            .ThenInclude(gm => gm.User)
                    .FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);

                if (project == null)
                    throw new AppException(
                        CommonMessageConstants.NOT_FOUND,
                        "Project not found",
                        StatusCodes.Status404NotFound
                    );

                // Validate instructor role
                var instructor = await _db.Users.FindAsync(dto.InstructorId);
                if (instructor == null || instructor.RoleId != 2)
                    throw new AppException(
                        CommonMessageConstants.FORBIDDEN,
                        "Only instructor can change project status",
                        StatusCodes.Status403Forbidden
                    );

                project.Status = dto.Status;
                project.UpdatedAt = DateTime.UtcNow;

                // NOTE: We don't create ProjectApprovalHistory here because:
                // - ProjectApprovalHistory is for milestone submission approvals (requires valid SubmissionId)
                // - Project status changes are tracked via notifications sent to group members
                // - Using SubmissionId = 0 causes foreign key constraint violations

                // Send notification to leader + members
                var groupMembers = project.Group?.GroupMembers?.ToList() ?? new List<GroupMember>();
                
                // Create Data JSON for notification
                var notificationData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    classId = project.Group?.ClassId,
                    groupId = project.GroupId,
                    projectId = project.ProjectId
                });

                foreach (var m in groupMembers)
                {
                    _db.Notifications.Add(new AppBackend.BusinessObjects.Models.Notification
                    {
                        UserId = m.UserId,
                        Title = $"Project '{project.Title}' status updated",
                        Message = $"Instructor marked project as {dto.Status}. {(string.IsNullOrEmpty(dto.Comment) ? "" : "Comment: " + dto.Comment)}",
                        Type = "project_status",
                        Data = notificationData,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    $"Error updating project status: {ex.Message}",
                    StatusCodes.Status500InternalServerError
                );
            }
        }

        // 6. Delete project (Admin, Instructor, or Leader)
        public async Task DeleteProjectAsync(int projectId, int requesterUserId)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var project = await _db.Projects
                    .Include(p => p.Group)
                    .Include(p => p.MilestoneEvaluations)
                    .Include(p => p.MilestoneSubmissions)
                        .ThenInclude(ms => ms.SubmissionFiles)
                    .Include(p => p.MilestoneSubmissions)
                        .ThenInclude(ms => ms.ProjectApprovalHistories)
                    .Include(p => p.ProjectMilestones)
                    .Include(p => p.Sensors)
                        .ThenInclude(s => s.SensorData)
                    .Include(p => p.LiveDemos)
                        .ThenInclude(ld => ld.LiveDemoSensors)
                    .Include(p => p.FinalProjectSubmission)
                        .ThenInclude(fps => fps.FinalSubmissionGrades)
                    .Include(p => p.HallOfFames)
                    .Include(p => p.ProjectTemplateRegistrations)
                    .Include(p => p.Simulations)
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                if (project == null)
                    throw new AppException(
                        CommonMessageConstants.NOT_FOUND,
                        "Project not found",
                        StatusCodes.Status404NotFound
                    );

                var requester = await _db.Users.FindAsync(requesterUserId);
                if (requester == null)
                    throw new AppException(
                        CommonMessageConstants.NOT_FOUND,
                        "Requester not found",
                        StatusCodes.Status404NotFound
                    );

                var isAdmin = requester.RoleId == 1;
                var isInstructor = requester.RoleId == 2;
                var isLeader = project.Group?.LeaderId == requesterUserId;

                if (!isAdmin && !isInstructor && !isLeader)
                    throw new AppException(
                        CommonMessageConstants.FORBIDDEN,
                        "You are not allowed to delete this project",
                        StatusCodes.Status403Forbidden
                    );

                // Delete related entities in correct order to avoid FK constraints

                // 1. Delete Hall of Fame entries
                if (project.HallOfFames.Any())
                {
                    _db.HallOfFames.RemoveRange(project.HallOfFames);
                }

                // 2. Delete Live Demo Sensors first, then Live Demos
                if (project.LiveDemos.Any())
                {
                    foreach (var liveDemo in project.LiveDemos)
                    {
                        if (liveDemo.LiveDemoSensors.Any())
                        {
                            _db.LiveDemoSensors.RemoveRange(liveDemo.LiveDemoSensors);
                        }
                    }
                    _db.LiveDemos.RemoveRange(project.LiveDemos);
                }

                // 3. Delete Sensor Data first, then Sensors
                if (project.Sensors.Any())
                {
                    foreach (var sensor in project.Sensors)
                    {
                        if (sensor.SensorData.Any())
                        {
                            _db.SensorData.RemoveRange(sensor.SensorData);
                        }
                    }
                    _db.Sensors.RemoveRange(project.Sensors);
                }

                // 4. Delete Simulations
                if (project.Simulations.Any())
                {
                    _db.Simulations.RemoveRange(project.Simulations);
                }

                // 5. Delete Final Project Submission and its grades
                if (project.FinalProjectSubmission != null)
                {
                    if (project.FinalProjectSubmission.FinalSubmissionGrades.Any())
                    {
                        _db.FinalSubmissionGrades.RemoveRange(project.FinalProjectSubmission.FinalSubmissionGrades);
                    }
                    _db.FinalProjectSubmissions.Remove(project.FinalProjectSubmission);
                }

                // 6. Delete Milestone Submissions and related data
                if (project.MilestoneSubmissions.Any())
                {
                    foreach (var submission in project.MilestoneSubmissions)
                    {
                        // Delete approval histories
                        if (submission.ProjectApprovalHistories.Any())
                        {
                            _db.ProjectApprovalHistories.RemoveRange(submission.ProjectApprovalHistories);
                        }
                        
                        // Delete submission files
                        if (submission.SubmissionFiles.Any())
                        {
                            _db.SubmissionFiles.RemoveRange(submission.SubmissionFiles);
                        }
                    }
                    _db.MilestoneSubmissions.RemoveRange(project.MilestoneSubmissions);
                }

                // 7. Delete Milestone Evaluations
                if (project.MilestoneEvaluations.Any())
                {
                    _db.MilestoneEvaluations.RemoveRange(project.MilestoneEvaluations);
                }

                // 8. Delete Project Milestones
                if (project.ProjectMilestones.Any())
                {
                    _db.ProjectMilestones.RemoveRange(project.ProjectMilestones);
                }

                // 9. Delete Project Template Registrations
                if (project.ProjectTemplateRegistrations.Any())
                {
                    _db.ProjectTemplateRegistrations.RemoveRange(project.ProjectTemplateRegistrations);
                }

                // 10. Finally, delete the project itself
                _db.Projects.Remove(project);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (AppException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    $"Database error while deleting project: {ex.InnerException?.Message ?? ex.Message}",
                    StatusCodes.Status500InternalServerError
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    $"Error deleting project: {ex.Message}",
                    StatusCodes.Status500InternalServerError
                );
            }
        }

        // 7. Get projects by class (with full details)
        public async Task<ResultModel<List<ProjectGroupResponseDto>>> GetProjectsByClassAsync(int classId)
        {
            try
            {
                var projects = await _projectRepository.GetProjectsByClassAsync(classId);

                var dtos = projects.Select(p => new ProjectGroupResponseDto
                {
                    ProjectId = p.ProjectId,
                    Title = p.Title,
                    Description = p.Description,
                    Component = p.Component,
                    Status = p.Status,
                    LeaderId = p.Group?.LeaderId,
                    LeaderName = p.Group?.Leader?.FullName,
                    GroupId = p.GroupId ?? 0,
                    GroupName = p.Group?.GroupName,
                    ClassId = p.Group?.ClassId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    MemberCount = p.Group?.GroupMembers?.Count ?? 0,
                    Members = (p.Group?.GroupMembers ?? new List<GroupMember>())
                        .Select(gm => new ProjectMemberDto
                        {
                            UserId = gm.UserId,
                            FullName = gm.User?.FullName,
                            Email = gm.User?.Email,
                            AvatarUrl = gm.User?.AvatarUrl,
                            RoleInProject = gm.RoleInGroup
                        }).ToList()
                }).ToList();

                return new ResultModel<List<ProjectGroupResponseDto>>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Projects retrieved successfully",
                    Data = dtos,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<List<ProjectGroupResponseDto>>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Error retrieving projects: {ex.Message}",
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        // 8. Update project status with comment (Instructor only) - NEW
        public async Task<ResultModel<UpdateProjectStatusResponseDto>> UpdateProjectStatusAsync(
            int projectId, 
            UpdateProjectStatusRequestDto request, 
            int instructorId)
        {
            try
            {
                // 1. Validate project exists
                var project = await _db.Projects
                    .Include(p => p.Group)
                        .ThenInclude(g => g!.GroupMembers)
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                if (project == null)
                {
                    return new ResultModel<UpdateProjectStatusResponseDto>
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = "Project not found",
                        Data = null,
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                // 2. Validate instructor
                var instructor = await _db.Users.FindAsync(instructorId);
                if (instructor == null || instructor.RoleId != 2)
                {
                    return new ResultModel<UpdateProjectStatusResponseDto>
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.FORBIDDEN,
                        Message = "Only instructors can update project status",
                        Data = null,
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                }

                // 3. Update project status
                var oldStatus = project.Status;
                project.Status = request.Status;
                project.UpdatedAt = DateTime.UtcNow;

                // NOTE: We don't create ProjectApprovalHistory here because:
                // - ProjectApprovalHistory requires a valid SubmissionId (foreign key to MilestoneSubmission)
                // - Project status updates are for the entire project, not specific milestone submissions
                // - Status changes are tracked via notifications and can be viewed in project history
                // - Using SubmissionId = 0 causes database constraint violations

                // 4. Send notifications to all group members
                var groupMembers = project.Group?.GroupMembers?.ToList() ?? new List<GroupMember>();
                
                // Create Data JSON for notification
                var notificationData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    classId = project.Group?.ClassId,
                    groupId = project.GroupId,
                    projectId = project.ProjectId
                });

                foreach (var member in groupMembers)
                {
                    var notification = new AppBackend.BusinessObjects.Models.Notification
                    {
                        UserId = member.UserId,
                        Title = $"Project Status Updated: {project.Title}",
                        Message = $"Instructor {instructor.FullName} changed project status from '{oldStatus}' to '{request.Status}'. " +
                                  $"{(!string.IsNullOrEmpty(request.Comment) ? $"Comment: {request.Comment}" : "")}",
                        Type = "project_status_update",
                        Data = notificationData,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Notifications.Add(notification);
                }

                // 5. Save all changes
                await _db.SaveChangesAsync();

                // 6. Return response
                return new ResultModel<UpdateProjectStatusResponseDto>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Project status updated successfully",
                    Data = new UpdateProjectStatusResponseDto
                    {
                        ProjectId = project.ProjectId,
                        ProjectTitle = project.Title,
                        GroupId = project.GroupId,
                        GroupName = project.Group?.GroupName,
                        Status = request.Status,
                        Comment = request.Comment,
                        ReviewerId = instructorId,
                        ReviewerName = instructor.FullName,
                        ReviewedAt = DateTime.UtcNow
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<UpdateProjectStatusResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Error updating project status: {ex.Message}",
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        // 9. Get project status history (for students to view) - NEW
        public async Task<ResultModel<List<ProjectStatusHistoryDto>>> GetProjectStatusHistoryAsync(int projectId)
        {
            try
            {
                // 1. Validate project exists
                var projectExists = await _db.Projects.AnyAsync(p => p.ProjectId == projectId);
                if (!projectExists)
                {
                    return new ResultModel<List<ProjectStatusHistoryDto>>
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = "Project not found",
                        Data = null,
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                // NOTE: ProjectApprovalHistory is for milestone submission approvals, not project status changes
                // Project status changes are tracked via notifications
                // To get status history, we query notifications of type "project_status" or "project_status_update"
                
                var project = await _db.Projects
                    .Include(p => p.Group)
                        .ThenInclude(g => g!.GroupMembers)
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                if (project == null || project.Group?.GroupMembers == null || !project.Group.GroupMembers.Any())
                {
                    return new ResultModel<List<ProjectStatusHistoryDto>>
                    {
                        IsSuccess = true,
                        ResponseCode = CommonMessageConstants.SUCCESS,
                        Message = "No status history available",
                        Data = new List<ProjectStatusHistoryDto>(),
                        StatusCode = StatusCodes.Status200OK
                    };
                }

                // Get notifications related to project status updates for group members
                var memberIds = project.Group.GroupMembers.Select(gm => gm.UserId).ToList();
                var statusNotifications = await _db.Notifications
                    .Where(n => n.UserId.HasValue && 
                               memberIds.Contains(n.UserId.Value) &&
                               (n.Type == "project_status" || n.Type == "project_status_update") &&
                               n.Title != null && n.Title.Contains(project.Title ?? ""))
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                // Extract status history from notifications
                var historyDtos = statusNotifications
                    .Select(n => new ProjectStatusHistoryDto
                    {
                        HistoryId = n.NotificationId,
                        Status = ExtractStatusFromNotification(n.Message),
                        Comment = ExtractCommentFromNotification(n.Message),
                        ReviewerId = 0, // We don't have reviewer ID from notifications, use 0 as placeholder
                        ReviewerName = "Instructor",
                        ReviewedAt = n.CreatedAt ?? DateTime.UtcNow
                    })
                    .DistinctBy(h => new { h.Status, h.ReviewedAt })
                    .ToList();

                return new ResultModel<List<ProjectStatusHistoryDto>>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = historyDtos.Count > 0 
                        ? $"Retrieved {historyDtos.Count} status history records for project {projectId}"
                        : "No status history found for this project",
                    Data = historyDtos,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<List<ProjectStatusHistoryDto>>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Error retrieving status history: {ex.Message}",
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        // Helper method to extract status from notification message
        private string ExtractStatusFromNotification(string? message)
        {
            if (string.IsNullOrEmpty(message)) return "Unknown";

            // Pattern: "marked project as {Status}" or "changed project status from 'X' to '{Status}'"
            if (message.Contains("marked project as"))
            {
                var startIndex = message.IndexOf("marked project as") + "marked project as".Length;
                var endIndex = message.IndexOf(".", startIndex);
                if (endIndex == -1) endIndex = message.IndexOf("Comment:", startIndex);
                if (endIndex == -1) endIndex = message.Length;
                return message.Substring(startIndex, endIndex - startIndex).Trim();
            }
            else if (message.Contains("to '") && message.Contains("'. "))
            {
                var startIndex = message.IndexOf("to '") + 4;
                var endIndex = message.IndexOf("'", startIndex);
                if (endIndex > startIndex)
                    return message.Substring(startIndex, endIndex - startIndex).Trim();
            }

            return "Unknown";
        }

        // Helper method to extract comment from notification message
        private string? ExtractCommentFromNotification(string? message)
        {
            if (string.IsNullOrEmpty(message)) return null;

            // Pattern: "Comment: {comment}"
            var commentIndex = message.IndexOf("Comment:");
            if (commentIndex >= 0)
            {
                return message.Substring(commentIndex + "Comment:".Length).Trim();
            }

            return null;
        }
    }
}
