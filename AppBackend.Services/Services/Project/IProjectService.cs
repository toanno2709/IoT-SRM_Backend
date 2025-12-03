using AppBackend.BusinessObjects.Dtos.Project;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Project;

public interface IProjectService
{
    // Get projects by class with full details (Group, Leader, Members, Status)
    Task<ResultModel<List<ProjectGroupResponseDto>>> GetProjectsByClassAsync(int classId);
    
    // Get projects by group with details (returns list because 1 group can have multiple projects)
    Task<ResultModel<List<ProjectDetailDto>>> GetProjectsByGroupAsync(int groupId);
    
    // Create new project (Leader only)
    Task<ProjectCreateResultDto> CreateProjectAsync(ProjectCreateDto dto, int leaderId);
    
    // Update project (Leader only)
    Task UpdateProjectAsync(ProjectUpdateDto dto);
    
    // Change project status (Instructor only)
    Task ChangeStatusAsync(ProjectStatusDto dto);
    
    // Delete project (Admin/Instructor/Leader)
    Task DeleteProjectAsync(int projectId, int requesterUserId);
    
    /// <summary>
    /// Update project status with comment (Instructor only)
    /// Creates a record in ProjectApprovalHistory so students can view the comment
    /// </summary>
    Task<ResultModel<UpdateProjectStatusResponseDto>> UpdateProjectStatusAsync(int projectId, UpdateProjectStatusRequestDto request, int instructorId);
    
    /// <summary>
    /// Get project status history with comments (for students to view)
    /// </summary>
    Task<ResultModel<List<ProjectStatusHistoryDto>>> GetProjectStatusHistoryAsync(int projectId);
}





















