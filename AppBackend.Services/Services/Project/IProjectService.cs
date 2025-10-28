using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Project;

public interface IProjectService
{
    Task<ResultModel<List<ProjectGroupResponseDto>>> GetProjectsByClassAsync(int classId);
}





