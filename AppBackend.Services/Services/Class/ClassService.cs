using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.SemesterRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AppBackend.Services.Services.Notification;

namespace AppBackend.Services.Services.Class;

public class ClassService : IClassService
{
    private readonly IotShowroomContext _context;
    private readonly ILogger<ClassService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IClassRepository _classRepo;
    private readonly ISemesterRepository _semesterRepo;
    private readonly IUserRepository _userRepo;

    public ClassService(
        IotShowroomContext context, 
        ILogger<ClassService> logger,
        INotificationService notificationService,
        IClassRepository classRepo,
        ISemesterRepository semesterRepo,
        IUserRepository userRepo)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
        _classRepo = classRepo;
        _semesterRepo = semesterRepo;
        _userRepo = userRepo;
    }

    public async Task<ResultModel<List<ClassResponseDto>>> GetAssignedClassesAsync(int instructorId)
    {
        var classes = await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.Semester)
            .Include(c => c.ClassEnrollments)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
            .Where(c => c.InstructorId == instructorId)
            .ToListAsync();
        
        var dtos = classes.Select(c => new ClassResponseDto
        {
            ClassId = c.ClassId,
            ClassName = c.ClassName,
            InstructorId = c.InstructorId,
            InstructorName = c.Instructor?.FullName,
            SemesterId = c.SemesterId,
            SemesterName = c.Semester?.Name,
            SemesterCode = c.Semester?.Code,
            Description = c.Description,
            CreatedAt = c.CreatedAt,
            Status = c.Status,
            StartTime = c.StartTime,
            TotalStudents = c.ClassEnrollments?.Count ?? 0,
            TotalGroups = c.Groups?.Count ?? 0,
            TotalProjects = c.Groups?.Sum(g => g.Projects?.Count ?? 0) ?? 0
        }).ToList();

        return new ResultModel<List<ClassResponseDto>>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = "Classes retrieved successfully",
            Data = dtos,
            StatusCode = StatusCodes.Status200OK
        };
    }

    public async Task<ResultModel<ClassDetailDto>> GetClassDetailAsync(int classId)
    {
        var classEntity = await _context.Classes
            .Include(c => c.Semester)
            .Include(c => c.Instructor)
            .Include(c => c.ClassEnrollments)
                .ThenInclude(ce => ce.Student)
            .Include(c => c.Groups)
                .ThenInclude(g => g.GroupMembers)
                    .ThenInclude(gm => gm.User)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Leader)
            .Include(c => c.Groups)
                .ThenInclude(g => g.Projects)
            .FirstOrDefaultAsync(c => c.ClassId == classId);
        
        if (classEntity == null)
        {
            return new ResultModel<ClassDetailDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = "Class not found",
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        var dto = new ClassDetailDto
        {
            ClassId = classEntity.ClassId,
            ClassName = classEntity.ClassName,
            InstructorId = classEntity.InstructorId,
            InstructorName = classEntity.Instructor?.FullName,
            SemesterId = classEntity.SemesterId,
            SemesterName = classEntity.Semester?.Name,
            SemesterCode = classEntity.Semester?.Code,
            Description = classEntity.Description,
            CreatedAt = classEntity.CreatedAt,
            Status = classEntity.Status,
            StartTime = classEntity.StartTime,
            TotalStudents = classEntity.ClassEnrollments?.Count ?? 0,
            TotalGroups = classEntity.Groups?.Count ?? 0,
            TotalProjects = classEntity.Groups?.Sum(g => g.Projects?.Count ?? 0) ?? 0,
            Groups = classEntity.Groups?.Select(g => new GroupSummaryDto
            {
                GroupId = g.GroupId,
                GroupName = g.GroupName,
                LeaderName = g.Leader?.FullName,
                MemberCount = g.GroupMembers?.Count ?? 0,
                ProjectCount = g.Projects?.Count ?? 0
            }).ToList(),
            Students = classEntity.ClassEnrollments?.Select(ce => new StudentEnrollmentDto
            {
                UserId = ce.StudentId ?? 0,
                FullName = ce.Student?.FullName,
                Email = ce.Student?.Email,
                EnrolledAt = ce.EnrolledAt
            }).ToList()
        };

        return new ResultModel<ClassDetailDto>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = "Class detail retrieved successfully",
            Data = dto,
            StatusCode = StatusCodes.Status200OK
        };
    }

    public async Task<ResultModel<List<ClassResponseDto>>> GetAllClassesAsync()
    {
        var classes = await _classRepo.GetAllAsync();
        
        var dtos = classes.Select(c => new ClassResponseDto
        {
            ClassId = c.ClassId,
            ClassName = c.ClassName,
            InstructorId = c.InstructorId,
            InstructorName = c.Instructor?.FullName,
            SemesterId = c.SemesterId,
            SemesterName = c.Semester?.Name,
            SemesterCode = c.Semester?.Code,
            Description = c.Description,
            CreatedAt = c.CreatedAt,
            Status = c.Status,
            StartTime = c.StartTime,
            TotalStudents = c.ClassEnrollments?.Count ?? 0,
            TotalGroups = c.Groups?.Count ?? 0,
            TotalProjects = c.Groups?.Sum(g => g.Projects?.Count ?? 0) ?? 0
        })
        .OrderByDescending(c => c.CreatedAt)
        .ToList();

        return new ResultModel<List<ClassResponseDto>>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = "Classes retrieved successfully",
            Data = dtos,
            StatusCode = StatusCodes.Status200OK
        };
    }

    public async Task<ResultModel<ClassResponseDto>> GetClassByIdAsync(int classId)
    {
        var classEntity = await _classRepo.GetByIdAsync(classId);
        
        if (classEntity == null)
        {
            return new ResultModel<ClassResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = "Class not found",
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        var dto = new ClassResponseDto
        {
            ClassId = classEntity.ClassId,
            ClassName = classEntity.ClassName,
            InstructorId = classEntity.InstructorId,
            InstructorName = classEntity.Instructor?.FullName,
            SemesterId = classEntity.SemesterId,
            SemesterName = classEntity.Semester?.Name,
            SemesterCode = classEntity.Semester?.Code,
            Description = classEntity.Description,
            CreatedAt = classEntity.CreatedAt,
            Status = classEntity.Status,
            StartTime = classEntity.StartTime,
            TotalStudents = classEntity.ClassEnrollments?.Count ?? 0,
            TotalGroups = classEntity.Groups?.Count ?? 0,
            TotalProjects = classEntity.Groups?.Sum(g => g.Projects?.Count ?? 0) ?? 0
        };

        return new ResultModel<ClassResponseDto>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = "Class retrieved successfully",
            Data = dto,
            StatusCode = StatusCodes.Status200OK
        };
    }

    public async Task<ResultModel<List<ClassResponseDto>>> GetClassesBySemesterAsync(int semesterId)
    {
        var classes = await _classRepo.GetBySemesterAsync(semesterId);
        
        var dtos = classes.Select(c => new ClassResponseDto
        {
            ClassId = c.ClassId,
            ClassName = c.ClassName,
            InstructorId = c.InstructorId,
            InstructorName = c.Instructor?.FullName,
            SemesterId = c.SemesterId,
            SemesterName = c.Semester?.Name,
            SemesterCode = c.Semester?.Code,
            Description = c.Description,
            CreatedAt = c.CreatedAt,
            Status = c.Status,
            StartTime = c.StartTime,
            TotalStudents = c.ClassEnrollments?.Count ?? 0,
            TotalGroups = c.Groups?.Count ?? 0,
            TotalProjects = c.Groups?.Sum(g => g.Projects?.Count ?? 0) ?? 0
        }).ToList();

        return new ResultModel<List<ClassResponseDto>>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = "Classes retrieved successfully",
            Data = dtos,
            StatusCode = StatusCodes.Status200OK
        };
    }

    public async Task<ResultModel<List<ClassResponseDto>>> SearchClassesAsync(int? semesterId, string? searchQuery)
    {
        var classes = await _classRepo.SearchClassesAsync(semesterId, searchQuery);
        
        var dtos = classes.Select(c => new ClassResponseDto
        {
            ClassId = c.ClassId,
            ClassName = c.ClassName,
            InstructorId = c.InstructorId,
            InstructorName = c.Instructor?.FullName,
            SemesterId = c.SemesterId,
            SemesterName = c.Semester?.Name,
            SemesterCode = c.Semester?.Code,
            Description = c.Description,
            CreatedAt = c.CreatedAt,
            Status = c.Status,
            StartTime = c.StartTime,
            TotalStudents = c.ClassEnrollments?.Count ?? 0,
            TotalGroups = c.Groups?.Count ?? 0,
            TotalProjects = c.Groups?.Sum(g => g.Projects?.Count ?? 0) ?? 0
        }).ToList();

        return new ResultModel<List<ClassResponseDto>>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = $"Found {dtos.Count} classes",
            Data = dtos,
            StatusCode = StatusCodes.Status200OK
        };
    }

    public async Task<ResultModel<ClassResponseDto>> CreateClassAsync(CreateClassRequestDto request)
    {
        // Validate semester exists
        var semester = await _semesterRepo.GetByIdAsync(request.SemesterId);
        if (semester == null)
        {
            return new ResultModel<ClassResponseDto>
            {
                IsSuccess = false,
                ResponseCode = "SEMESTER_NOT_FOUND",
                Message = "Semester not found",
                Data = null,
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        // Validate semester is active
        if (semester.IsActive != true)
        {
            return new ResultModel<ClassResponseDto>
            {
                IsSuccess = false,
                ResponseCode = "SEMESTER_NOT_ACTIVE",
                Message = $"Cannot create class: Semester '{semester.Name}' ({semester.Code}) is not active",
                Data = null,
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        // Check if class name already exists in this semester
        var exists = await _classRepo.ClassNameExistsAsync(request.ClassName, request.SemesterId);
        if (exists)
        {
            return new ResultModel<ClassResponseDto>
            {
                IsSuccess = false,
                ResponseCode = "DUPLICATE_CLASS_NAME",
                Message = $"Class '{request.ClassName}' already exists in this semester",
                Data = null,
                StatusCode = StatusCodes.Status409Conflict
            };
        }

        // If instructor is provided, validate they exist and have correct role
        if (request.InstructorId.HasValue)
        {
            var instructor = await _userRepo.GetByIdAsync(request.InstructorId.Value);
            if (instructor == null)
            {
                return new ResultModel<ClassResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "INSTRUCTOR_NOT_FOUND",
                    Message = "Instructor not found",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            if (instructor.RoleId != 2) // RoleId 2 = Instructor
            {
                return new ResultModel<ClassResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_ROLE",
                    Message = "User must have Instructor role",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }

        // Create new class
        var newClass = new BusinessObjects.Models.Class
        {
            ClassName = request.ClassName,
            SemesterId = request.SemesterId,
            Description = request.Description,
            InstructorId = request.InstructorId,
            CreatedAt = DateTime.UtcNow,
            Status = "Not Started",
            StartTime = request.StartTime
        };

        await _classRepo.AddAsync(newClass);
        await _classRepo.SaveChangesAsync();

        // Reload with navigation properties
        var createdClass = await _classRepo.GetByIdAsync(newClass.ClassId);

        var dto = new ClassResponseDto
        {
            ClassId = createdClass!.ClassId,
            ClassName = createdClass.ClassName,
            InstructorId = createdClass.InstructorId,
            InstructorName = createdClass.Instructor?.FullName,
            SemesterId = createdClass.SemesterId,
            SemesterName = createdClass.Semester?.Name,
            SemesterCode = createdClass.Semester?.Code,
            Description = createdClass.Description,
            CreatedAt = createdClass.CreatedAt,
            Status = createdClass.Status,
            StartTime = createdClass.StartTime,
            TotalStudents = 0,
            TotalGroups = 0,
            TotalProjects = 0
        };

        return new ResultModel<ClassResponseDto>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = "Class created successfully",
            Data = dto,
            StatusCode = StatusCodes.Status201Created
        };
    }

    public async Task<ResultModel<ClassResponseDto>> UpdateClassAsync(int classId, UpdateClassRequestDto request)
    {
        var classEntity = await _classRepo.GetByIdAsync(classId);
        
        if (classEntity == null)
        {
            return new ResultModel<ClassResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = "Class not found",
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        // Update class name if provided
        if (!string.IsNullOrWhiteSpace(request.ClassName))
        {
            // Check if new name already exists in the same semester
            var exists = await _classRepo.ClassNameExistsAsync(request.ClassName, classEntity.SemesterId ?? 0, classId);
            if (exists)
            {
                return new ResultModel<ClassResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "DUPLICATE_CLASS_NAME",
                    Message = $"Class '{request.ClassName}' already exists in this semester",
                    Data = null,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }
            classEntity.ClassName = request.ClassName;
        }

        // Update description if provided
        if (request.Description != null)
        {
            classEntity.Description = request.Description;
        }

        // Update instructor if provided
        if (request.InstructorId.HasValue)
        {
            var instructor = await _userRepo.GetByIdAsync(request.InstructorId.Value);
            if (instructor == null)
            {
                return new ResultModel<ClassResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "INSTRUCTOR_NOT_FOUND",
                    Message = "Instructor not found",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            if (instructor.RoleId != 2) // RoleId 2 = Instructor
            {
                return new ResultModel<ClassResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_ROLE",
                    Message = "User must have Instructor role",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            classEntity.InstructorId = request.InstructorId.Value;
        }

        // Update StartTime if provided
        if (request.StartTime.HasValue)
        {
            classEntity.StartTime = request.StartTime;
        }

        await _classRepo.UpdateAsync(classEntity);
        await _classRepo.SaveChangesAsync();

        // Reload with navigation properties
        var updatedClass = await _classRepo.GetByIdAsync(classId);

        var dto = new ClassResponseDto
        {
            ClassId = updatedClass!.ClassId,
            ClassName = updatedClass.ClassName,
            InstructorId = updatedClass.InstructorId,
            InstructorName = updatedClass.Instructor?.FullName,
            SemesterId = updatedClass.SemesterId,
            SemesterName = updatedClass.Semester?.Name,
            SemesterCode = updatedClass.Semester?.Code,
            Description = updatedClass.Description,
            CreatedAt = updatedClass.CreatedAt,
            Status = updatedClass.Status,
            StartTime = updatedClass.StartTime,
            TotalStudents = updatedClass.ClassEnrollments?.Count ?? 0,
            TotalGroups = updatedClass.Groups?.Count ?? 0,
            TotalProjects = updatedClass.Groups?.Sum(g => g.Projects?.Count ?? 0) ?? 0
        };

        return new ResultModel<ClassResponseDto>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = "Class updated successfully",
            Data = dto,
            StatusCode = StatusCodes.Status200OK
        };
    }

    public async Task<ResultModel<bool>> DeleteClassAsync(int classId)
    {
        var classEntity = await _classRepo.GetByIdAsync(classId);
        
        if (classEntity == null)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = "Class not found",
                Data = false,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        // Check if class has groups
        var hasGroups = await _classRepo.HasGroupsAsync(classId);
        if (hasGroups)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                ResponseCode = "HAS_GROUPS",
                Message = "Cannot delete class that has groups",
                Data = false,
                StatusCode = StatusCodes.Status409Conflict
            };
        }

        // Check if class has enrollments
        var hasEnrollments = await _classRepo.HasEnrollmentsAsync(classId);
        if (hasEnrollments)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                ResponseCode = "HAS_ENROLLMENTS",
                Message = "Cannot delete class that has enrolled students",
                Data = false,
                StatusCode = StatusCodes.Status409Conflict
            };
        }

        await _classRepo.DeleteAsync(classEntity);
        await _classRepo.SaveChangesAsync();

        return new ResultModel<bool>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = "Class deleted successfully",
            Data = true,
            StatusCode = StatusCodes.Status200OK
        };
    }

    public async Task<ResultModel<bool>> AssignInstructorAsync(int classId, int instructorId)
    {
        var classEntity = await _classRepo.GetByIdAsync(classId);
        
        if (classEntity == null)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = "Class not found",
                Data = false,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        var instructor = await _userRepo.GetByIdAsync(instructorId);
        if (instructor == null)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                ResponseCode = "INSTRUCTOR_NOT_FOUND",
                Message = "Instructor not found",
                Data = false,
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        if (instructor.RoleId != 2) // RoleId 2 = Instructor
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                ResponseCode = "INVALID_ROLE",
                Message = "User must have Instructor role",
                Data = false,
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        classEntity.InstructorId = instructorId;
        await _classRepo.UpdateAsync(classEntity);
        await _classRepo.SaveChangesAsync();

        return new ResultModel<bool>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = $"Instructor '{instructor.FullName}' assigned to class successfully",
            Data = true,
            StatusCode = StatusCodes.Status200OK
        };
    }

    public async Task<ResultModel<ChangeClassStatusResponseDto>> ChangeClassStatusAsync(int classId, ChangeClassStatusRequestDto request)
    {
        var classEntity = await _context.Classes
            .Include(c => c.Instructor)
            .Include(c => c.ClassEnrollments)
                .ThenInclude(ce => ce.Student)
            .Include(c => c.Groups)
                .ThenInclude(g => g.GroupMembers)
            .FirstOrDefaultAsync(c => c.ClassId == classId);

        if (classEntity == null)
        {
            return new ResultModel<ChangeClassStatusResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = "Class not found",
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        var oldStatus = classEntity.Status;
        
        // Validation when changing to "In Progress"
        if (request.Status == "In Progress")
        {
            // Get all enrolled students
            var totalStudents = classEntity.ClassEnrollments?.Count ?? 0;
            if (totalStudents == 0)
            {
                return new ResultModel<ChangeClassStatusResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "NO_STUDENTS",
                    Message = "Cannot change status to 'In Progress': Class has no enrolled students",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }

        // Validation when changing to "Completed"
        if (request.Status == "Completed")
        {
            var validationErrors = new List<string>();

            // Get all projects in this class with their milestones and final submissions
            var projects = await _context.Projects
                .Include(p => p.Group)
                .Include(p => p.ProjectMilestones)
                    .ThenInclude(pm => pm.MilestoneEvaluations)
                .Include(p => p.FinalProjectSubmission)
                    .ThenInclude(fps => fps!.FinalSubmissionGrades)
                .Where(p => p.Group!.ClassId == classId && 
                           p.Status != null && 
                           p.Status.ToLower() == "approved")
                .ToListAsync();

            if (projects.Any())
            {
                // Check if all milestones are graded
                foreach (var project in projects)
                {
                    var milestones = project.ProjectMilestones ?? new List<BusinessObjects.Models.ProjectMilestone>();
                    
                    foreach (var milestone in milestones)
                    {
                        var hasEvaluation = milestone.MilestoneEvaluations != null && 
                                          milestone.MilestoneEvaluations.Any();
                        
                        if (!hasEvaluation)
                        {
                            validationErrors.Add($"Project '{project.Title}' (Group: {project.Group?.GroupName}) - Milestone '{milestone.Title}' has not been graded yet");
                        }
                    }
                }

                // Check if instructor has graded all final submissions
                foreach (var project in projects)
                {
                    if (project.FinalProjectSubmission == null)
                    {
                        validationErrors.Add($"Project '{project.Title}' (Group: {project.Group?.GroupName}) - No final submission found");
                    }
                    else
                    {
                        // Check if instructor has graded
                        if (project.FinalProjectSubmission.Grade == null)
                        {
                            validationErrors.Add($"Project '{project.Title}' (Group: {project.Group?.GroupName}) - Final submission has not been graded by instructor yet");
                        }
                    }
                }

                // Check if all assigned graders have graded
                var activeGraders = await _context.ClassGraders
                    .Where(cg => cg.ClassId == classId && cg.IsActive)
                    .ToListAsync();

                if (activeGraders.Any())
                {
                    foreach (var project in projects)
                    {
                        if (project.FinalProjectSubmission != null)
                        {
                            var submissionGrades = project.FinalProjectSubmission.FinalSubmissionGrades ?? new List<FinalSubmissionGrade>();
                            var gradedInstructorIds = submissionGrades.Select(g => g.InstructorId).ToList();

                            foreach (var grader in activeGraders)
                            {
                                if (!gradedInstructorIds.Contains(grader.InstructorId))
                                {
                                    var graderInfo = await _context.Users
                                        .FirstOrDefaultAsync(u => u.UserId == grader.InstructorId);
                                    
                                    validationErrors.Add($"Project '{project.Title}' (Group: {project.Group?.GroupName}) - Grader '{graderInfo?.FullName ?? "Unknown"}' has not graded yet");
                                }
                            }
                        }
                    }
                }
            }

            // If there are validation errors, return error response
            if (validationErrors.Any())
            {
                return new ResultModel<ChangeClassStatusResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "GRADING_INCOMPLETE",
                    Message = $"Cannot change status to 'Completed': Not all grading requirements are met. Found {validationErrors.Count} issue(s).",
                    Data = new ChangeClassStatusResponseDto
                    {
                        ClassId = classId,
                        ClassName = classEntity.ClassName,
                        OldStatus = oldStatus,
                        NewStatus = oldStatus, // Keep old status
                        ChangedAt = DateTime.UtcNow,
                        Warnings = validationErrors
                    },
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }

        // Update class status
        classEntity.Status = request.Status;
        await _context.SaveChangesAsync();

        // If status changed to "In Progress", update StudentCourseHistory for all students in class
        int updatedHistoryCount = 0;
        var warnings = new List<string>();
        
        if (request.Status == "In Progress" && classEntity.ClassEnrollments != null)
        {
            var classSemesterId = classEntity.SemesterId;
            var studentIds = classEntity.ClassEnrollments
                .Where(ce => ce.StudentId.HasValue)
                .Select(ce => ce.StudentId!.Value)
                .ToList();

            if (studentIds.Any())
            {
                foreach (var studentId in studentIds)
                {
                    AppBackend.BusinessObjects.Models.StudentCourseHistory? history = null;
                    
                    if (classSemesterId.HasValue)
                    {
                        // Try to find existing history for this student and semester
                        history = await _context.StudentCourseHistories
                            .FirstOrDefaultAsync(h => h.StudentId == studentId && h.SemesterId == classSemesterId.Value);
                        
                        if (history == null)
                        {
                            // Create new StudentCourseHistory if not exists
                            history = new AppBackend.BusinessObjects.Models.StudentCourseHistory
                            {
                                StudentId = studentId,
                                SemesterId = classSemesterId.Value,
                                Status = "In Progress",
                                IsRetake = false,
                                IsCurrent = false, // Will be updated by background service
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.StudentCourseHistories.Add(history);
                            updatedHistoryCount++;
                        }
                        else
                        {
                            // Update existing history only if not already completed
                            if (history.Status != "Pass" && history.Status != "Not Pass")
                            {
                                history.Status = "In Progress";
                                history.UpdatedAt = DateTime.UtcNow;
                                _context.StudentCourseHistories.Update(history);
                                updatedHistoryCount++;
                            }
                        }
                    }
                    else
                    {
                        // If class doesn't have semester, try to find current history
                        history = await _context.StudentCourseHistories
                            .FirstOrDefaultAsync(h => h.StudentId == studentId && h.IsCurrent == true);
                        
                        if (history != null)
                        {
                            // Update existing history only if not already completed
                            if (history.Status != "Pass" && history.Status != "Not Pass")
                            {
                                history.Status = "In Progress";
                                history.UpdatedAt = DateTime.UtcNow;
                                _context.StudentCourseHistories.Update(history);
                                updatedHistoryCount++;
                            }
                        }
                        else
                        {
                            warnings.Add($"Cannot update course history for student ID {studentId}: Class has no semester and student has no current course history");
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        // If status changed to "Completed", update StudentCourseHistory with final grades
        if (request.Status == "Completed" && classEntity.ClassEnrollments != null)
        {
            var classInstructorId = classEntity.InstructorId;
            var classSemesterId = classEntity.SemesterId;
            
            foreach (var enrollment in classEntity.ClassEnrollments.Where(e => e.StudentId.HasValue))
            {
                var studentId = enrollment.StudentId.Value;
                
                // Find or create StudentCourseHistory for this student and semester
                AppBackend.BusinessObjects.Models.StudentCourseHistory? history = null;
                
                if (classSemesterId.HasValue)
                {
                    // Try to find existing history for this student and semester
                    history = await _context.StudentCourseHistories
                        .FirstOrDefaultAsync(h => h.StudentId == studentId && h.SemesterId == classSemesterId.Value);
                    
                    if (history == null)
                    {
                        // Create new StudentCourseHistory if not exists
                        history = new AppBackend.BusinessObjects.Models.StudentCourseHistory
                        {
                            StudentId = studentId,
                            SemesterId = classSemesterId.Value,
                            Status = "Not Started",
                            IsRetake = false,
                            IsCurrent = false, // Will be updated by background service
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.StudentCourseHistories.Add(history);
                        await _context.SaveChangesAsync(); // Save to get the HistoryId
                    }
                }
                else
                {
                    // If class doesn't have semester, try to find current history
                    history = await _context.StudentCourseHistories
                        .FirstOrDefaultAsync(h => h.StudentId == studentId && h.IsCurrent == true);
                    
                    if (history == null)
                    {
                        // Cannot proceed without semester information
                        warnings.Add($"Cannot update course history for student ID {studentId}: Class has no semester and student has no current course history");
                        continue;
                    }
                }

                // Find student's group and project
                var studentGroup = await _context.GroupMembers
                    .Include(gm => gm.Group)
                        .ThenInclude(g => g!.Projects)
                            .ThenInclude(p => p.FinalProjectSubmission)
                                .ThenInclude(fs => fs!.FinalSubmissionGrades)
                    .Where(gm => gm.UserId == studentId && gm.Group!.ClassId == classId)
                    .Select(gm => gm.Group)
                    .FirstOrDefaultAsync();

                decimal? finalGrade = null;
                decimal? avgGradeFromOthers = null;
                int? finalSubmissionId = null;

                if (studentGroup != null)
                {
                    var project = studentGroup.Projects?.FirstOrDefault();
                    if (project?.FinalProjectSubmission != null)
                    {
                        var finalSubmission = project.FinalProjectSubmission;
                        finalSubmissionId = finalSubmission.FinalSubmissionId;
                        
                        // Get the average final grade (from Final_Project_Submissions.grade)
                        finalGrade = finalSubmission.Grade;

                        // Calculate average grade from other instructors (excluding primary class instructor)
                        if (finalSubmission.FinalSubmissionGrades != null && finalSubmission.FinalSubmissionGrades.Any())
                        {
                            var otherInstructorGrades = finalSubmission.FinalSubmissionGrades
                                .Where(g => classInstructorId.HasValue && g.InstructorId != classInstructorId.Value)
                                .Select(g => g.Grade)
                                .ToList();

                            if (otherInstructorGrades.Any())
                            {
                                avgGradeFromOthers = otherInstructorGrades.Average();
                            }
                        }
                    }
                }

                // Determine Pass/Not Pass based on final grade
                string newStatus;
                bool isRetake;
                string notes;

                if (finalGrade.HasValue)
                {
                    if (finalGrade.Value >= 5)
                    {
                        newStatus = "Pass";
                        isRetake = false;
                        notes = $"Passed with final grade: {finalGrade.Value:F2}/10";
                    }
                    else
                    {
                        newStatus = "Not Pass";
                        isRetake = true;
                        notes = $"Not passed with final grade: {finalGrade.Value:F2}/10 (Below passing threshold of 5.0)";
                    }

                    // Add information about other instructor grades if available
                    if (avgGradeFromOthers.HasValue)
                    {
                        notes += $". Average grade from other instructors: {avgGradeFromOthers.Value:F2}/10";
                        
                        if (avgGradeFromOthers.Value >= 5)
                        {
                            notes += " (Pass)";
                        }
                        else
                        {
                            notes += " (Not Pass)";
                        }
                    }
                }
                else
                {
                    // No final grade available - student might not have submitted or not graded
                    newStatus = "Not Pass";
                    isRetake = true;
                    notes = "No final submission or final grade not available";
                }

                // Update the history record
                history.Status = newStatus;
                history.FinalSubmissionId = finalSubmissionId;
                history.FinalGrade = finalGrade;
                history.AverageGradeFromOtherInstructors = avgGradeFromOthers;
                history.IsRetake = isRetake;
                history.Notes = notes;
                history.CompletedAt = DateTime.UtcNow;
                history.EvaluatedAt = DateTime.UtcNow;
                history.UpdatedAt = DateTime.UtcNow;
                
                _context.StudentCourseHistories.Update(history);
                updatedHistoryCount++;
            }

            await _context.SaveChangesAsync();

            // Send notification to students about Pass/Not Pass status and grades
            foreach (var enrollment in classEntity.ClassEnrollments.Where(e => e.StudentId.HasValue))
            {
                var studentId = enrollment.StudentId.Value;
                
                // Get current StudentCourseHistory for this student and semester
                AppBackend.BusinessObjects.Models.StudentCourseHistory? history = null;
                
                if (classSemesterId.HasValue)
                {
                    history = await _context.StudentCourseHistories
                        .FirstOrDefaultAsync(h => h.StudentId == studentId && h.SemesterId == classSemesterId.Value);
                }
                else
                {
                    history = await _context.StudentCourseHistories
                        .FirstOrDefaultAsync(h => h.StudentId == studentId && h.IsCurrent == true);
                }

                if (history != null)
                {
                    // Create notification content
                    string title = $"Course Results - Class {classEntity.ClassName}";
                    string message = $"Class '{classEntity.ClassName}' has been completed.\n\n";

                    // Add information about grades and Pass/Not Pass status
                    if (history.FinalGrade.HasValue)
                    {
                        message += $"• Final grade: {history.FinalGrade.Value:F2}/10\n";
                        
                        if (history.Status == "Pass")
                        {
                            message += $"• Result: PASS\n";
                        }
                        else if (history.Status == "Not Pass")
                        {
                            message += $"• Result: NOT PASS\n";
                        }

                        // Add information about grades from other instructors if available
                        if (history.AverageGradeFromOtherInstructors.HasValue)
                        {
                            message += $"• Average grade from other instructors: {history.AverageGradeFromOtherInstructors.Value:F2}/10\n";
                        }
                    }
                    else
                    {
                        message += $"• Result: NOT PASS\n";
                        message += "• Reason: No final grade available or no submission\n";
                    }

                    // Add note if need to retake
                    if (history.IsRetake == true)
                    {
                        message += "\n• You need to re-register for this course.";
                    }

                    // Send notification with data containing userId
                    var notificationRequest = new NotificationCreateRequestDto
                    {
                        UserId = studentId,
                        Title = title,
                        Message = message,
                        Type = "course_completion"
                    };

                    try
                    {
                        await _notificationService.SendNotificationAsync(notificationRequest);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send notification to student {studentId}");
                        // Continue with other students even if notification fails
                    }
                }
            }
        }

        // Prepare response
        var totalEnrolled = classEntity.ClassEnrollments?.Count ?? 0;
        var studentsInGroups = classEntity.Groups?
            .SelectMany(g => g.GroupMembers ?? Enumerable.Empty<GroupMember>())
            .Select(gm => gm.UserId)
            .Distinct()
            .Count() ?? 0;

        if (request.Status == "In Progress" && updatedHistoryCount > 0)
        {
            warnings.Add($"Updated {updatedHistoryCount} student course history records to 'In Progress' status");
        }
        
        if (request.Status == "Completed" && updatedHistoryCount > 0)
        {
            warnings.Add($"Automatically evaluated and updated {updatedHistoryCount} student course history records");
            warnings.Add("Students have been assigned Pass/Not Pass status based on final grades (threshold: 5.0)");
        }

        return new ResultModel<ChangeClassStatusResponseDto>
        {
            IsSuccess = true,
            ResponseCode = CommonMessageConstants.SUCCESS,
            Message = $"Class status changed from '{oldStatus}' to '{request.Status}' successfully",
            Data = new ChangeClassStatusResponseDto
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                OldStatus = oldStatus,
                NewStatus = request.Status,
                ChangedAt = DateTime.UtcNow,
                TotalStudents = totalEnrolled,
                StudentsWithGroup = studentsInGroups,
                StudentsWithoutGroup = totalEnrolled - studentsInGroups,
                Warnings = warnings
            },
            StatusCode = StatusCodes.Status200OK
        };
    }
}
