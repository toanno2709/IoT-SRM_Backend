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

        public async Task<ResultModel> GetCurrentUserInfoAsync(int userId)
        {
            var user = await _userRepository.GetByIdWithRoleAsync(userId);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "User"),
                    StatusCodes.Status404NotFound
                );

            var userResponse = _mapper.Map<UserResponseDto>(user);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Current user information retrieved successfully",
                Data = userResponse,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> CreateUserAsync(CreateUserRequest request)
        {
            // Check email duplication
            var existing = await _userRepository.GetByEmailAsync(request.Email);
            if (existing != null)
                throw new AppException(
                    CommonMessageConstants.EXISTED,
                    string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Email"),
                    StatusCodes.Status409Conflict
                );

            // Verify role exists (you may need to add a RoleRepository check here)
            // For now, we'll assume the role exists

            // Map & hash password
            var newUser = _mapper.Map<User>(request);
            newUser.PasswordHash = _userHelper.HashPassword(request.Password);
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            // Get user with role information
            var createdUser = await _userRepository.GetByIdWithRoleAsync(newUser.UserId);
            var userResponse = _mapper.Map<UserResponseDto>(createdUser);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "User created successfully",
                Data = userResponse,
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel> UpdateUserAsync(int id, UpdateUserRequest request, int requesterId, bool isAdmin)
        {
            // Authorization check: Only admin or the user themselves can update their info
            if (!isAdmin && id != requesterId)
                throw new AppException(
                    CommonMessageConstants.UNAUTHORIZED,
                    "You are not authorized to update this user's information",
                    StatusCodes.Status403Forbidden
                );

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "User"),
                    StatusCodes.Status404NotFound
                );

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName;

            if (!string.IsNullOrWhiteSpace(request.Phone))
                user.Phone = request.Phone;

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                user.AvatarUrl = request.AvatarUrl;

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
                user.PasswordHash = _userHelper.HashPassword(request.NewPassword);

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            // Get updated user with role information
            var updatedUser = await _userRepository.GetByIdWithRoleAsync(user.UserId);
            var userResponse = _mapper.Map<UserResponseDto>(updatedUser);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "User updated successfully",
                Data = userResponse,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "User"),
                    StatusCodes.Status404NotFound
                );

            // Optional: Check if user has related data that prevents deletion
            // For example, check if user is referenced in other tables

            await _userRepository.DeleteAsync(user);
            await _userRepository.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "User deleted successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetUsersByRoleAsync(int? roleId)
        {
            IEnumerable<User> users;

            if (roleId.HasValue)
            {
                // Filter by role
                users = await _userRepository.GetByRoleAsync(roleId.Value);
            }
            else
            {
                // Get all users
                users = await _userRepository.GetAllAsync();
            }

            var userDtos = _mapper.Map<IEnumerable<UserResponseDto>>(users);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Users retrieved successfully",
                Data = userDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}
