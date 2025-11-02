using AppBackend.BusinessObjects.Dtos.Project;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.Project;

public interface IProjectService
{
    Task<ResultModel<List<ProjectGroupResponseDto>>> GetProjectsByClassAsync(int classId);
    Task<ProjectCreateResultDto> CreateProjectAsync(ProjectCreateDto dto, int leaderId);
    Task UpdateProjectAsync(ProjectUpdateDto dto);
    Task<IEnumerable<ProjectListItemDto>> GetProjectsByClassAsync1(int classId);
    Task<ProjectDetailDto> GetProjectByGroupAsync(int groupId);
    Task ChangeStatusAsync(ProjectStatusDto dto);
    Task DeleteProjectAsync(int projectId, int requesterUserId);
}











