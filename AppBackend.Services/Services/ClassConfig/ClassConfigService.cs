using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.ClassConfigRepo;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.GroupRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.ClassConfig;

public class ClassConfigService : IClassConfigService
{
    private readonly IClassConfigRepository _configRepository;
    private readonly IClassRepository _classRepository;
    private readonly IGroupRepository _groupRepository;

    public ClassConfigService(
        IClassConfigRepository configRepository,
        IClassRepository classRepository,
        IGroupRepository groupRepository)
    {
        _configRepository = configRepository;
        _classRepository = classRepository;
        _groupRepository = groupRepository;
    }

    public async Task<ResultModel<ClassConfigResponseDto>> GetConfigAsync(int classId)
    {
        try
        {
            // Check if class exists
            var classEntity = await _classRepository.GetByIdAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<ClassConfigResponseDto>
                {
                    IsSuccess = false,
                    Message = "Class not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Get config
            var config = await _configRepository.GetByClassIdWithDetailsAsync(classId);
            
            // If no config exists, create default
            if (config == null)
            {
                var createResult = await CreateDefaultConfigAsync(classId);
                if (!createResult.IsSuccess)
                    return createResult;
                
                config = await _configRepository.GetByClassIdWithDetailsAsync(classId);
            }

            // Get current group count
            var groups = await _groupRepository.GetGroupsByClassAsync(classId);
            var currentGroupCount = groups.Count;

            // Map to DTO
            var response = new ClassConfigResponseDto
            {
                ConfigId = config!.ConfigId,
                ClassId = config.ClassId,
                ClassName = config.Class?.ClassName,
                MaxGroupsAllowed = config.MaxGroupsAllowed,
                MinMembersPerGroup = config.MinMembersPerGroup,
                MaxMembersPerGroup = config.MaxMembersPerGroup,
                GroupFormationDeadline = config.GroupFormationDeadline,
                AllowStudentCreateGroup = config.AllowStudentCreateGroup,
                
                // Milestone Submission Settings
                SubmissionStartDate = config.SubmissionStartDate,
                SubmissionDeadline = config.SubmissionDeadline,
                AllowLateSubmission = config.AllowLateSubmission,
                LateSubmissionPenaltyPercent = config.LateSubmissionPenaltyPercent,
                
                // Milestone Edit Window Settings
                EditWindowStartDate = config.EditWindowStartDate,
                EditWindowEndDate = config.EditWindowEndDate,
                
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt,
                CurrentGroupCount = currentGroupCount,
                IsGroupFormationOpen = config.IsGroupFormationAllowed(),
                GroupFormationStatus = GetDeadlineStatus(config.GroupFormationDeadline),
                
                // Submission Period Status
                SubmissionPeriodStatus = config.GetSubmissionPeriodStatus(),
                CanSubmitNow = config.IsWithinSubmissionPeriod(),
                IsSubmissionLate = config.IsSubmissionLate(),
                
                // Edit Window Status
                EditWindowStatus = config.GetEditWindowStatus(),
                CanEditNow = config.IsWithinEditWindow()
            };

            return new ResultModel<ClassConfigResponseDto>
            {
                IsSuccess = true,
                Message = "Configuration retrieved successfully",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassConfigResponseDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving configuration: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<ClassConfigResponseDto>> UpdateConfigAsync(int classId, ClassConfigUpdateDto dto, int instructorId)
    {
        try
        {
            // Validate DTO
            if (!dto.IsValid(out string errorMessage))
            {
                return new ResultModel<ClassConfigResponseDto>
                {
                    IsSuccess = false,
                    Message = errorMessage,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Check if class exists and instructor owns it
            var classEntity = await _classRepository.GetByIdAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<ClassConfigResponseDto>
                {
                    IsSuccess = false,
                    Message = "Class not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (classEntity.InstructorId != instructorId)
            {
                return new ResultModel<ClassConfigResponseDto>
                {
                    IsSuccess = false,
                    Message = "You are not authorized to configure this class",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Get or create config
            var config = await _configRepository.GetByClassIdAsync(classId);
            if (config == null)
            {
                var createResult = await CreateDefaultConfigAsync(classId);
                if (!createResult.IsSuccess)
                    return createResult;
                
                config = await _configRepository.GetByClassIdAsync(classId);
            }

            // Update basic fields if provided
            if (dto.MaxGroupsAllowed.HasValue)
            {
                // Validate against current group count
                var currentGroupCount = (await _groupRepository.GetGroupsByClassAsync(classId)).Count;
                if (dto.MaxGroupsAllowed.Value < currentGroupCount)
                {
                    return new ResultModel<ClassConfigResponseDto>
                    {
                        IsSuccess = false,
                        Message = $"Cannot set max groups to {dto.MaxGroupsAllowed.Value}. There are already {currentGroupCount} groups in this class.",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
                config!.MaxGroupsAllowed = dto.MaxGroupsAllowed.Value;
            }

            if (dto.MinMembersPerGroup.HasValue)
            {
                config!.MinMembersPerGroup = dto.MinMembersPerGroup.Value;
            }

            if (dto.MaxMembersPerGroup.HasValue)
            {
                config!.MaxMembersPerGroup = dto.MaxMembersPerGroup.Value;
            }

            if (dto.GroupFormationDeadline.HasValue)
            {
                config!.GroupFormationDeadline = dto.GroupFormationDeadline.Value;
            }

            if (dto.AllowStudentCreateGroup.HasValue)
            {
                config!.AllowStudentCreateGroup = dto.AllowStudentCreateGroup.Value;
            }

            // Update milestone submission settings
            if (dto.SubmissionStartDate.HasValue)
            {
                config!.SubmissionStartDate = dto.SubmissionStartDate.Value;
            }

            if (dto.SubmissionDeadline.HasValue)
            {
                config!.SubmissionDeadline = dto.SubmissionDeadline.Value;
            }

            if (dto.AllowLateSubmission.HasValue)
            {
                config!.AllowLateSubmission = dto.AllowLateSubmission.Value;
            }

            if (dto.LateSubmissionPenaltyPercent.HasValue)
            {
                config!.LateSubmissionPenaltyPercent = dto.LateSubmissionPenaltyPercent.Value;
            }

            // Update milestone edit window settings
            if (dto.EditWindowStartDate.HasValue)
            {
                config!.EditWindowStartDate = dto.EditWindowStartDate.Value;
            }

            if (dto.EditWindowEndDate.HasValue)
            {
                config!.EditWindowEndDate = dto.EditWindowEndDate.Value;
            }

            config!.UpdatedAt = DateTime.UtcNow;

            // Validate final config
            if (!config.IsValid())
            {
                return new ResultModel<ClassConfigResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid configuration: Please check all date ranges and member limits",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            await _configRepository.UpdateAsync(config);
            await _configRepository.SaveChangesAsync();

            // Return updated config
            return await GetConfigAsync(classId);
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassConfigResponseDto>
            {
                IsSuccess = false,
                Message = $"Error updating configuration: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<ClassConfigResponseDto>> CreateDefaultConfigAsync(int classId)
    {
        try
        {
            // Check if class exists
            var classEntity = await _classRepository.GetByIdAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<ClassConfigResponseDto>
                {
                    IsSuccess = false,
                    Message = "Class not found",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check if config already exists
            if (await _configRepository.ExistsForClassAsync(classId))
            {
                return new ResultModel<ClassConfigResponseDto>
                {
                    IsSuccess = false,
                    Message = "Configuration already exists for this class",
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            // Create default config
            var config = new ClassConfiguration
            {
                ClassId = classId,
                MaxGroupsAllowed = 20,
                MinMembersPerGroup = 2,
                MaxMembersPerGroup = 5,
                AllowStudentCreateGroup = true,
                AllowLateSubmission = true,
                LateSubmissionPenaltyPercent = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _configRepository.AddAsync(config);
            await _configRepository.SaveChangesAsync();

            return await GetConfigAsync(classId);
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassConfigResponseDto>
            {
                IsSuccess = false,
                Message = $"Error creating default configuration: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<GroupValidationDto>> ValidateGroupCreationAsync(int classId, int memberCount)
    {
        try
        {
            var config = await _configRepository.GetByClassIdAsync(classId);
            if (config == null)
            {
                // No config = allow by default
                return new ResultModel<GroupValidationDto>
                {
                    IsSuccess = true,
                    Data = new GroupValidationDto
                    {
                        ClassId = classId,
                        ProposedMemberCount = memberCount,
                        IsValid = true
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }

            var validation = new GroupValidationDto
            {
                ClassId = classId,
                ProposedMemberCount = memberCount,
                IsValid = true
            };

            // Check group formation deadline
            if (!config.IsGroupFormationAllowed())
            {
                validation.IsValid = false;
                validation.ValidationErrors.Add("Group formation deadline has passed");
            }

            // Check if student group creation is allowed
            if (!config.AllowStudentCreateGroup)
            {
                validation.IsValid = false;
                validation.ValidationErrors.Add("Student group creation is not allowed for this class");
            }

            // Check max groups
            var currentGroupCount = (await _groupRepository.GetGroupsByClassAsync(classId)).Count;
            if (currentGroupCount >= config.MaxGroupsAllowed)
            {
                validation.IsValid = false;
                validation.ValidationErrors.Add($"Maximum number of groups ({config.MaxGroupsAllowed}) reached");
            }

            // Check member count
            if (memberCount < config.MinMembersPerGroup)
            {
                validation.ValidationErrors.Add($"Group must have at least {config.MinMembersPerGroup} members");
                validation.IsValid = false;
            }

            if (memberCount > config.MaxMembersPerGroup)
            {
                validation.ValidationErrors.Add($"Group cannot have more than {config.MaxMembersPerGroup} members");
                validation.IsValid = false;
            }

            // Warnings (not blocking)
            if (config.GroupFormationDeadline.HasValue)
            {
                var daysUntilDeadline = (config.GroupFormationDeadline.Value - DateTime.UtcNow).TotalDays;
                if (daysUntilDeadline <= 7)
                {
                    validation.Warnings.Add($"Group formation deadline is in {Math.Ceiling(daysUntilDeadline)} days");
                }
            }

            return new ResultModel<GroupValidationDto>
            {
                IsSuccess = true,
                Data = validation,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<GroupValidationDto>
            {
                IsSuccess = false,
                Message = $"Error validating group creation: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<bool>> CanCreateGroupAsync(int classId)
    {
        try
        {
            var config = await _configRepository.GetByClassIdAsync(classId);
            if (config == null)
                return new ResultModel<bool> { IsSuccess = true, Data = true };

            var canCreate = config.IsGroupFormationAllowed() && 
                          config.AllowStudentCreateGroup;

            var currentGroupCount = (await _groupRepository.GetGroupsByClassAsync(classId)).Count;
            canCreate = canCreate && currentGroupCount < config.MaxGroupsAllowed;

            return new ResultModel<bool>
            {
                IsSuccess = true,
                Data = canCreate,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                Message = $"Error checking group creation: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<SubmissionDeadlineValidationDto>> ValidateSubmissionDeadlineAsync(int classId)
    {
        try
        {
            var config = await _configRepository.GetByClassIdAsync(classId);
            
            var validation = new SubmissionDeadlineValidationDto
            {
                CanSubmit = true,
                IsLate = false,
                Status = "Open"
            };

            if (config == null)
            {
                validation.Message = "No deadline configuration set - submissions allowed";
                return new ResultModel<SubmissionDeadlineValidationDto>
                {
                    IsSuccess = true,
                    Data = validation,
                    StatusCode = StatusCodes.Status200OK
                };
            }

            validation.DeadlineDate = config.SubmissionDeadline;
            validation.Status = config.GetSubmissionPeriodStatus();
            validation.CanSubmit = config.IsWithinSubmissionPeriod();
            validation.IsLate = config.IsSubmissionLate();

            if (validation.IsLate && config.AllowLateSubmission)
            {
                validation.ApplicablePenaltyPercent = config.LateSubmissionPenaltyPercent;
                validation.Message = $"Submission is late. A penalty of {config.LateSubmissionPenaltyPercent}% will be applied.";
            }
            else if (validation.IsLate && !config.AllowLateSubmission)
            {
                validation.CanSubmit = false;
                validation.Message = "Submission deadline has passed and late submissions are not allowed.";
            }
            else if (validation.Status == "NotStarted")
            {
                validation.CanSubmit = false;
                validation.Message = $"Submission period has not started yet. Opens on {config.SubmissionStartDate:yyyy-MM-dd HH:mm}";
            }
            else
            {
                validation.Message = "Submission is on time.";
            }

            return new ResultModel<SubmissionDeadlineValidationDto>
            {
                IsSuccess = true,
                Data = validation,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<SubmissionDeadlineValidationDto>
            {
                IsSuccess = false,
                Message = $"Error validating submission deadline: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<EditWindowValidationDto>> ValidateEditWindowAsync(int classId)
    {
        try
        {
            var config = await _configRepository.GetByClassIdAsync(classId);
            
            var validation = new EditWindowValidationDto
            {
                CanEdit = false,
                Status = "NotConfigured"
            };

            if (config == null)
            {
                validation.Message = "No edit window configuration set - editing not allowed";
                return new ResultModel<EditWindowValidationDto>
                {
                    IsSuccess = true,
                    Data = validation,
                    StatusCode = StatusCodes.Status200OK
                };
            }

            validation.WindowStartDate = config.EditWindowStartDate;
            validation.WindowEndDate = config.EditWindowEndDate;
            validation.Status = config.GetEditWindowStatus();
            validation.CanEdit = config.IsWithinEditWindow();

            if (validation.Status == "NotConfigured")
            {
                validation.Message = "Edit window is not configured for this class.";
            }
            else if (validation.Status == "NotStarted")
            {
                validation.Message = $"Edit window has not started yet. Opens on {config.EditWindowStartDate:yyyy-MM-dd HH:mm}";
            }
            else if (validation.Status == "Closed")
            {
                validation.Message = $"Edit window has closed on {config.EditWindowEndDate:yyyy-MM-dd HH:mm}";
            }
            else
            {
                validation.Message = $"Edit window is open until {config.EditWindowEndDate:yyyy-MM-dd HH:mm}";
            }

            return new ResultModel<EditWindowValidationDto>
            {
                IsSuccess = true,
                Data = validation,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<EditWindowValidationDto>
            {
                IsSuccess = false,
                Message = $"Error validating edit window: {ex.Message}",
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    private string GetDeadlineStatus(DateTime? deadline)
    {
        if (deadline == null)
            return "No deadline set";

        var now = DateTime.UtcNow;
        if (now > deadline.Value)
            return "Expired";

        var daysRemaining = (deadline.Value - now).TotalDays;
        if (daysRemaining <= 1)
            return $"Expires in {Math.Ceiling(daysRemaining * 24)} hours";
        
        return $"Expires in {Math.Ceiling(daysRemaining)} days";
    }
}
