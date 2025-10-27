using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.ServicesHelpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly UserHelper _userHelper;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            UserHelper userHelper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _userHelper = userHelper;
        }

<<<<<<< Updated upstream
        public async Task<ResultModel> RegisterAsync(RegisterRequest request)
=======
        #region User Profile

        public async Task<ResultModel> GetCurrentUserAsync(int userId)
>>>>>>> Stashed changes
        {
            var user = await _userRepository.GetByIdWithRoleAsync(userId);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "User"),
                    StatusCodes.Status404NotFound
                );

<<<<<<< Updated upstream
            // Map & hash password
            var newUser = _mapper.Map<User>(request);
            newUser.PasswordHash = _userHelper.HashPassword(request.Password);
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();
            var role = await _userRepository.GetByIdAsync(newUser.RoleId ?? 3);
            
            // Generate tokens
            var accessToken = _userHelper.CreateToken(newUser);
            var refreshToken = _userHelper.GenerateRefreshToken();
            var refreshExpiry = _userHelper.GetRefreshTokenExpiry();

            SaveRefreshTokenToSession(newUser.UserId, refreshToken, refreshExpiry);
=======
            var dto = MapToUserResponseDto(user);
>>>>>>> Stashed changes

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = dto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = userDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "User"),
                    StatusCodes.Status404NotFound
                );

            var dto = _mapper.Map<UserDto>(user);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = dto,
                StatusCode = StatusCodes.Status200OK
            };
        }

<<<<<<< Updated upstream
        // --- Private Helper ---
        private void SaveRefreshTokenToSession(int userId, string refreshToken, DateTime expiry)
        {
            if (_httpContextAccessor.HttpContext?.Session == null) return;

            _httpContextAccessor.HttpContext.Session.SetString("RefreshToken", refreshToken);
            _httpContextAccessor.HttpContext.Session.SetString("UserId", userId.ToString());
            _httpContextAccessor.HttpContext.Session.SetString("RefreshExpiry", expiry.ToString("O"));
        }
=======
        public async Task<ResultModel<UserResponseDto>> CreateUserAsync(CreateUserRequest request)
        {
            // Check email duplication
            var existingEmail = await _userRepository.EmailExistsAsync(request.Email);
            if (existingEmail)
            {
                return new ResultModel<UserResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "DUPLICATE_EMAIL",
                    Message = "Email already exists",
                    Data = null,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            // Validate RoleId exists
            var roleExists = await _roleRepository.GetByIdAsync(request.RoleId);
            if (roleExists == null)
            {
                return new ResultModel<UserResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_ROLE",
                    Message = $"Role with ID {request.RoleId} does not exist",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Create new user
            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                RoleId = request.RoleId,
                PasswordHash = _userHelper.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            // Get user with role for response
            var createdUser = await _userRepository.GetByIdWithRoleAsync(newUser.UserId);
            var responseDto = MapToUserResponseDto(createdUser!);

            return new ResultModel<UserResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "User created successfully",
                Data = responseDto,
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel<UserResponseDto>> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            var user = await _userRepository.GetByIdWithRoleAsync(userId);
            if (user == null)
            {
                return new ResultModel<UserResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "NOT_FOUND",
                    Message = "User not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName;

            if (!string.IsNullOrWhiteSpace(request.Phone))
                user.Phone = request.Phone;

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                user.AvatarUrl = request.AvatarUrl;

            // Reset password if provided
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                user.PasswordHash = _userHelper.HashPassword(request.NewPassword);
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            // Get updated user with role
            var updatedUser = await _userRepository.GetByIdWithRoleAsync(userId);
            var responseDto = MapToUserResponseDto(updatedUser!);

            return new ResultModel<UserResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "User updated successfully",
                Data = responseDto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<List<UserResponseDto>>> GetUsersByRoleAsync(int? roleId)
        {
            List<User> users;

            if (roleId.HasValue)
            {
                // Validate role exists
                var roleExists = await _roleRepository.GetByIdAsync(roleId.Value);
                if (roleExists == null)
                {
                    return new ResultModel<List<UserResponseDto>>
                    {
                        IsSuccess = false,
                        ResponseCode = "INVALID_ROLE",
                        Message = $"Role with ID {roleId.Value} does not exist",
                        Data = null,
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                users = await _userRepository.GetByRoleAsync(roleId.Value);
            }
            else
            {
                // Get all users if no roleId specified
                var allUsers = await _userRepository.GetAllAsync();
                users = allUsers.ToList();
            }

            var responseDtos = users.Select(MapToUserResponseDto).ToList();

            return new ResultModel<List<UserResponseDto>>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Users retrieved successfully",
                Data = responseDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<bool>> DeleteUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = "NOT_FOUND",
                    Message = "User not found",
                    Data = false,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            await _userRepository.DeleteAsync(user);
            await _userRepository.SaveChangesAsync();

            return new ResultModel<bool>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "User deleted successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion

        #region Private Helpers

        private UserResponseDto MapToUserResponseDto(User user)
        {
            return new UserResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        #endregion
>>>>>>> Stashed changes
    }
}
