using AppBackend.BusinessObjects.Models;

namespace AppBackend.Repositories.Repositories.GroupRepo;

public interface IGroupRepository
{
    Task<IEnumerable<Group>> GetAllAsync();
    Task<Group?> GetByIdAsync(int groupId);
    Task<Group> CreateGroupAsync(Group group, int leaderId);
    Task<bool> AddMemberAsync(int groupId, int userId, string role);
    Task<bool> CheckStudentInClassGroupAsync(int classId, int userId);
    Task<List<Group>> GetGroupsByClassAsync(int classId);
    Task<Group?> GetGroupWithDetailsAsync(int groupId);
}


