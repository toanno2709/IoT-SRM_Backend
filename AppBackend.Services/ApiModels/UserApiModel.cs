using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels
{
    #region User Management DTOs

    /// <summary>
    /// DTO for creating new user (Admin only)
    /// </summary>
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(255, ErrorMessage = "Full name cannot exceed 255 characters")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; } = null!;

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 digits")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Role ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Role ID must be greater than 0")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; } = null!;
    }

    /// <summary>
    /// DTO for updating user (Admin only)
    /// </summary>
    public class UpdateUserRequest
    {
        [StringLength(255, ErrorMessage = "Full name cannot exceed 255 characters")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 digits")]
        public string? Phone { get; set; }

        [Url(ErrorMessage = "Avatar URL must be a valid URL")]
        public string? AvatarUrl { get; set; }

        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string? NewPassword { get; set; }
    }

    /// <summary>
    /// DTO for user response (without password)
    /// </summary>
    public class UserResponseDto
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    #endregion

    #region Authentication DTOs

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = null!;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;
        
        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = null!;
        
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? Phone { get; set; }
    }

    public class LoginRequest
    {
        [Required, EmailAddress] 
        public string Email { get; set; } = null!;
        
        [Required] 
        public string Password { get; set; } = null!;
    }

    #endregion

    #region Legacy DTOs (for backward compatibility)

    public class UserDto
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public int? RoleId { get; set; }
        public string? AvatarUrl { get; set; }
    }

    #endregion
}