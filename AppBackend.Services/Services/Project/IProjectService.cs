using AppBackend.BusinessObjects.Dtos.Project;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Project;

public interface IProjectService
{
    // Get projects by class with full details (Group, Leader, Members, Status)
    Task<ResultModel<List<ProjectGroupResponseDto>>> GetProjectsByClassAsync(int classId);
    
    // Get project by group with details
    Task<ProjectDetailDto> GetProjectByGroupAsync(int groupId);
    
    // Create new project (Leader only)
    Task<ProjectCreateResultDto> CreateProjectAsync(ProjectCreateDto dto, int leaderId);
    
    // Update project (Leader only)
    Task UpdateProjectAsync(ProjectUpdateDto dto);
    
    // Change project status (Instructor only)
    Task ChangeStatusAsync(ProjectStatusDto dto);
    
    // Delete project (Admin/Instructor/Leader)
    Task DeleteProjectAsync(int projectId, int requesterUserId);
}











