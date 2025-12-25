using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.SemesterRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels.Commons;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.Class;

public class ClassService : IClassService
{
    private readonly IClassRepository _classRepo;
    private readonly ISemesterRepository _semesterRepo;
    private readonly IUserRepository _userRepo;
    private readonly IGroupRepository _groupRepo;
    private readonly IMapper _mapper;
    private readonly AppBackend.BusinessObjects.Data.IotShowroomContext _context;

    public ClassService(
        IClassRepository classRepo, 
        ISemesterRepository semesterRepo,
        IUserRepository userRepo,
        IGroupRepository groupRepo,
        IMapper mapper,
        AppBackend.BusinessObjects.Data.IotShowroomContext context)
    {
        _classRepo = classRepo;
        _semesterRepo = semesterRepo;
        _userRepo = userRepo;
        _groupRepo = groupRepo;
        _mapper = mapper;
        _context = context;
    }

    public async Task<ResultModel<List<ClassResponseDto>>> GetAssignedClassesAsync(int instructorId)
    {
        var classes = await _classRepo.GetAssignedClassesAsync(instructorId);
        
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
        var classEntity = await _classRepo.GetClassWithDetailsAsync(classId);
        
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

            // Get students who are in groups
            var studentIdsInGroups = classEntity.Groups?
                .SelectMany(g => g.GroupMembers ?? new List<GroupMember>())
                .Select(gm => gm.UserId)
                .Distinct()
                .ToHashSet() ?? new HashSet<int>();

            var enrolledStudentIds = classEntity.ClassEnrollments?
                .Select(ce => ce.StudentId ?? 0)
                .Where(id => id > 0)
                .ToHashSet() ?? new HashSet<int>();

            var studentsWithoutGroup = enrolledStudentIds.Except(studentIdsInGroups).ToList();

            if (studentsWithoutGroup.Any())
            {
                var studentsWithoutGroupDetails = classEntity.ClassEnrollments?
                    .Where(ce => studentsWithoutGroup.Contains(ce.StudentId ?? 0))
                    .Select(ce => ce.Student?.FullName ?? ce.Student?.Email ?? $"Student ID: {ce.StudentId}")
                    .ToList() ?? new List<string>();

                return new ResultModel<ChangeClassStatusResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "STUDENTS_WITHOUT_GROUP",
                    Message = $"Cannot change status to 'In Progress': {studentsWithoutGroup.Count} student(s) do not have a group yet. All students must be in a group before starting the class.",
                    Data = new ChangeClassStatusResponseDto
                    {
                        ClassId = classId,
                        ClassName = classEntity.ClassName,
                        OldStatus = oldStatus,
                        NewStatus = request.Status,
                        ChangedAt = DateTime.UtcNow,
                        TotalStudents = totalStudents,
                        StudentsWithGroup = studentIdsInGroups.Count,
                        StudentsWithoutGroup = studentsWithoutGroup.Count,
                        Warnings = studentsWithoutGroupDetails
                    },
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }

        // Update status
        classEntity.Status = request.Status;
        await _context.SaveChangesAsync();

        // Prepare response
        var totalEnrolled = classEntity.ClassEnrollments?.Count ?? 0;
        var studentsInGroups = classEntity.Groups?
            .SelectMany(g => g.GroupMembers ?? new List<GroupMember>())
            .Select(gm => gm.UserId)
            .Distinct()
            .Count() ?? 0;

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
                Warnings = new List<string>()
            },
            StatusCode = StatusCodes.Status200OK
        };
    }
}
