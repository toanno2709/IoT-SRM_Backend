using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Repositories.Repositories.SemesterRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels.Commons;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.Class;

public class ClassService : IClassService
{
    private readonly IClassRepository _classRepo;
    private readonly ISemesterRepository _semesterRepo;
    private readonly IUserRepository _userRepo;
    private readonly IGroupRepository _groupRepo;
    private readonly IMapper _mapper;

    public ClassService(
        IClassRepository classRepo, 
        ISemesterRepository semesterRepo,
        IUserRepository userRepo,
        IGroupRepository groupRepo,
        IMapper mapper)
    {
        _classRepo = classRepo;
        _semesterRepo = semesterRepo;
        _userRepo = userRepo;
        _groupRepo = groupRepo;
        _mapper = mapper;
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
            CreatedAt = DateTime.UtcNow
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
}
