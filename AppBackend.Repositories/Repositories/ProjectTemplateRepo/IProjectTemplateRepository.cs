using AppBackend.BusinessObjects.Models;

namespace AppBackend.Repositories.Repositories.ProjectTemplateRepo;

public interface IProjectTemplateRepository
{
    // Template CRUD
    Task<ProjectTemplate?> GetByIdAsync(int templateId);
    Task<ProjectTemplate?> GetByIdWithDetailsAsync(int templateId);
    Task<List<ProjectTemplate>> GetByClassIdAsync(int classId);
    Task<List<ProjectTemplate>> GetAvailableByClassIdAsync(int classId);
    Task<ProjectTemplate> CreateAsync(ProjectTemplate template);
    Task<ProjectTemplate> UpdateAsync(ProjectTemplate template);
    Task<bool> DeleteAsync(int templateId);
    
    // Registration
    Task<ProjectTemplateRegistration?> GetRegistrationByIdAsync(int registrationId);
    Task<ProjectTemplateRegistration?> GetRegistrationByGroupAndTemplateAsync(int groupId, int templateId);
    Task<List<ProjectTemplateRegistration>> GetRegistrationsByTemplateIdAsync(int templateId);
    Task<List<ProjectTemplateRegistration>> GetRegistrationsByGroupIdAsync(int groupId);
    Task<ProjectTemplateRegistration> CreateRegistrationAsync(ProjectTemplateRegistration registration);
    Task<bool> CancelRegistrationAsync(int registrationId);
    
    // Statistics
    Task<bool> HasAvailableSlotsAsync(int templateId);
    Task<int> GetRegisteredCountAsync(int templateId);
    Task<bool> IsGroupRegisteredAsync(int groupId, int templateId);
}
