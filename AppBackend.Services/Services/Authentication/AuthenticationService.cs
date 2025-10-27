using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.ServicesHelpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly UserHelper _userHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationService(
            IUserRepository userRepository,
            IMapper mapper,
            UserHelper userHelper,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _userHelper = userHelper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResultModel> RegisterAsync(RegisterRequest request)
        {
            // Check email duplication
            var existing = await _userRepository.GetByEmailAsync(request.Email);
            if (existing != null)
                throw new AppException(
                    CommonMessageConstants.EXISTED,
                    string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Email"),
                    StatusCodes.Status400BadRequest
                );

            // Map & hash password
            var newUser = _mapper.Map<User>(request);
            newUser.PasswordHash = _userHelper.HashPassword(request.Password);
            
            // Set RoleId default to 3 (Student) for public registration
            newUser.RoleId = 3; // Student role
            
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();
            
            // Generate tokens
            var accessToken = _userHelper.CreateToken(newUser);
            var refreshToken = _userHelper.GenerateRefreshToken();
            var refreshExpiry = _userHelper.GetRefreshTokenExpiry();

            SaveRefreshTokenToSession(newUser.UserId, refreshToken, refreshExpiry);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.REGISTER_SUCCESS,
                Data = new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = refreshExpiry
                },
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !_userHelper.VerifyPassword(request.Password, user.PasswordHash ?? ""))
                throw new AppException(
                    CommonMessageConstants.UNAUTHORIZED,
                    CommonMessageConstants.PASSWORD_INCORRECT,
                    StatusCodes.Status401Unauthorized
                );

            var accessToken = _userHelper.CreateToken(user);
            var refreshToken = _userHelper.GenerateRefreshToken();
            var refreshExpiry = _userHelper.GetRefreshTokenExpiry();

            SaveRefreshTokenToSession(user.UserId, refreshToken, refreshExpiry);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.LOGIN_SUCCESS,
                Data = new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = refreshExpiry
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            
            if (session == null)
            {
                throw new AppException(
                    CommonMessageConstants.UNAUTHORIZED,
                    "Session not available",
                    StatusCodes.Status401Unauthorized
                );
            }

            // Get stored refresh token from session
            var storedRefreshToken = session.GetString("RefreshToken");
            var userIdStr = session.GetString("UserId");
            var refreshExpiryStr = session.GetString("RefreshExpiry");

            if (string.IsNullOrEmpty(storedRefreshToken) || 
                string.IsNullOrEmpty(userIdStr) || 
                string.IsNullOrEmpty(refreshExpiryStr))
            {
                throw new AppException(
                    CommonMessageConstants.UNAUTHORIZED,
                    "Refresh token not found or expired",
                    StatusCodes.Status401Unauthorized
                );
            }

            // Validate refresh token matches
            if (storedRefreshToken != request.RefreshToken)
            {
                throw new AppException(
                    CommonMessageConstants.UNAUTHORIZED,
                    "Invalid refresh token",
                    StatusCodes.Status401Unauthorized
                );
            }

            // Check if refresh token is expired
            if (DateTime.TryParse(refreshExpiryStr, out DateTime refreshExpiry))
            {
                if (refreshExpiry < DateTime.UtcNow)
                {
                    // Clear expired session
                    session.Clear();
                    throw new AppException(
                        CommonMessageConstants.UNAUTHORIZED,
                        "Refresh token has expired. Please login again",
                        StatusCodes.Status401Unauthorized
                    );
                }
            }

            // Get user from database
            if (!int.TryParse(userIdStr, out int userId))
            {
                throw new AppException(
                    CommonMessageConstants.UNAUTHORIZED,
                    "Invalid user session",
                    StatusCodes.Status401Unauthorized
                );
            }

            var user = await _userRepository.GetByIdWithRoleAsync(userId);
            if (user == null)
            {
                session.Clear();
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "User not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Generate new access token (keep same refresh token)
            var newAccessToken = _userHelper.CreateToken(user);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Token refreshed successfully",
                Data = new
                {
                    AccessToken = newAccessToken,
                    RefreshToken = storedRefreshToken, // Keep the same refresh token
                    RefreshTokenExpiry = refreshExpiry
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> LogoutAsync()
        {
            // Clear session data
            if (_httpContextAccessor.HttpContext?.Session != null)
            {
                _httpContextAccessor.HttpContext.Session.Remove("RefreshToken");
                _httpContextAccessor.HttpContext.Session.Remove("UserId");
                _httpContextAccessor.HttpContext.Session.Remove("RefreshExpiry");
                _httpContextAccessor.HttpContext.Session.Clear();
            }

            return await Task.FromResult(new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Logged out successfully",
                Data = null,
                StatusCode = StatusCodes.Status200OK
            });
        }

        #region Private Helpers

        private void SaveRefreshTokenToSession(int userId, string refreshToken, DateTime expiry)
        {
            if (_httpContextAccessor.HttpContext?.Session == null) return;

            _httpContextAccessor.HttpContext.Session.SetString("RefreshToken", refreshToken);
            _httpContextAccessor.HttpContext.Session.SetString("UserId", userId.ToString());
            _httpContextAccessor.HttpContext.Session.SetString("RefreshExpiry", expiry.ToString("O"));
        }

        #endregion
    }
}
