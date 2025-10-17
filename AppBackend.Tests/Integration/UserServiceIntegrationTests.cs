using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.RoleRepo;
using AppBackend.Services;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ServicesHelpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace AppBackend.Tests.Integration
{
    public class UserServiceIntegrationTests : IDisposable
    {
        private readonly IOTShowroomContext _context;
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public UserServiceIntegrationTests()
        {
            // Setup InMemory Database
            var options = new DbContextOptionsBuilder<IOTShowroomContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new IOTShowroomContext(options);

            // Seed roles
            SeedRoles();

            // Setup repositories
            _userRepository = new UserRepository(_context);
            _roleRepository = new RoleRepository(_context);

            // Setup mapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RegisterRequest, User>();
                cfg.CreateMap<User, UserDto>();
            });
            var mapper = mapperConfig.CreateMapper();

            // Setup UserHelper
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var userHelper = new UserHelper(new Mock<Microsoft.Extensions.Configuration.IConfiguration>().Object);

            // Setup service
            _userService = new UserService(
                _userRepository,
                _roleRepository,
                mapper,
                userHelper,
                mockHttpContextAccessor.Object
            );
        }

        private void SeedRoles()
        {
            var roles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "Admin", Description = "Administrator" },
                new Role { RoleId = 2, RoleName = "Instructor", Description = "Instructor" },
                new Role { RoleId = 3, RoleName = "Student", Description = "Student" },
                new Role { RoleId = 4, RoleName = "Manager", Description = "Manager" }
            };

            _context.Roles.AddRange(roles);
            _context.SaveChanges();
        }

        [Fact]
        public async Task CreateUser_WithDuplicateEmail_Returns409Conflict()
        {
            // Arrange
            var existingUser = new User
            {
                FullName = "Existing User",
                Email = "duplicate@example.com",
                PasswordHash = "hashed_password",
                RoleId = 3,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddAsync(existingUser);
            await _userRepository.SaveChangesAsync();

            var request = new CreateUserRequest
            {
                FullName = "New User",
                Email = "duplicate@example.com", // Same email
                Phone = "0901234567",
                RoleId = 3,
                Password = "Password123"
            };

            // Act
            var result = await _userService.CreateUserAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("DUPLICATE_EMAIL", result.ResponseCode);
            Assert.Contains("Email already exists", result.Message);
        }

        [Fact]
        public async Task CreateUser_WithInvalidRole_Returns400BadRequest()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                FullName = "Test User",
                Email = "test@example.com",
                Phone = "0901234567",
                RoleId = 999, // Non-existent role
                Password = "Password123"
            };

            // Act
            var result = await _userService.CreateUserAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("INVALID_ROLE", result.ResponseCode);
            Assert.Contains("Role with ID 999 does not exist", result.Message);
        }

        [Fact]
        public async Task CreateUser_WithValidData_Returns201Created()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                FullName = "Valid User",
                Email = "valid@example.com",
                Phone = "0901234567",
                RoleId = 3, // Student
                Password = "ValidPass123"
            };

            // Act
            var result = await _userService.CreateUserAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(201, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal("Valid User", result.Data.FullName);
            Assert.Equal("valid@example.com", result.Data.Email);
            Assert.Equal("Student", result.Data.RoleName);
            Assert.Null(result.Data.AvatarUrl); // Should not return password
        }

        [Fact]
        public async Task UpdateUser_WithNewPassword_HashesPassword()
        {
            // Arrange
            var user = new User
            {
                FullName = "Original Name",
                Email = "update@example.com",
                PasswordHash = "old_hash",
                RoleId = 3,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var updateRequest = new UpdateUserRequest
            {
                FullName = "Updated Name",
                Phone = "0999999999",
                NewPassword = "NewPassword123"
            };

            // Act
            var result = await _userService.UpdateUserAsync(user.UserId, updateRequest);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            
            var updatedUser = await _userRepository.GetByIdAsync(user.UserId);
            Assert.NotNull(updatedUser);
            Assert.Equal("Updated Name", updatedUser.FullName);
            Assert.Equal("0999999999", updatedUser.Phone);
            Assert.NotEqual("old_hash", updatedUser.PasswordHash); // Password should be changed
        }

        [Fact]
        public async Task GetUsersByRole_WithValidRole_ReturnsFilteredUsers()
        {
            // Arrange
            var students = new List<User>
            {
                new User { FullName = "Student 1", Email = "s1@example.com", PasswordHash = "hash", RoleId = 3, CreatedAt = DateTime.UtcNow },
                new User { FullName = "Student 2", Email = "s2@example.com", PasswordHash = "hash", RoleId = 3, CreatedAt = DateTime.UtcNow }
            };
            var instructor = new User { FullName = "Instructor 1", Email = "i1@example.com", PasswordHash = "hash", RoleId = 2, CreatedAt = DateTime.UtcNow };

            await _userRepository.AddRangeAsync(students);
            await _userRepository.AddAsync(instructor);
            await _userRepository.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersByRoleAsync(3); // Student role

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
            Assert.All(result.Data, u => Assert.Equal("Student", u.RoleName));
        }

        [Fact]
        public async Task GetUsersByRole_WithNullRoleId_ReturnsAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { FullName = "Admin", Email = "admin@example.com", PasswordHash = "hash", RoleId = 1, CreatedAt = DateTime.UtcNow },
                new User { FullName = "Student", Email = "student@example.com", PasswordHash = "hash", RoleId = 3, CreatedAt = DateTime.UtcNow }
            };
            await _userRepository.AddRangeAsync(users);
            await _userRepository.SaveChangesAsync();

            // Act
            var result = await _userService.GetUsersByRoleAsync(null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
        }

        [Fact]
        public async Task DeleteUser_WithExistingUser_ReturnsSuccess()
        {
            // Arrange
            var user = new User
            {
                FullName = "To Delete",
                Email = "delete@example.com",
                PasswordHash = "hash",
                RoleId = 3,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Act
            var result = await _userService.DeleteUserAsync(user.UserId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.True(result.Data);

            var deletedUser = await _userRepository.GetByIdAsync(user.UserId);
            Assert.Null(deletedUser);
        }

        [Fact]
        public async Task DeleteUser_WithNonExistentUser_Returns404NotFound()
        {
            // Act
            var result = await _userService.DeleteUserAsync(999);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            Assert.False(result.Data);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
