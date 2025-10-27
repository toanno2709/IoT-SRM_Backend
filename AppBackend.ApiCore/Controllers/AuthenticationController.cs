using AppBackend.Attributes;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for authentication and authorization
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
        /// Register a new user account (Public - defaults to Student role)
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
                return BadRequest(ModelState);

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
                return BadRequest(ModelState);

            var result = await _authenticationService.LoginAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New access token with same refresh token</returns>
        /// <response code="200">Token refreshed successfully</response>
        /// <response code="401">Invalid or expired refresh token</response>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [RateLimit(permitLimit: 10, windowSeconds: 60)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authenticationService.RefreshTokenAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Logout current user (clears session and invalidates refresh token)
        /// </summary>
        /// <returns>Success message</returns>
        /// <response code="200">Logged out successfully</response>
        [HttpPost("logout")]
        [Authorize]
        [RateLimit(permitLimit: 10, windowSeconds: 60)]
        public async Task<IActionResult> Logout()
        {
            var result = await _authenticationService.LogoutAsync();
            return StatusCode(result.StatusCode, result);
        }
    }
}
