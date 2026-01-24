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
        private readonly FirebaseHelper _firebaseHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationService(
            IUserRepository userRepository,
            IMapper mapper,
            UserHelper userHelper,
            FirebaseHelper firebaseHelper,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _userHelper = userHelper;
            _firebaseHelper = firebaseHelper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResultModel> RegisterAsync(RegisterRequest request)
        {
            // Check email duplication
            var existing = await _userRepository.GetByEmailAsync(request.Email);
            if (existing != null)
                throw new AppException(
                    CommonMessageConstants.EXISTED,
                    CommonMessageConstants.EMAIL_ALREADY_EXISTS,
                    StatusCodes.Status409Conflict
                );

            // Map & hash password
            var newUser = _mapper.Map<User>(request);
            newUser.PasswordHash = _userHelper.HashPassword(request.Password);
            newUser.RoleId = 3; // Default role: Student
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();
            
            // Get user with role information
            var userWithRole = await _userRepository.GetByIdWithRoleAsync(newUser.UserId);
            if (userWithRole == null)
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    "Failed to retrieve user information after registration",
                    StatusCodes.Status500InternalServerError
                );

            // Generate tokens
            var accessToken = _userHelper.CreateToken(userWithRole);
            var refreshToken = _userHelper.GenerateRefreshToken();
            var refreshExpiry = _userHelper.GetRefreshTokenExpiry();

            SaveRefreshTokenToSession(userWithRole.UserId, refreshToken, refreshExpiry);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.REGISTER_SUCCESS,
                Data = new
                {
                    UserId = userWithRole.UserId,
                    Email = userWithRole.Email,
                    FullName = userWithRole.FullName,
                    RoleId = userWithRole.RoleId,
                    RoleName = userWithRole.Role?.RoleName,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = refreshExpiry
                },
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel> LoginAsync(LoginRequest request)
        {
            // Check if user exists (already includes Role via GetByEmailAsync)
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    CommonMessageConstants.USER_NOT_FOUND,
                    StatusCodes.Status404NotFound
                );

            // Verify password
            if (!_userHelper.VerifyPassword(request.Password, user.PasswordHash ?? ""))
                throw new AppException(
                    CommonMessageConstants.UNAUTHORIZED,
                    CommonMessageConstants.PASSWORD_INCORRECT,
                    StatusCodes.Status401Unauthorized
                );

            // Check if user account is active (if you have an IsActive field)
            // Uncomment if your User model has an IsActive property
            // if (user.IsActive == false)
            //     throw new AppException(
            //         CommonMessageConstants.FORBIDDEN,
            //         CommonMessageConstants.ACCOUNT_INACTIVE,
            //         StatusCodes.Status403Forbidden
            //     );

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
                    UserId = user.UserId,
                    Email = user.Email,
                    FullName = user.FullName,
                    RoleId = user.RoleId,
                    RoleName = user.Role?.RoleName,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = refreshExpiry
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GoogleLoginAsync(GoogleLoginRequest request)
        {
            try
            {
                // Step 1: Decode Firebase JWT token (no verification, just decode)
                var claims = _firebaseHelper.DecodeFirebaseToken(request.FirebaseToken);
                
                if (claims == null || !claims.Any())
                {
                    throw new AppException(
                        CommonMessageConstants.UNAUTHORIZED,
                        "Invalid Firebase token",
                        StatusCodes.Status401Unauthorized
                    );
                }

                // Step 2: Extract user information from token claims
                var (email, name, picture) = _firebaseHelper.ExtractUserInfo(claims);

                if (string.IsNullOrEmpty(email))
                {
                    throw new AppException(
                        CommonMessageConstants.INVALID,
                        "Email not found in Firebase token",
                        StatusCodes.Status400BadRequest
                    );
                }

                // Step 3: Check if user exists in database
                var user = await _userRepository.GetByEmailAsync(email);
                
                if (user == null)
                {
                    throw new AppException(
                        CommonMessageConstants.NOT_FOUND,
                        "The account you logged in with does not exist in the system. Please contact the administrator.",
                        StatusCodes.Status404NotFound
                    );
                }

                // Step 4: Update user information from Google (name and avatar)
                bool needsUpdate = false;
                
                if (!string.IsNullOrEmpty(name) && user.FullName != name)
                {
                    user.FullName = name;
                    needsUpdate = true;
                }
                
                if (!string.IsNullOrEmpty(picture) && user.AvatarUrl != picture)
                {
                    user.AvatarUrl = picture;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                    await _userRepository.SaveChangesAsync();
                }

                // Step 5: Generate JWT tokens for the system
                var accessToken = _userHelper.CreateToken(user);
                var refreshToken = _userHelper.GenerateRefreshToken();
                var refreshExpiry = _userHelper.GetRefreshTokenExpiry();

                SaveRefreshTokenToSession(user.UserId, refreshToken, refreshExpiry);

                // Step 6: Return response similar to regular login
                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Google login successful",
                    Data = new
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        FullName = user.FullName,
                        AvatarUrl = user.AvatarUrl,
                        RoleId = user.RoleId,
                        RoleName = user.Role?.RoleName,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        RefreshTokenExpiry = refreshExpiry
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    $"Google login failed: {ex.Message}",
                    StatusCodes.Status500InternalServerError
                );
            }
        }

        public async Task<ResultModel> LogoutAsync(int userId)
        {
            try
            {
                // Verify user exists
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    throw new AppException(
                        CommonMessageConstants.NOT_FOUND,
                        "User not found",
                        StatusCodes.Status404NotFound
                    );

                // Clear refresh token from session
                ClearRefreshTokenFromSession(userId);

                // Optional: Add logic to blacklist the current token or store it in a revoked tokens list
                // This would require additional implementation with a database table or cache

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Logout successful",
                    Data = new
                    {
                        UserId = userId,
                        LogoutTime = DateTime.UtcNow
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    $"Logout failed: {ex.Message}",
                    StatusCodes.Status500InternalServerError
                );
            }
        }

        public async Task<ResultModel> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Session == null)
                    throw new AppException(
                        CommonMessageConstants.UNAUTHORIZED,
                        CommonMessageConstants.SESSION_NOT_FOUND,
                        StatusCodes.Status401Unauthorized
                    );

                // Get stored refresh token from session
                var storedRefreshToken = httpContext.Session.GetString("RefreshToken");
                var userIdString = httpContext.Session.GetString("UserId");
                var expiryString = httpContext.Session.GetString("RefreshExpiry");

                if (string.IsNullOrEmpty(storedRefreshToken) || 
                    string.IsNullOrEmpty(userIdString) || 
                    refreshToken != storedRefreshToken)
                    throw new AppException(
                        CommonMessageConstants.UNAUTHORIZED,
                        CommonMessageConstants.REFRESH_TOKEN_INVALID,
                        StatusCodes.Status401Unauthorized
                    );

                // Check if refresh token is expired
                if (!string.IsNullOrEmpty(expiryString) && 
                    DateTime.TryParse(expiryString, out var expiry) && 
                    expiry < DateTime.UtcNow)
                    throw new AppException(
                        CommonMessageConstants.UNAUTHORIZED,
                        CommonMessageConstants.REFRESH_TOKEN_EXPIRED,
                        StatusCodes.Status401Unauthorized
                    );

                // Get user and generate new tokens
                if (!int.TryParse(userIdString, out var userId))
                    throw new AppException(
                        CommonMessageConstants.UNAUTHORIZED,
                        CommonMessageConstants.USER_SESSION_INVALID,
                        StatusCodes.Status401Unauthorized
                    );

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    throw new AppException(
                        CommonMessageConstants.NOT_FOUND,
                        CommonMessageConstants.USER_NOT_FOUND,
                        StatusCodes.Status404NotFound
                    );

                var newAccessToken = _userHelper.CreateToken(user);
                var newRefreshToken = _userHelper.GenerateRefreshToken();
                var newRefreshExpiry = _userHelper.GetRefreshTokenExpiry();

                SaveRefreshTokenToSession(user.UserId, newRefreshToken, newRefreshExpiry);

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = CommonMessageConstants.REFRESH_TOKEN_SUCCESS,
                    Data = new
                    {
                        AccessToken = newAccessToken,
                        RefreshToken = newRefreshToken,
                        RefreshTokenExpiry = newRefreshExpiry
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    $"Token refresh failed: {ex.Message}",
                    StatusCodes.Status500InternalServerError
                );
            }
        }

        // --- Private Helpers ---
        
        private void SaveRefreshTokenToSession(int userId, string refreshToken, DateTime expiry)
        {
            if (_httpContextAccessor.HttpContext?.Session == null) return;

            _httpContextAccessor.HttpContext.Session.SetString("RefreshToken", refreshToken);
            _httpContextAccessor.HttpContext.Session.SetString("UserId", userId.ToString());
            _httpContextAccessor.HttpContext.Session.SetString("RefreshExpiry", expiry.ToString("O"));
        }

        private void ClearRefreshTokenFromSession(int userId)
        {
            if (_httpContextAccessor.HttpContext?.Session == null) return;

            // Verify userId matches before clearing
            var storedUserId = _httpContextAccessor.HttpContext.Session.GetString("UserId");
            if (storedUserId == userId.ToString())
            {
                _httpContextAccessor.HttpContext.Session.Remove("RefreshToken");
                _httpContextAccessor.HttpContext.Session.Remove("UserId");
                _httpContextAccessor.HttpContext.Session.Remove("RefreshExpiry");
                _httpContextAccessor.HttpContext.Session.Clear();
            }
        }
    }
}
