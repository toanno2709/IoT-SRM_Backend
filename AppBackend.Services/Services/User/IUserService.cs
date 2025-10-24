using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services
{
    public interface IUserService
    {
        #region Authentication
        Task<ResultModel> RegisterAsync(RegisterRequest request);
        Task<ResultModel> LoginAsync(LoginRequest request);
        #endregion

        #region Admin - User Management
        Task<ResultModel> GetAllUsersAsync();
        Task<ResultModel> GetUserByIdAsync(int id);
        Task<ResultModel<UserResponseDto>> CreateUserAsync(CreateUserRequest request);
        Task<ResultModel<UserResponseDto>> UpdateUserAsync(int userId, UpdateUserRequest request);
        Task<ResultModel<List<UserResponseDto>>> GetUsersByRoleAsync(int? roleId);
        Task<ResultModel<bool>> DeleteUserAsync(int userId);
        #endregion
    }
}