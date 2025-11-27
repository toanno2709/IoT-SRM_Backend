using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.Authentication;
using AppBackend.Services.Services.Password;
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
        private readonly IPasswordService _passwordService;

        public AuthenticationController(
            IAuthenticationService authenticationService,
            IPasswordService passwordService)
        {
            _authenticationService = authenticationService;
            _passwordService = passwordService;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="request">Registration request payload</param>
        /// <returns>JWT access token and refresh token</returns>
        /// <response code="201">User registered successfully</response>
        /// <response code="400">Invalid input data or email already exists</response>
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
        /// <returns>JWT access token and refresh token</returns>
        /// <response code="200">Login successful</response>
        /// <response code="401">Invalid credentials</response>
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

        /// <summary>
        /// Send OTP to email for password reset
        /// </summary>
        /// <param name="request">Email address</param>
        /// <returns>OTP sent confirmation</returns>
        /// <response code="200">OTP sent successfully (or email doesn't exist for security)</response>
        /// <response code="400">Invalid email format</response>
        [HttpPost("forgot-password/send-otp")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 5, windowSeconds: 300, strategy: "fixed")]
        public async Task<IActionResult> SendOTP([FromBody] SendOtpRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_INPUT",
                    Message = "Invalid input data",
                    StatusCode = 400
                });

            var result = await _passwordService.SendOTPAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Verify OTP for password reset
        /// </summary>
        /// <param name="request">Email and OTP</param>
        /// <returns>OTP verification result</returns>
        /// <response code="200">OTP verified successfully</response>
        /// <response code="400">Invalid or expired OTP</response>
        [HttpPost("forgot-password/verify-otp")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 10, windowSeconds: 60, strategy: "fixed")]
        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOtpRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_INPUT",
                    Message = "Invalid input data",
                    StatusCode = 400
                });

            var result = await _passwordService.VerifyOTPAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Change password after OTP verification (No authentication required)
        /// </summary>
        /// <param name="request">Email, OTP, and new password</param>
        /// <returns>Password change confirmation</returns>
        /// <response code="200">Password changed successfully</response>
        /// <response code="400">Invalid OTP or password requirements not met</response>
        /// <response code="404">User not found</response>
        [HttpPost("forgot-password/change-password")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 5, windowSeconds: 300, strategy: "fixed")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_INPUT",
                    Message = "Invalid input data",
                    StatusCode = 400
                });

            var result = await _passwordService.ChangePasswordAsync(request);
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
