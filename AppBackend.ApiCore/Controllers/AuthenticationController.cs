using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Attributes;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for authentication and authorization (Register, Login, Logout, Refresh Token)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// Register a new user account (Default role: Student)
        /// </summary>
        /// <param name="request">Registration request payload (FullName, Email, Password, Phone)</param>
        /// <returns>JWT access token, refresh token, and user information with role</returns>
        /// <response code="201">User registered successfully with Student role</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="409">Email already exists</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 3, windowSeconds: 60, queueLimit: 1, strategy: "fixed")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_INPUT",
                    Message = "Invalid input data",
                    StatusCode = 400
                });

            var result = await _authenticationService.RegisterAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Login an existing user
        /// </summary>
        /// <param name="request">Login credentials (Email + Password)</param>
        /// <returns>JWT access token, refresh token, and user information with role details</returns>
        /// <response code="200">Login successful</response>
        /// <response code="401">Incorrect password</response>
        /// <response code="404">User not found with this email</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 5, windowSeconds: 60, queueLimit: 2, strategy: "sliding")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_INPUT",
                    Message = "Invalid input data",
                    StatusCode = 400
                });

            var result = await _authenticationService.LoginAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        /// <returns>Logout confirmation</returns>
        /// <response code="200">Logout successful</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        /// <response code="404">User not found</response>
        [HttpPost("logout")]
        [Authorize]
        [RateLimit(permitLimit: 10, windowSeconds: 60, strategy: "fixed")]
        public async Task<IActionResult> Logout()
        {
            // Get user ID from JWT claims
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

            var result = await _authenticationService.LogoutAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="request">Refresh token</param>
        /// <returns>New access token and refresh token</returns>
        /// <response code="200">Token refreshed successfully</response>
        /// <response code="401">Invalid or expired refresh token</response>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 10, windowSeconds: 60, strategy: "fixed")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_INPUT",
                    Message = "Refresh token is required",
                    StatusCode = 400
                });

            var result = await _authenticationService.RefreshTokenAsync(request.RefreshToken);
            return StatusCode(result.StatusCode, result);
        }
    }

    /// <summary>
    /// Request DTO for refresh token endpoint
    /// </summary>
    public class RefreshTokenRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
