using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services
{
    public interface IUserService
    {
        Task<ResultModel> GetAllUsersAsync();
        Task<ResultModel> GetUserByIdAsync(int id);
        Task<ResultModel> GetCurrentUserInfoAsync(int userId);
        Task<ResultModel> CreateUserAsync(CreateUserRequest request);
        Task<ResultModel> UpdateUserAsync(int id, UpdateUserRequest request, int requesterId, bool isAdmin);
        Task<ResultModel> DeleteUserAsync(int id);
        Task<ResultModel> GetUsersByRoleAsync(int? roleId);
    }
}