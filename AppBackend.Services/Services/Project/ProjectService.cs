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

        // 1. Create project (leader only, group must not already have one)
        public async Task<ProjectCreateResultDto> CreateProjectAsync(ProjectCreateDto dto, int leaderId)
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

            // Kiểm tra người tạo là leader
            if (group.LeaderId != leaderId)
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "Only group leader can create project",
                    StatusCodes.Status403Forbidden
                );

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

            // Gửi thông báo đến instructor
            AppBackend.BusinessObjects.Models.User? instructor = null;
            var instructorId = group.Class?.InstructorId;
            if (instructorId.HasValue)
            {
                instructor = await _db.Users
                    .FirstOrDefaultAsync(u => u.UserId == instructorId.Value && u.RoleId == 2);
            }

            if (instructor != null)
            {
                var note = new AppBackend.BusinessObjects.Models.Notification
                {
                    UserId = instructor.UserId,
                    Title = $"New project submitted by group {group.GroupName}",
                    Message = $"Group '{group.GroupName}' in class {group.Class?.ClassName ?? group.ClassId.ToString()} has created a new project: '{project.Title}'.",
                    Type = "project_created",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Notifications.Add(note);
                await _db.SaveChangesAsync();
            }

            return new ProjectCreateResultDto(project.ProjectId, project.Title ?? string.Empty, project.Status);
        }

        // 2. Update project (leader only)
        public async Task UpdateProjectAsync(ProjectUpdateDto dto)
        {
            var project = await _db.Projects
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);

            if (project == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Project not found",
                    StatusCodes.Status404NotFound
                );

            if (project.Group?.LeaderId != dto.RequesterUserId)
                throw new AppException(
                    CommonMessageConstants.FORBIDDEN,
                    "Only group leader can update project",
                    StatusCodes.Status403Forbidden
                );

            if (!string.IsNullOrWhiteSpace(dto.Title)) project.Title = dto.Title;
            if (dto.Description != null) project.Description = dto.Description;
            if (dto.Component != null) project.Component = dto.Component;
            project.UpdatedAt = DateTime.UtcNow;

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
            var project = await _db.Projects
                .Include(p => p.Group).ThenInclude(g => g!.GroupMembers)
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

            // Add to Project_Approval_History
            var history = new ProjectApprovalHistory
            {
                SubmissionId = 0,
                ReviewerId = dto.InstructorId,
                Action = dto.Status.ToUpper(),
                Comment = dto.Comment ?? "",
                ActedAt = DateTime.UtcNow
            };
            _db.ProjectApprovalHistories.Add(history);

            // Send notification to leader + members
            var groupMembers = project.Group?.GroupMembers?.ToList() ?? new List<GroupMember>();
            foreach (var m in groupMembers)
            {
                _db.Notifications.Add(new AppBackend.BusinessObjects.Models.Notification
                {
                    UserId = m.UserId,
                    Title = $"Project '{project.Title}' status updated",
                    Message = $"Instructor marked project as {dto.Status}. {(string.IsNullOrEmpty(dto.Comment) ? "" : "Comment: " + dto.Comment)}",
                    Type = "project_status",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

        // 6. Delete project (Admin, Instructor, or Leader)
        public async Task DeleteProjectAsync(int projectId, int requesterUserId)
        {
            var project = await _db.Projects
                .Include(p => p.Group)
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

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
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

                // 4. Create approval history record (students can view this)
                var history = new ProjectApprovalHistory
                {
                    SubmissionId = 0, // 0 means general project status update
                    ReviewerId = instructorId,
                    Action = request.Status,
                    Comment = request.Comment ?? "",
                    ActedAt = DateTime.UtcNow
                };
                _db.ProjectApprovalHistories.Add(history);

                // 5. Send notifications to all group members
                var groupMembers = project.Group?.GroupMembers?.ToList() ?? new List<GroupMember>();
                foreach (var member in groupMembers)
                {
                    var notification = new AppBackend.BusinessObjects.Models.Notification
                    {
                        UserId = member.UserId,
                        Title = $"Project Status Updated: {project.Title}",
                        Message = $"Instructor {instructor.FullName} changed project status from '{oldStatus}' to '{request.Status}'. " +
                                  $"{(!string.IsNullOrEmpty(request.Comment) ? $"Comment: {request.Comment}" : "")}",
                        Type = "project_status_update",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Notifications.Add(notification);
                }

                // 6. Save all changes
                await _db.SaveChangesAsync();

                // 7. Return response
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

                // Get approval history records for this project
                var historyRecords = await _db.ProjectApprovalHistories
                    .Include(h => h.Reviewer)
                    .Include(h => h.Submission)
                        .ThenInclude(s => s.MilestoneDef)
                    .Where(h => h.Submission.ProjectId == projectId)
                    .OrderByDescending(h => h.ActedAt)
                    .ToListAsync();

                // Map to DTOs
                var historyDtos = historyRecords.Select(h => new ProjectStatusHistoryDto
                {
                    HistoryId = h.HistoryId,
                    Status = h.Action ?? "Unknown",
                    Comment = h.Comment,
                    ReviewerId = h.ReviewerId,
                    ReviewerName = h.Reviewer?.FullName,
                    ReviewedAt = h.ActedAt ?? DateTime.UtcNow
                }).ToList();

                return new ResultModel<List<ProjectStatusHistoryDto>>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = historyDtos.Count > 0 
                        ? $"Retrieved {historyDtos.Count} status history records for project {projectId}"
                        : "No approval history found for this project",
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
    }
}
