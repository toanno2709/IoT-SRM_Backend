using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.ProjectTemplate;

public interface IProjectTemplateService
{
    // Instructor APIs
    Task<ResultModel<ProjectTemplateResponseDto>> CreateTemplateAsync(CreateProjectTemplateDto dto, int instructorId);
    Task<ResultModel<ImportTemplatesResponseDto>> ImportTemplatesFromExcelAsync(ImportTemplatesFromExcelRequestDto request, int instructorId);
    Task<ResultModel<ProjectTemplateResponseDto>> GetTemplateByIdAsync(int templateId, int instructorId);
    Task<ResultModel<List<ProjectTemplateResponseDto>>> GetTemplatesByClassIdAsync(int classId, int instructorId);
    Task<ResultModel<ProjectTemplateResponseDto>> UpdateTemplateAsync(int templateId, UpdateProjectTemplateDto dto, int instructorId);
    Task<ResultModel<bool>> DeleteTemplateAsync(int templateId, int instructorId);
    Task<ResultModel<List<TemplateRegistrationListDto>>> GetTemplateRegistrationsAsync(int templateId, int instructorId);
    Task<ResultModel<TemplateStatisticsDto>> GetTemplateStatisticsAsync(int templateId, int instructorId);
    
    // Student APIs
    Task<ResultModel<List<AvailableTemplateDto>>> GetAvailableTemplatesAsync(int classId, int studentId);
    Task<ResultModel<TemplateRegistrationResponseDto>> RegisterToTemplateAsync(RegisterTemplateDto dto, int studentId);
    Task<ResultModel<bool>> CancelRegistrationAsync(int registrationId, int studentId);
    Task<ResultModel<List<MyGroupRegistrationDto>>> GetMyGroupRegistrationsAsync(int studentId);
}
