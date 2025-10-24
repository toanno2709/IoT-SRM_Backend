using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Class;

public interface IClassService
{
    // Existing methods
    Task<ResultModel<List<ClassResponseDto>>> GetAssignedClassesAsync(int instructorId);
    Task<ResultModel<ClassResponseDto>> GetClassByIdAsync(int classId);
    Task<ResultModel<ClassSettingsResponseDto>> UpdateClassSettingsAsync(int classId, ClassSettingsUpdateRequestDto request);
    Task<ResultModel<ClassSettingsResponseDto>> GetClassSettingsAsync(int classId);
}

public class ClassService : IClassService
{
    private readonly IClassRepository _classRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;

    public ClassService(IClassRepository classRepository, IGroupRepository groupRepository, IMapper mapper)
    {
        _classRepository = classRepository;
        _groupRepository = groupRepository;
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
                SemesterName = c.Semester?.Name,
                CreatedAt = c.CreatedAt,
                TotalStudents = c.ClassEnrollments?.Count ?? 0,
                TotalProjects = c.Groups?.Count ?? 0
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
                SemesterName = classEntity.Semester?.Name,
                CreatedAt = classEntity.CreatedAt,
                TotalStudents = classEntity.ClassEnrollments?.Count ?? 0,
                TotalProjects = classEntity.Groups?.Count ?? 0
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

    public async Task<ResultModel<ClassSettingsResponseDto>> UpdateClassSettingsAsync(
        int classId, 
        ClassSettingsUpdateRequestDto request)
    {
        try
        {
            var classEntity = await _classRepository.GetClassWithDetailsAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<ClassSettingsResponseDto>
                {
                    IsSuccess = false,
                    Message = "Class not found",
                    Data = null
                };
            }

            // Validate min <= max
            if (request.MinMembersPerGroup.HasValue && request.MaxMembersPerGroup.HasValue)
            {
                if (request.MinMembersPerGroup > request.MaxMembersPerGroup)
                {
                    return new ResultModel<ClassSettingsResponseDto>
                    {
                        IsSuccess = false,
                        Message = "Min members cannot be greater than max members",
                        Data = null
                    };
                }
            }

            // Get current groups to check constraints
            var groups = await _groupRepository.GetGroupsByClassAsync(classId);
            var warnings = new List<string>();

            // Check max groups constraint
            if (request.MaxGroups.HasValue && groups.Count > request.MaxGroups.Value)
            {
                warnings.Add($"Current group count ({groups.Count}) exceeds new max groups limit ({request.MaxGroups.Value})");
            }

            // Check member constraints
            if (request.MaxMembersPerGroup.HasValue || request.MinMembersPerGroup.HasValue)
            {
                foreach (var group in groups)
                {
                    var memberCount = group.GroupMembers?.Count ?? 0;
                    
                    if (request.MaxMembersPerGroup.HasValue && memberCount > request.MaxMembersPerGroup.Value)
                    {
                        warnings.Add($"Group '{group.GroupName}' has {memberCount} members, exceeds new max ({request.MaxMembersPerGroup.Value})");
                    }
                    
                    if (request.MinMembersPerGroup.HasValue && memberCount < request.MinMembersPerGroup.Value)
                    {
                        warnings.Add($"Group '{group.GroupName}' has {memberCount} members, below new min ({request.MinMembersPerGroup.Value})");
                    }
                }
            }

            // Update settings
            if (request.MaxGroups.HasValue)
                classEntity.MaxGroups = request.MaxGroups.Value;
            
            if (request.MaxMembersPerGroup.HasValue)
                classEntity.MaxMembersPerGroup = request.MaxMembersPerGroup.Value;
            
            if (request.MinMembersPerGroup.HasValue)
                classEntity.MinMembersPerGroup = request.MinMembersPerGroup.Value;

            await _classRepository.UpdateAsync(classEntity);
            await _classRepository.SaveChangesAsync();

            // Calculate current stats
            var memberCounts = groups
                .Select(g => g.GroupMembers?.Count ?? 0)
                .Where(c => c > 0)
                .ToList();

            return new ResultModel<ClassSettingsResponseDto>
            {
                IsSuccess = true,
                Message = warnings.Any() 
                    ? "Settings updated with warnings" 
                    : "Settings updated successfully",
                Data = new ClassSettingsResponseDto
                {
                    ClassId = classEntity.ClassId,
                    ClassName = classEntity.ClassName,
                    MaxGroups = classEntity.MaxGroups,
                    MaxMembersPerGroup = classEntity.MaxMembersPerGroup,
                    MinMembersPerGroup = classEntity.MinMembersPerGroup,
                    CurrentGroupCount = groups.Count,
                    LargestGroupSize = memberCounts.Any() ? memberCounts.Max() : null,
                    SmallestGroupSize = memberCounts.Any() ? memberCounts.Min() : null,
                    Warnings = warnings
                }
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassSettingsResponseDto>
            {
                IsSuccess = false,
                Message = $"Error updating settings: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ResultModel<ClassSettingsResponseDto>> GetClassSettingsAsync(int classId)
    {
        try
        {
            var classEntity = await _classRepository.GetClassWithDetailsAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<ClassSettingsResponseDto>
                {
                    IsSuccess = false,
                    Message = "Class not found",
                    Data = null
                };
            }

            var groups = await _groupRepository.GetGroupsByClassAsync(classId);
            var memberCounts = groups
                .Select(g => g.GroupMembers?.Count ?? 0)
                .Where(c => c > 0)
                .ToList();

            return new ResultModel<ClassSettingsResponseDto>
            {
                IsSuccess = true,
                Message = "Settings retrieved successfully",
                Data = new ClassSettingsResponseDto
                {
                    ClassId = classEntity.ClassId,
                    ClassName = classEntity.ClassName,
                    MaxGroups = classEntity.MaxGroups,
                    MaxMembersPerGroup = classEntity.MaxMembersPerGroup,
                    MinMembersPerGroup = classEntity.MinMembersPerGroup,
                    CurrentGroupCount = groups.Count,
                    LargestGroupSize = memberCounts.Any() ? memberCounts.Max() : null,
                    SmallestGroupSize = memberCounts.Any() ? memberCounts.Min() : null,
                    Warnings = new List<string>()
                }
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassSettingsResponseDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving settings: {ex.Message}",
                Data = null
            };
        }
    }
}
