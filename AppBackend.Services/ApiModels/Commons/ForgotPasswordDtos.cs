using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.Commons
{
    /// <summary>
    /// DTO for sending OTP to email
    /// </summary>
    public class SendOtpRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;
    }

    /// <summary>
    /// DTO for verifying OTP
    /// </summary>
    public class VerifyOtpRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must contain only digits")]
        public string Otp { get; set; } = null!;
    }

    /// <summary>
    /// DTO for changing password after OTP verification
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must contain only digits")]
        public string Otp { get; set; } = null!;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string NewPassword { get; set; } = null!;
    }

    /// <summary>
    /// DTO for OTP response
    /// </summary>
    public class SendOtpResponse
    {
        public string Email { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; } = null!;
    }

    /// <summary>
    /// DTO for OTP verification response
    /// </summary>
    public class VerifyOtpResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = null!;
    }
}
