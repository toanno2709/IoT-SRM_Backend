using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Services.ApiModels.Commons;
using AutoMapper;

namespace AppBackend.Services.Services.Class;

public interface IClassService
{
    Task<ResultModel<List<ClassResponseDto>>> GetAssignedClassesAsync(int instructorId);
    Task<ResultModel<ClassResponseDto>> GetClassByIdAsync(int classId);
}

public class ClassService : IClassService
{
    private readonly IClassRepository _classRepository;
    private readonly IMapper _mapper;

    public ClassService(IClassRepository classRepository, IMapper mapper)
    {
        _classRepository = classRepository;
        _mapper = mapper;
    }

    public async Task<ResultModel<List<ClassResponseDto>>> GetAssignedClassesAsync(int instructorId)
    {
        try
        {
            var classes = await _classRepository.GetAssignedClassesAsync(instructorId);
            
            var classDtos = classes.Select(c => new ClassResponseDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName ?? string.Empty,
                Description = c.Description,
                InstructorId = c.InstructorId,
                InstructorName = c.Instructor?.FullName,
                SemesterId = c.SemesterId,
                SemesterName = c.Semester?.SemesterName,
                CreatedAt = c.CreatedAt,
                TotalStudents = c.ClassEnrollments?.Count ?? 0,
                TotalProjects = c.Projects?.Count ?? 0
            }).ToList();

            return new ResultModel<List<ClassResponseDto>>
            {
                IsSuccess = true,
                Message = "Classes retrieved successfully",
                Data = classDtos
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<ClassResponseDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving classes: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<ClassResponseDto>> GetClassByIdAsync(int classId)
    {
        try
        {
            var classEntity = await _classRepository.GetClassWithDetailsAsync(classId);
            
            if (classEntity == null)
            {
                return new ResultModel<ClassResponseDto>
                {
                    IsSuccess = false,
                    Message = "Class not found",
                    Data = null
                };
            }

            var classDto = new ClassResponseDto
            {
                ClassId = classEntity.ClassId,
                ClassName = classEntity.ClassName ?? string.Empty,
                Description = classEntity.Description,
                InstructorId = classEntity.InstructorId,
                InstructorName = classEntity.Instructor?.FullName,
                SemesterId = classEntity.SemesterId,
                SemesterName = classEntity.Semester?.SemesterName,
                CreatedAt = classEntity.CreatedAt,
                TotalStudents = classEntity.ClassEnrollments?.Count ?? 0,
                TotalProjects = classEntity.Projects?.Count ?? 0
            };

            return new ResultModel<ClassResponseDto>
            {
                IsSuccess = true,
                Message = "Class retrieved successfully",
                Data = classDto
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassResponseDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving class: {ex.Message}",
                Data = null
            };
        }
    }
}




