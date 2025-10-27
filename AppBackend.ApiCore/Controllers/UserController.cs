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
<<<<<<< Updated upstream
    /// APIs for managing users (Register, Login, Query Users)
=======
    /// APIs for user management
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
        /// <summary>
        /// Register a new user account
=======
        #region User Profile

        /// <summary>
        /// Get current authenticated user information
>>>>>>> Stashed changes
        /// </summary>
        /// <returns>Current user details</returns>
        /// <response code="200">User information retrieved successfully</response>
        /// <response code="401">Unauthorized - Login required</response>
        /// <response code="404">User not found</response>
        [HttpGet("me")]
        [Authorize]
        [RateLimit(permitLimit: 20, windowSeconds: 60)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "UNAUTHORIZED",
                    Message = "Invalid token",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            }

            var result = await _userService.GetCurrentUserAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get all users (only for Admin or Manager)
        /// </summary>
        /// <returns>List of users</returns>
        /// <response code="200">Users retrieved successfully</response>
        /// <response code="403">Forbidden (not enough role permissions)</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        [RateLimit(5, 30)] 
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get user detail by Id
        /// </summary>
        /// <param name="id">User Id</param>
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
    }
}
