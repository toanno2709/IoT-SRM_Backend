using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Password
{
    public interface IPasswordService
    {
        /// <summary>
        /// Send OTP to email for password reset
        /// </summary>
        Task<ResultModel<SendOtpResponse>> SendOTPAsync(SendOtpRequest request);

        /// <summary>
        /// Verify OTP
        /// </summary>
        Task<ResultModel<VerifyOtpResponse>> VerifyOTPAsync(VerifyOtpRequest request);

        /// <summary>
        /// Change password after OTP verification
        /// </summary>
        Task<ResultModel> ChangePasswordAsync(ChangePasswordRequest request);
    }
}

