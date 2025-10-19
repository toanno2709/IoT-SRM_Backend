using AppBackend.BusinessObjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AppBackend.Repositories.Repositories.GroupRepo
{
    public interface IGroupRepository
    {
        Task<IEnumerable<Group>> GetAllAsync();
        Task<Group?> GetByIdAsync(int groupId);
        Task<Group> CreateGroupAsync(Group group, int leaderId);
        Task<bool> AddMemberAsync(int groupId, int userId, string role);
        Task<bool> CheckStudentInClassGroupAsync(int classId, int userId);

    }
}
