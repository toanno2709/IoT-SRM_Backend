using AppBackend.BusinessObjects.Dtos.Project;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Services.Services.Project
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IOTShowroomContext _db;


        public ProjectService(IProjectRepository projectRepository, IOTShowroomContext db)
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
                throw new KeyNotFoundException("Group not found.");

            // Kiểm tra người tạo là leader
            if (group.LeaderId != leaderId)
                throw new InvalidOperationException("Only group leader can create project.");

            // Kiểm tra group đã có project chưa
            var existing = await _db.Projects.FirstOrDefaultAsync(p => p.GroupId == dto.GroupId);
            if (existing != null)
                throw new InvalidOperationException("This group already has a project.");

            // Tạo project
            var project = new AppBackend.BusinessObjects.Models.Project
            {
                GroupId = dto.GroupId,
                Title = dto.Title,
                Description = dto.Description,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            // 📨 Gửi thông báo đến instructor
            AppBackend.BusinessObjects.Models.User? instructor = null;
            var instructorId = group.Class?.InstructorId;
            if (instructorId.HasValue)
            {
                instructor = await _db.Users
                    .FirstOrDefaultAsync(u => u.UserId == instructorId.Value && (u.RoleId == 2 || u.RoleId == 3));
            }

            if (instructor != null)
            {
                var note = new Notification
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
            if (project == null) throw new KeyNotFoundException("Project not found.");
            if (project.Group?.LeaderId != dto.RequesterUserId)
                throw new InvalidOperationException("Only group leader can update project.");

            if (!string.IsNullOrWhiteSpace(dto.Title)) project.Title = dto.Title;
            if (dto.Description != null) project.Description = dto.Description;
            project.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        // 3. Get project by group
        public async Task<ProjectDetailDto> GetProjectByGroupAsync(int groupId)
        {
            var project = await _db.Projects
                .Include(p => p.Group!).ThenInclude(g => g.GroupMembers)
                .FirstOrDefaultAsync(p => p.GroupId == groupId);
            if (project == null) throw new KeyNotFoundException("Project not found for this group.");

            return new ProjectDetailDto
            {
                ProjectId = project.ProjectId,
                Title = project.Title,
                Description = project.Description,
                Status = project.Status,
                GroupId = project.GroupId,
                GroupName = project.Group?.GroupName,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                MemberNames = project.Group?.GroupMembers
                    .Select(m => m.User?.FullName ?? $"User#{m.UserId}")
                    .ToList() ?? new List<string>()
            };
        }

        // 4. Get all projects by class
        public async Task<IEnumerable<ProjectListItemDto>> GetProjectsByClassAsync1(int classId)
        {
            var results = await _db.Projects
                .Include(p => p.Group)
                .Where(p => p.Group != null && p.Group.ClassId == classId)
                .Select(p => new ProjectListItemDto
                {
                    ProjectId = p.ProjectId,
                    Title = p.Title!,
                    Status = p.Status!,
                    GroupId = p.GroupId,
                    GroupName = p.Group!.GroupName
                })
                .ToListAsync();

            return results;
        }

        // 5. Change status (Instructor only)
        public async Task ChangeStatusAsync(ProjectStatusDto dto)
        {
            var project = await _db.Projects.Include(p => p.Group).FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);
            if (project == null) throw new KeyNotFoundException("Project not found.");

            // validate instructor role
            var instructor = await _db.Users.FindAsync(dto.InstructorId);
            if (instructor == null || (instructor.RoleId != 2 && instructor.RoleId != 3))
                throw new InvalidOperationException("Only instructor can approve projects.");

            project.Status = dto.Status;
            project.UpdatedAt = DateTime.UtcNow;

            // add to Project_Approval_History
            var history = new ProjectApprovalHistory
            {
                SubmissionId = 0,
                ReviewerId = dto.InstructorId,
                Action = dto.Status.ToUpper(),
                Comment = dto.Comment ?? "",
                ActedAt = DateTime.UtcNow
            };
            _db.ProjectApprovalHistories.Add(history);

            // send notification to leader + members
            var groupMembers = await _db.GroupMembers.Where(gm => gm.GroupId == project.GroupId).ToListAsync();
            foreach (var m in groupMembers)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = m.UserId,
                    Title = $"Project '{project.Title}' status updated",
                    Message = $"Instructor marked project as {dto.Status}.",
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
            var project = await _db.Projects.Include(p => p.Group).FirstOrDefaultAsync(p => p.ProjectId == projectId);
            if (project == null) throw new KeyNotFoundException("Project not found.");

            var requester = await _db.Users.FindAsync(requesterUserId);
            if (requester == null) throw new KeyNotFoundException("Requester not found.");

            var isAdmin = requester.RoleId == 1;
            var isInstructor = requester.RoleId == 2 || requester.RoleId == 3;
            var isLeader = project.Group?.LeaderId == requesterUserId;

            if (!isAdmin && !isInstructor && !isLeader)
                throw new InvalidOperationException("You are not allowed to delete this project.");

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
        }

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
                    // Leader gi? l?y t? Group
                    LeaderId = p.Group?.LeaderId,
                    LeaderName = p.Group?.Leader?.FullName,
                    GroupId = p.GroupId,
                    GroupName = p.Group?.GroupName,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    // Members gi? là GroupMembers
                    MemberCount = p.Group?.GroupMembers?.Count ?? 0,
                    Members = (p.Group?.GroupMembers ?? new List<AppBackend.BusinessObjects.Models.GroupMember>())
                        .Select(gm => new ProjectMemberDto
                        {
                            UserId = gm.UserId,
                            FullName = gm.User?.FullName,
                            Email = gm.User?.Email,
                            RoleInProject = gm.RoleInGroup
                        }).ToList()
                }).ToList();

                return new ResultModel<List<ProjectGroupResponseDto>>
                {
                    IsSuccess = true,
                    Message = "Projects retrieved successfully",
                    Data = dtos
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<List<ProjectGroupResponseDto>>
                {
                    IsSuccess = false,
                    Message = $"Error retrieving projects: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}
