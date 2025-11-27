using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.Services.Email;
using AppBackend.Services.Services.OTP;
using AppBackend.Services.ServicesHelpers;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.Password
{
    public class PasswordService : IPasswordService
    {
        private readonly IUserRepository _userRepository;
        private readonly IOTPService _otpService;
        private readonly IEmailService _emailService;
        private readonly UserHelper _userHelper;

        public PasswordService(
            IUserRepository userRepository,
            IOTPService otpService,
            IEmailService emailService,
            UserHelper userHelper)
        {
            _userRepository = userRepository;
            _otpService = otpService;
            _emailService = emailService;
            _userHelper = userHelper;
        }

        public async Task<ResultModel<SendOtpResponse>> SendOTPAsync(SendOtpRequest request)
        {
            try
            {
                // Check if email exists
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    // For security, don't reveal if email exists or not
                    // Return success message even if email doesn't exist
                    return new ResultModel<SendOtpResponse>
                    {
                        IsSuccess = true,
                        ResponseCode = CommonMessageConstants.SUCCESS,
                        Message = "If the email exists, an OTP has been sent.",
                        StatusCode = StatusCodes.Status200OK,
                        Data = new SendOtpResponse
                        {
                            Email = request.Email,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                            Message = "If the email exists, an OTP has been sent."
                        }
                    };
                }

                // Generate and store OTP
                var otp = _otpService.GenerateAndStoreOTP(request.Email);

                // Send email with OTP
                var subject = "Password Reset OTP - IoT-SRM";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Password Reset Request</h2>
                        <p>Hello {user.FullName ?? "User"},</p>
                        <p>You have requested to reset your password. Please use the following OTP to verify your identity:</p>
                        <div style='background-color: #f0f0f0; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; margin: 20px 0;'>
                            {otp}
                        </div>
                        <p>This OTP will expire in 10 minutes.</p>
                        <p>If you did not request this password reset, please ignore this email.</p>
                        <p>Best regards,<br/>IoT-SRM Team</p>
                    </body>
                    </html>";

                await _emailService.SendEmail(request.Email, subject, body);

                return new ResultModel<SendOtpResponse>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "OTP has been sent to your email.",
                    StatusCode = StatusCodes.Status200OK,
                    Data = new SendOtpResponse
                    {
                        Email = request.Email,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                        Message = "OTP has been sent to your email."
                    }
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<SendOtpResponse>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Error sending OTP: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Data = null
                };
            }
        }

        public Task<ResultModel<VerifyOtpResponse>> VerifyOTPAsync(VerifyOtpRequest request)
        {
            try
            {
                // Verify OTP
                var isValid = _otpService.VerifyOTP(request.Email, request.Otp);

                if (!isValid)
                {
                    return Task.FromResult(new ResultModel<VerifyOtpResponse>
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "Invalid or expired OTP.",
                        StatusCode = StatusCodes.Status400BadRequest,
                        Data = new VerifyOtpResponse
                        {
                            IsValid = false,
                            Message = "Invalid or expired OTP."
                        }
                    });
                }

                return Task.FromResult(new ResultModel<VerifyOtpResponse>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "OTP verified successfully.",
                    StatusCode = StatusCodes.Status200OK,
                    Data = new VerifyOtpResponse
                    {
                        IsValid = true,
                        Message = "OTP verified successfully."
                    }
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ResultModel<VerifyOtpResponse>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Error verifying OTP: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Data = new VerifyOtpResponse
                    {
                        IsValid = false,
                        Message = $"Error verifying OTP: {ex.Message}"
                    }
                });
            }
        }

        public async Task<ResultModel> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                // Verify OTP first
                var isValid = _otpService.VerifyOTP(request.Email, request.Otp);
                if (!isValid)
                {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.INVALID,
                    Message = "Invalid or expired OTP.",
                    StatusCode = StatusCodes.Status400BadRequest
                };
                }

                // Get user by email
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = "User not found.",
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                // Hash new password
                var hashedPassword = _userHelper.HashPassword(request.NewPassword);

                // Update password
                user.PasswordHash = hashedPassword;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                // Remove OTP from cache after successful password change
                _otpService.RemoveOTP(request.Email);

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Password changed successfully.",
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = $"Error changing password: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}

