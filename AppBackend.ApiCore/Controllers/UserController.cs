using AppBackend.Services;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Attributes;
using System.Security.Claims;

namespace AppBackend.Api.Controllers
{
    /// <summary>
    /// APIs for managing users
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        #region Query Users

        /// <summary>
        /// Get all users (Admin and Instructor can access)
        /// </summary>
        /// <returns>List of users</returns>
        /// <response code="200">Users retrieved successfully</response>
        /// <response code="403">Forbidden (not enough role permissions)</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Instructor")]
        [RateLimit(5, 30)] 
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get current authenticated user information
        /// </summary>
        /// <returns>Current user details with role information</returns>
        /// <response code="200">User information retrieved successfully</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        /// <response code="404">User not found</response>
        [HttpGet("me")]
        [Authorize]
        [RateLimit(permitLimit: 30, windowSeconds: 60, strategy: "fixed")]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "UNAUTHORIZED",
                    Message = "User not authenticated",
                    StatusCode = 401
                });
            }

            var result = await _userService.GetCurrentUserInfoAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get user detail by Id (Admin and Instructor can access)
        /// </summary>
        /// <param name="id">User Id</param>
        /// <returns>User detail</returns>
        /// <response code="200">User retrieved successfully</response>
        /// <response code="404">User not found</response>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Instructor")]
        [RateLimit(permitLimit: 10, windowSeconds: 30, strategy: "token")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get users filtered by role (Admin and Instructor can access)
        /// </summary>
        /// <param name="roleId">Optional role ID filter</param>
        /// <returns>List of users matching the role filter</returns>
        /// <response code="200">Users retrieved successfully</response>
        /// <response code="400">Invalid role ID</response>
        [HttpGet("by-role")]
        [Authorize(Roles = "Admin,Instructor")]
        [RateLimit(permitLimit: 10, windowSeconds: 30)]
        public async Task<IActionResult> GetUsersByRole([FromQuery] int? roleId)
        {
            var result = await _userService.GetUsersByRoleAsync(roleId);
            return StatusCode(result.StatusCode, result);
        }

        #endregion

        #region User Management

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
        /// Import multiple users from Excel file (Admin only)
        /// </summary>
        /// <param name="request">Excel file and import configuration</param>
        /// <returns>Import result with successful and failed user counts</returns>
        /// <response code="200">Import completed with detailed results</response>
        /// <response code="400">Invalid file format or content</response>
        /// <response code="403">Forbidden - only Admin can import users</response>
        /// <remarks>
        /// Expected Excel format:
        /// - Column A: No (row number)
        /// - Column B: Fullname (required)
        /// - Column C: Email (required, must be unique)
        /// - Column D: Phone (optional, must be unique if provided)
        /// - Column E: Role (Student/Instructor/Admin, defaults to Student)
        /// - Column F: Password (optional, defaults to "12345678")
        /// 
        /// File constraints:
        /// - Max file size: 10MB
        /// - Supported formats: .xlsx, .xls
        /// - First row must be header (will be skipped)
        /// 
        /// Duplicate handling:
        /// - Email and phone numbers are checked against existing database records
        /// - Duplicate entries in the same file are also detected
        /// - Duplicates will be skipped and reported in the response
        /// </remarks>
        [HttpPost("import-from-excel")]
        [Authorize(Roles = "Admin")]
        [RateLimit(permitLimit: 2, windowSeconds: 300)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportUsersFromExcel([FromForm] ImportUsersFromExcelRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.ImportUsersFromExcelAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Update user information (Admin can update any user, users can update their own information)
        /// </summary>
        /// <param name="id">User ID to update</param>
        /// <param name="request">Update data (fullname, phone, avatar, password)</param>
        /// <returns>Updated user information</returns>
        /// <response code="200">User updated successfully</response>
        /// <response code="403">Forbidden - not authorized to update this user</response>
        /// <response code="404">User not found</response>
        [HttpPut("update/{id:int}")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        [RateLimit(permitLimit: 10, windowSeconds: 60)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get current user info from JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int requesterId))
            {
                return Unauthorized(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "UNAUTHORIZED",
                    Message = "User not authenticated",
                    StatusCode = 401
                });
            }

            bool isAdmin = userRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;

            var result = await _userService.UpdateUserAsync(id, request, requesterId, isAdmin);
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
