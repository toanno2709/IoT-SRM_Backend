using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.ClassConfig;

public interface IClassConfigService
{
    Task<ResultModel<ClassConfigResponseDto>> GetConfigAsync(int classId);
    Task<ResultModel<ClassConfigResponseDto>> UpdateConfigAsync(int classId, ClassConfigUpdateDto dto, int instructorId);
    Task<ResultModel<ClassConfigResponseDto>> CreateDefaultConfigAsync(int classId);
    Task<ResultModel<GroupValidationDto>> ValidateGroupCreationAsync(int classId, int memberCount);
    Task<ResultModel<bool>> CanCreateGroupAsync(int classId);
    Task<ResultModel<SubmissionDeadlineValidationDto>> ValidateSubmissionDeadlineAsync(int classId);
    Task<ResultModel<EditWindowValidationDto>> ValidateEditWindowAsync(int classId);
}
