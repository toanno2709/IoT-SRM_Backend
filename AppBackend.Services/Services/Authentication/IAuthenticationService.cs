using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<ResultModel> RegisterAsync(RegisterRequest request);
        Task<ResultModel> LoginAsync(LoginRequest request);
        Task<ResultModel> LogoutAsync(int userId);
        Task<ResultModel> RefreshTokenAsync(string refreshToken);
    }
}
