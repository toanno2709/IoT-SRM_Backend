using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.ProjectTemplateRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.ProjectTemplate;

public class ProjectTemplateService : IProjectTemplateService
{
    private readonly IProjectTemplateRepository _templateRepo;
    private readonly IotShowroomContext _context;

    public ProjectTemplateService(
        IProjectTemplateRepository templateRepo,
        IotShowroomContext context)
    {
        _templateRepo = templateRepo;
        _context = context;
    }

    /// <summary>
    /// Helper method to truncate DateTime to seconds precision (to match database Precision(0))
    /// </summary>
    private static DateTime TruncateToSeconds(DateTime dateTime)
    {
        return new DateTime(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            dateTime.Minute,
            dateTime.Second,
            dateTime.Kind
        );
    }

    #region Instructor APIs

    public async Task<ResultModel<ProjectTemplateResponseDto>> CreateTemplateAsync(CreateProjectTemplateDto dto, int instructorId)
    {
        try
        {
            // Validate class and instructor
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.ClassId == dto.ClassId && c.InstructorId == instructorId);

            if (classEntity == null)
            {
                return new ResultModel<ProjectTemplateResponseDto>
                {
                    IsSuccess = false,
                    Message = "Class not found or you are not the instructor",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var createdAt = TruncateToSeconds(DateTime.UtcNow);

            // Create template
            var template = new BusinessObjects.Models.ProjectTemplate
            {
                ClassId = dto.ClassId,
                Title = dto.Title,
                Description = dto.Description,
                Component = dto.Component,
                MaxGroups = dto.MaxGroups,
                RegisteredCount = 0,
                IsActive = true,
                CreatedBy = instructorId,
                CreatedAt = createdAt
            };

            await _templateRepo.CreateAsync(template);

            // Create milestones
            if (dto.Milestones.Any())
            {
                foreach (var milestoneDto in dto.Milestones)
                {
                    var milestone = new TemplateMilestone
                    {
                        TemplateId = template.TemplateId,
                        Title = milestoneDto.Title,
                        Description = milestoneDto.Description,
                        OrderIndex = milestoneDto.OrderIndex,
                        Weight = milestoneDto.Weight,
                        DaysDuration = milestoneDto.DaysDuration,
                        CreatedAt = createdAt
                    };
                    _context.TemplateMilestones.Add(milestone);
                }
                await _context.SaveChangesAsync();
            }

            // Reload with details
            var created = await _templateRepo.GetByIdWithDetailsAsync(template.TemplateId);

            return new ResultModel<ProjectTemplateResponseDto>
            {
                IsSuccess = true,
                Message = "Template created successfully",
                Data = MapToResponseDto(created!),
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ProjectTemplateResponseDto>
            {
                IsSuccess = false,
                Message = $"Error creating template: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<ProjectTemplateResponseDto>> GetTemplateByIdAsync(int templateId, int instructorId)
    {
        try
        {
            var template = await _templateRepo.GetByIdWithDetailsAsync(templateId);
            if (template == null)
            {
                return new ResultModel<ProjectTemplateResponseDto>
                {
                    IsSuccess = false,
                    Message = "Template not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check authorization
            if (template.Class.InstructorId != instructorId)
            {
                return new ResultModel<ProjectTemplateResponseDto>
                {
                    IsSuccess = false,
                    Message = "You are not authorized to view this template",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            return new ResultModel<ProjectTemplateResponseDto>
            {
                IsSuccess = true,
                Data = MapToResponseDto(template),
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ProjectTemplateResponseDto>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<List<ProjectTemplateResponseDto>>> GetTemplatesByClassIdAsync(int classId, int instructorId)
    {
        try
        {
            // Validate authorization
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.ClassId == classId && c.InstructorId == instructorId);

            if (classEntity == null)
            {
                return new ResultModel<List<ProjectTemplateResponseDto>>
                {
                    IsSuccess = false,
                    Message = "Class not found or unauthorized",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var templates = await _templateRepo.GetByClassIdAsync(classId);

            return new ResultModel<List<ProjectTemplateResponseDto>>
            {
                IsSuccess = true,
                Data = templates.Select(MapToResponseDto).ToList(),
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<ProjectTemplateResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<ProjectTemplateResponseDto>> UpdateTemplateAsync(int templateId, UpdateProjectTemplateDto dto, int instructorId)
    {
        try
        {
            var template = await _templateRepo.GetByIdWithDetailsAsync(templateId);
            if (template == null)
            {
                return new ResultModel<ProjectTemplateResponseDto>
                {
                    IsSuccess = false,
                    Message = "Template not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (template.Class.InstructorId != instructorId)
            {
                return new ResultModel<ProjectTemplateResponseDto>
                {
                    IsSuccess = false,
                    Message = "Unauthorized",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Update fields
            if (dto.Title != null) template.Title = dto.Title;
            if (dto.Description != null) template.Description = dto.Description;
            if (dto.Component != null) template.Component = dto.Component;
            if (dto.MaxGroups.HasValue) template.MaxGroups = dto.MaxGroups;
            if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;

            await _templateRepo.UpdateAsync(template);

            return new ResultModel<ProjectTemplateResponseDto>
            {
                IsSuccess = true,
                Message = "Template updated successfully",
                Data = MapToResponseDto(template),
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ProjectTemplateResponseDto>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<bool>> DeleteTemplateAsync(int templateId, int instructorId)
    {
        try
        {
            var template = await _templateRepo.GetByIdWithDetailsAsync(templateId);
            if (template == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "Template not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (template.Class.InstructorId != instructorId)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "Unauthorized",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Check if any groups registered
            if (template.RegisteredCount > 0)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "Cannot delete template with active registrations",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            await _templateRepo.DeleteAsync(templateId);

            return new ResultModel<bool>
            {
                IsSuccess = true,
                Message = "Template deleted successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<List<TemplateRegistrationListDto>>> GetTemplateRegistrationsAsync(int templateId, int instructorId)
    {
        try
        {
            var template = await _templateRepo.GetByIdWithDetailsAsync(templateId);
            if (template == null || template.Class.InstructorId != instructorId)
            {
                return new ResultModel<List<TemplateRegistrationListDto>>
                {
                    IsSuccess = false,
                    Message = "Template not found or unauthorized",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var registrations = await _templateRepo.GetRegistrationsByTemplateIdAsync(templateId);

            var result = registrations.Select(r => new TemplateRegistrationListDto
            {
                RegistrationId = r.RegistrationId,
                GroupId = r.GroupId,
                GroupName = r.Group?.GroupName ?? "Unknown",
                ProjectId = r.ProjectId,
                ProjectTitle = r.Project?.Title,
                Status = r.Status,
                RegisteredAt = r.RegisteredAt ?? TruncateToSeconds(DateTime.UtcNow),
                RegisteredByName = r.RegisteredByUser?.FullName ?? "Unknown"
            }).ToList();

            return new ResultModel<List<TemplateRegistrationListDto>>
            {
                IsSuccess = true,
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<TemplateRegistrationListDto>>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<TemplateStatisticsDto>> GetTemplateStatisticsAsync(int templateId, int instructorId)
    {
        try
        {
            var template = await _templateRepo.GetByIdWithDetailsAsync(templateId);
            if (template == null || template.Class.InstructorId != instructorId)
            {
                return new ResultModel<TemplateStatisticsDto>
                {
                    IsSuccess = false,
                    Message = "Template not found or unauthorized",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var activeCount = template.ProjectTemplateRegistrations.Count(r => r.Status == "Active");
            var cancelledCount = template.ProjectTemplateRegistrations.Count(r => r.Status == "Cancelled");

            var stats = new TemplateStatisticsDto
            {
                TemplateId = template.TemplateId,
                Title = template.Title,
                MaxGroups = template.MaxGroups,
                RegisteredCount = template.RegisteredCount,
                AvailableSlots = template.MaxGroups.HasValue ? template.MaxGroups.Value - template.RegisteredCount : null,
                ActiveRegistrations = activeCount,
                CancelledRegistrations = cancelledCount,
                IsFull = template.MaxGroups.HasValue && template.RegisteredCount >= template.MaxGroups.Value
            };

            return new ResultModel<TemplateStatisticsDto>
            {
                IsSuccess = true,
                Data = stats,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<TemplateStatisticsDto>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<bool>> CancelRegistrationAsync(int registrationId, int studentId)
    {
        try
        {
            var registration = await _templateRepo.GetRegistrationByIdAsync(registrationId);
            if (registration == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "Registration not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check authorization
            var group = await _context.Groups.FindAsync(registration.GroupId);
            if (group?.LeaderId != studentId)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "Only group leader can cancel registration",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Check if project has submissions
            if (registration.ProjectId.HasValue)
            {
                var hasSubmissions = await _context.MilestoneSubmissions
                    .AnyAsync(s => s.ProjectId == registration.ProjectId.Value);

                if (hasSubmissions)
                {
                    return new ResultModel<bool>
                    {
                        IsSuccess = false,
                        Message = "Cannot cancel registration after project has submissions",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
            }

            await _templateRepo.CancelRegistrationAsync(registrationId);

            return new ResultModel<bool>
            {
                IsSuccess = true,
                Message = "Registration cancelled successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    #endregion

    #region Student APIs

    public async Task<ResultModel<List<AvailableTemplateDto>>> GetAvailableTemplatesAsync(int classId, int studentId)
    {
        try
        {
            // Check if student is enrolled in class
            var isEnrolled = await _context.ClassEnrollments
                .AnyAsync(e => e.ClassId == classId && e.StudentId == studentId);

            if (!isEnrolled)
            {
                return new ResultModel<List<AvailableTemplateDto>>
                {
                    IsSuccess = false,
                    Message = "You are not enrolled in this class",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Get student's group in this class
            var studentGroup = await _context.GroupMembers
                .Include(gm => gm.Group)
                .Where(gm => gm.UserId == studentId && gm.Group.ClassId == classId)
                .Select(gm => gm.Group)
                .FirstOrDefaultAsync();

            // Changed: Use GetByClassIdAsync to get ALL templates (not just active ones)
            var templates = await _templateRepo.GetByClassIdAsync(classId);

            var result = new List<AvailableTemplateDto>();
            foreach (var template in templates)
            {
                bool isMyGroupRegistered = false;
                int? myRegistrationId = null;
                
                if (studentGroup != null)
                {
                    // Check if group is registered and get registration ID
                    var registration = await _context.ProjectTemplateRegistrations
                        .Where(r => r.GroupId == studentGroup.GroupId 
                                 && r.TemplateId == template.TemplateId 
                                 && r.Status == "Active")
                        .FirstOrDefaultAsync();
                    
                    if (registration != null)
                    {
                        isMyGroupRegistered = true;
                        myRegistrationId = registration.RegistrationId;
                    }
                }

                var dto = new AvailableTemplateDto
                {
                    TemplateId = template.TemplateId,
                    Title = template.Title,
                    Description = template.Description,
                    Component = template.Component,
                    MaxGroups = template.MaxGroups,
                    RegisteredCount = template.RegisteredCount,
                    AvailableSlots = template.MaxGroups.HasValue ? template.MaxGroups.Value - template.RegisteredCount : null,
                    // Updated: CanRegister now considers IsActive, available slots, group exists, and not already registered
                    CanRegister = template.IsActive && 
                                  (template.MaxGroups == null || template.RegisteredCount < template.MaxGroups) &&
                                  studentGroup != null && !isMyGroupRegistered,
                    IsMyGroupRegistered = isMyGroupRegistered,
                    MyRegistrationId = myRegistrationId,
                    MilestoneCount = template.TemplateMilestones.Count,
                    Milestones = template.TemplateMilestones.Select(m => new TemplateMilestoneDto
                    {
                        TemplateMilestoneId = m.TemplateMilestoneId,
                        Title = m.Title,
                        Description = m.Description,
                        OrderIndex = m.OrderIndex,
                        Weight = m.Weight,
                        DaysDuration = m.DaysDuration
                    }).OrderBy(m => m.OrderIndex).ToList()
                };

                result.Add(dto);
            }

            return new ResultModel<List<AvailableTemplateDto>>
            {
                IsSuccess = true,
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<AvailableTemplateDto>>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}. Inner: {ex.InnerException?.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<TemplateRegistrationResponseDto>> RegisterToTemplateAsync(RegisterTemplateDto dto, int studentId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Validate template
            var template = await _templateRepo.GetByIdWithDetailsAsync(dto.TemplateId);
            if (template == null || !template.IsActive)
            {
                return new ResultModel<TemplateRegistrationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Template not found or inactive",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // 2. Check available slots
            if (!await _templateRepo.HasAvailableSlotsAsync(dto.TemplateId))
            {
                return new ResultModel<TemplateRegistrationResponseDto>
                {
                    IsSuccess = false,
                    Message = "No available slots for this template",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // 3. Validate group and student authorization
            var group = await _context.Groups
                .Include(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.GroupId == dto.GroupId);

            if (group == null)
            {
                return new ResultModel<TemplateRegistrationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Group not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check if student is group leader
            if (group.LeaderId != studentId)
            {
                return new ResultModel<TemplateRegistrationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Only group leader can register template",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // 4. Check if group already has a project
            var existingProject = await _context.Projects
                .FirstOrDefaultAsync(p => p.GroupId == dto.GroupId);

            if (existingProject != null)
            {
                return new ResultModel<TemplateRegistrationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Group already has a project",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // 5. Check if group already registered this template
            var existingReg = await _templateRepo.GetRegistrationByGroupAndTemplateAsync(dto.GroupId, dto.TemplateId);
            if (existingReg != null)
            {
                return new ResultModel<TemplateRegistrationResponseDto>
                {
                    IsSuccess = false,
                    Message = "Group already registered to this template",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var now = TruncateToSeconds(DateTime.UtcNow);

            // 6. AUTO CREATE PROJECT from template
            var project = new BusinessObjects.Models.Project
            {
                GroupId = dto.GroupId,
                Title = template.Title,
                Description = template.Description,
                Component = template.Component,
                Status = "In Progress",
                CreatedAt = now
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // 7. AUTO CREATE MILESTONES from template
            var milestones = template.TemplateMilestones.OrderBy(m => m.OrderIndex).ToList();
            DateTime currentDueDate = DateTime.UtcNow;
            
            foreach (var templateMilestone in milestones)
            {
                // Calculate due date based on days_duration
                if (templateMilestone.DaysDuration.HasValue)
                {
                    currentDueDate = currentDueDate.AddDays(templateMilestone.DaysDuration.Value);
                }

                var milestone = new BusinessObjects.Models.ProjectMilestone
                {
                    ProjectId = project.ProjectId,
                    Title = templateMilestone.Title,
                    Description = templateMilestone.Description,
                    DueDate = DateOnly.FromDateTime(currentDueDate),
                    Weight = templateMilestone.Weight,
                    Status = "Pending",
                    CreatedAt = now
                };
                _context.ProjectMilestones.Add(milestone);
            }
            await _context.SaveChangesAsync();

            // 8. CREATE REGISTRATION
            var registration = new ProjectTemplateRegistration
            {
                TemplateId = dto.TemplateId,
                GroupId = dto.GroupId,
                ProjectId = project.ProjectId,
                Status = "Active",
                RegisteredAt = now,
                RegisteredBy = studentId
            };
            await _templateRepo.CreateRegistrationAsync(registration);

            await transaction.CommitAsync();

            return new ResultModel<TemplateRegistrationResponseDto>
            {
                IsSuccess = true,
                Message = "Registration successful! Project and milestones created automatically.",
                Data = new TemplateRegistrationResponseDto
                {
                    RegistrationId = registration.RegistrationId,
                    TemplateId = template.TemplateId,
                    TemplateTitle = template.Title,
                    GroupId = group.GroupId,
                    GroupName = group.GroupName,
                    ProjectId = project.ProjectId,
                    ProjectTitle = project.Title ?? template.Title,
                    Status = "Active",
                    RegisteredAt = now,
                    MilestonesCreated = milestones.Count,
                    Message = $"Project created with {milestones.Count} milestones"
                },
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new ResultModel<TemplateRegistrationResponseDto>
            {
                IsSuccess = false,
                Message = $"Error registering template: {ex.Message}. Inner: {ex.InnerException?.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    #endregion

    #region Helper Methods

    private ProjectTemplateResponseDto MapToResponseDto(BusinessObjects.Models.ProjectTemplate template)
    {
        return new ProjectTemplateResponseDto
        {
            TemplateId = template.TemplateId,
            ClassId = template.ClassId,
            ClassName = template.Class?.ClassName ?? "Unknown",
            Title = template.Title,
            Description = template.Description,
            Component = template.Component,
            MaxGroups = template.MaxGroups,
            RegisteredCount = template.RegisteredCount,
            AvailableSlots = template.MaxGroups.HasValue ? template.MaxGroups.Value - template.RegisteredCount : null,
            IsActive = template.IsActive,
            CanRegister = template.IsActive && (template.MaxGroups == null || template.RegisteredCount < template.MaxGroups),
            CreatedBy = template.Creator?.FullName ?? "Unknown",
            CreatedAt = template.CreatedAt ?? TruncateToSeconds(DateTime.UtcNow),
            Milestones = template.TemplateMilestones.Select(m => new TemplateMilestoneDto
            {
                TemplateMilestoneId = m.TemplateMilestoneId,
                Title = m.Title,
                Description = m.Description,
                OrderIndex = m.OrderIndex,
                Weight = m.Weight,
                DaysDuration = m.DaysDuration
            }).OrderBy(m => m.OrderIndex).ToList()
        };
    }

    public async Task<ResultModel<List<MyGroupRegistrationDto>>> GetMyGroupRegistrationsAsync(int studentId)
    {
        try
        {
            // Get student's groups
            var studentGroups = await _context.GroupMembers
                .Include(gm => gm.Group)
                .Where(gm => gm.UserId == studentId)
                .Select(gm => gm.Group)
                .ToListAsync();

            if (!studentGroups.Any())
            {
                return new ResultModel<List<MyGroupRegistrationDto>>
                {
                    IsSuccess = true,
                    Message = "You are not in any group",
                    Data = new List<MyGroupRegistrationDto>(),
                    StatusCode = StatusCodes.Status200OK
                };
            }

            var groupIds = studentGroups.Select(g => g.GroupId).ToList();

            // Get all registrations for these groups
            var registrations = await _context.ProjectTemplateRegistrations
                .Include(r => r.ProjectTemplate)
                .Include(r => r.Group)
                .Include(r => r.Project)
                .Where(r => groupIds.Contains(r.GroupId))
                .OrderByDescending(r => r.RegisteredAt)
                .ToListAsync();

            var result = new List<MyGroupRegistrationDto>();
            foreach (var reg in registrations)
            {
                // Check if can cancel (no submissions)
                bool canCancel = false;
                if (reg.Status == "Active" && reg.ProjectId.HasValue)
                {
                    var hasSubmissions = await _context.MilestoneSubmissions
                        .AnyAsync(s => s.ProjectId == reg.ProjectId.Value);
                    canCancel = !hasSubmissions;
                }

                result.Add(new MyGroupRegistrationDto
                {
                    RegistrationId = reg.RegistrationId,
                    TemplateId = reg.TemplateId,
                    TemplateTitle = reg.ProjectTemplate?.Title ?? "Unknown",
                    TemplateDescription = reg.ProjectTemplate?.Description,
                    GroupId = reg.GroupId,
                    GroupName = reg.Group?.GroupName ?? "Unknown",
                    ProjectId = reg.ProjectId,
                    ProjectTitle = reg.Project?.Title,
                    Status = reg.Status,
                    RegisteredAt = reg.RegisteredAt ?? TruncateToSeconds(DateTime.UtcNow),
                    CanCancel = canCancel
                });
            }

            return new ResultModel<List<MyGroupRegistrationDto>>
            {
                IsSuccess = true,
                Message = $"Found {result.Count} registration(s)",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<MyGroupRegistrationDto>>
            {
                IsSuccess = false,
                Message = $"Error: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    #endregion
}
