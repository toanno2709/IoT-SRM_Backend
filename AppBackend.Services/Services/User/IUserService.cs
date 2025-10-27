using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services
{
    public interface IUserService
    {
<<<<<<< Updated upstream
        Task<ResultModel> RegisterAsync(RegisterRequest request);
        Task<ResultModel> LoginAsync(LoginRequest request);
        Task<ResultModel> GetAllUsersAsync();
        Task<ResultModel> GetUserByIdAsync(int id);
    }
}