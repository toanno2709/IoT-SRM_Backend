using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.ProjectRepo;

public interface IProjectRepository : IGenericRepository<Project>
{
    Task<List<Project>> GetProjectsByClassAsync(int classId);
    Task<List<Project>> GetProjectsByGroupAsync(int groupId);
    Task<Project?> GetProjectWithDetailsAsync(int projectId);
    Task<Project?> GetByIdWithDetailsAsync(int projectId);
    Task<List<Project>> GetProjectsBySemesterAsync(int semesterId);
}





