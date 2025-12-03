using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.ProjectTemplateRepo;

public class ProjectTemplateRepository : IProjectTemplateRepository
{
    private readonly IotShowroomContext _context;

    public ProjectTemplateRepository(IotShowroomContext context)
    {
        _context = context;
    }

    public async Task<ProjectTemplate?> GetByIdAsync(int templateId)
    {
        return await _context.ProjectTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId);
    }

    public async Task<ProjectTemplate?> GetByIdWithDetailsAsync(int templateId)
    {
        return await _context.ProjectTemplates
            .Include(t => t.Class)
            .Include(t => t.Creator)
            .Include(t => t.TemplateMilestones.OrderBy(m => m.OrderIndex))
            .Include(t => t.ProjectTemplateRegistrations)
                .ThenInclude(r => r.Group)
            .FirstOrDefaultAsync(t => t.TemplateId == templateId);
    }

    public async Task<List<ProjectTemplate>> GetByClassIdAsync(int classId)
    {
        return await _context.ProjectTemplates
            .Include(t => t.Creator)
            .Include(t => t.TemplateMilestones.OrderBy(m => m.OrderIndex))
            .Where(t => t.ClassId == classId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ProjectTemplate>> GetAvailableByClassIdAsync(int classId)
    {
        return await _context.ProjectTemplates
            .Include(t => t.TemplateMilestones.OrderBy(m => m.OrderIndex))
            .Where(t => t.ClassId == classId 
                     && t.IsActive 
                     && (t.MaxGroups == null || t.RegisteredCount < t.MaxGroups))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProjectTemplate> CreateAsync(ProjectTemplate template)
    {
        _context.ProjectTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<ProjectTemplate> UpdateAsync(ProjectTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        _context.ProjectTemplates.Update(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<bool> DeleteAsync(int templateId)
    {
        var template = await GetByIdAsync(templateId);
        if (template == null) return false;

        _context.ProjectTemplates.Remove(template);
        await _context.SaveChangesAsync();
        return true;
    }

    // Registration methods
    public async Task<ProjectTemplateRegistration?> GetRegistrationByIdAsync(int registrationId)
    {
        return await _context.ProjectTemplateRegistrations
            .Include(r => r.ProjectTemplate)
            .Include(r => r.Group)
            .Include(r => r.Project)
            .Include(r => r.RegisteredByUser)
            .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);
    }

    public async Task<ProjectTemplateRegistration?> GetRegistrationByGroupAndTemplateAsync(int groupId, int templateId)
    {
        return await _context.ProjectTemplateRegistrations
            .Include(r => r.ProjectTemplate)
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.GroupId == groupId && r.TemplateId == templateId);
    }

    public async Task<List<ProjectTemplateRegistration>> GetRegistrationsByTemplateIdAsync(int templateId)
    {
        return await _context.ProjectTemplateRegistrations
            .Include(r => r.Group)
            .Include(r => r.Project)
            .Include(r => r.RegisteredByUser)
            .Where(r => r.TemplateId == templateId)
            .OrderByDescending(r => r.RegisteredAt)
            .ToListAsync();
    }

    public async Task<List<ProjectTemplateRegistration>> GetRegistrationsByGroupIdAsync(int groupId)
    {
        return await _context.ProjectTemplateRegistrations
            .Include(r => r.ProjectTemplate)
            .Include(r => r.Project)
            .Where(r => r.GroupId == groupId)
            .OrderByDescending(r => r.RegisteredAt)
            .ToListAsync();
    }

    public async Task<ProjectTemplateRegistration> CreateRegistrationAsync(ProjectTemplateRegistration registration)
    {
        _context.ProjectTemplateRegistrations.Add(registration);
        await _context.SaveChangesAsync();
        return registration;
    }

    public async Task<bool> CancelRegistrationAsync(int registrationId)
    {
        var registration = await _context.ProjectTemplateRegistrations
            .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);
        
        if (registration == null) return false;

        registration.Status = "Cancelled";
        await _context.SaveChangesAsync();
        return true;
    }

    // Statistics methods
    public async Task<bool> HasAvailableSlotsAsync(int templateId)
    {
        var template = await GetByIdAsync(templateId);
        if (template == null) return false;
        
        return template.IsActive && 
               (template.MaxGroups == null || template.RegisteredCount < template.MaxGroups);
    }

    public async Task<int> GetRegisteredCountAsync(int templateId)
    {
        return await _context.ProjectTemplateRegistrations
            .CountAsync(r => r.TemplateId == templateId && r.Status == "Active");
    }

    public async Task<bool> IsGroupRegisteredAsync(int groupId, int templateId)
    {
        return await _context.ProjectTemplateRegistrations
            .AnyAsync(r => r.GroupId == groupId 
                        && r.TemplateId == templateId 
                        && r.Status == "Active");
    }
}
