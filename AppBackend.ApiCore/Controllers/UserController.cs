using AppBackend.Services;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Attributes;

namespace AppBackend.Api.Controllers
{
    /// <summary>
    /// APIs for user management and authentication
    /// </summary>
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        #region Public Authentication Endpoints

        /// <summary>
        /// Register a new user account (Public - defaults to Student role)
        /// </summary>
        /// <param name="request">Registration request payload</param>
        /// <returns>JWT access token and refresh token</returns>
        /// <response code="201">User registered successfully</response>
        /// <response code="400">Invalid input data</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 3, windowSeconds: 60, queueLimit: 1, strategy: "fixed")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.RegisterAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Login an existing user
        /// </summary>
        /// <param name="request">Login credentials (Email + Password)</param>
        /// <returns>JWT access token and refresh token</returns>
        /// <response code="200">Login successful</response>
        /// <response code="401">Invalid credentials</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 5, windowSeconds: 60, queueLimit: 2, strategy: "sliding")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.LoginAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        #endregion

        #region Admin - User Management

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        /// <returns>List of all users</returns>
        /// <response code="200">Users retrieved successfully</response>
        /// <response code="403">Forbidden - Admin access required</response>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        [RateLimit(10, 30)]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get user by ID (Admin or self)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User detail</returns>
        /// <response code="200">User retrieved successfully</response>
        /// <response code="404">User not found</response>
        [HttpGet("{id:int}")]
        [Authorize]
        [RateLimit(permitLimit: 10, windowSeconds: 30, strategy: "token")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Create new user (Admin only)
        /// </summary>
        /// <param name="request">User creation data</param>
        /// <returns>Created user information</returns>
        /// <response code="201">User created successfully</response>
        /// <response code="400">Invalid role ID</response>
        /// <response code="409">Email already exists</response>
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        [RateLimit(permitLimit: 5, windowSeconds: 60)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.CreateUserAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Update user information (Admin only)
        /// </summary>
        /// <param name="id">User ID to update</param>
        /// <param name="request">Update data (phone, avatar, password)</param>
        /// <returns>Updated user information</returns>
        /// <response code="200">User updated successfully</response>
        /// <response code="404">User not found</response>
        [HttpPut("update/{id:int}")]
        [Authorize(Roles = "Admin")]
        [RateLimit(permitLimit: 10, windowSeconds: 60)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.UpdateUserAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get users filtered by role (Admin only)
        /// </summary>
        /// <param name="roleId">Optional role ID filter</param>
        /// <returns>List of users matching the role filter</returns>
        /// <response code="200">Users retrieved successfully</response>
        /// <response code="400">Invalid role ID</response>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [RateLimit(permitLimit: 10, windowSeconds: 30)]
        public async Task<IActionResult> GetUsersByRole([FromQuery] int? roleId)
        {
            var result = await _userService.GetUsersByRoleAsync(roleId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Delete user (Admin only)
        /// </summary>
        /// <param name="id">User ID to delete</param>
        /// <returns>Success status</returns>
        /// <response code="200">User deleted successfully</response>
        /// <response code="404">User not found</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [RateLimit(permitLimit: 5, windowSeconds: 60)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        #endregion
    }
}
