using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Authentication
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Register a new user account
        /// </summary>
        Task<ResultModel> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Login with email and password
        /// </summary>
        Task<ResultModel> LoginAsync(LoginRequest request);

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        Task<ResultModel> RefreshTokenAsync(RefreshTokenRequest request);

        /// <summary>
        /// Logout current user (clear session)
        /// </summary>
        Task<ResultModel> LogoutAsync();
    }
}
