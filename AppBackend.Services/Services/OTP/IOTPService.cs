namespace AppBackend.Services.Services.OTP
{
    public interface IOTPService
    {
        /// <summary>
        /// Generate and store OTP for email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Generated OTP (6 digits)</returns>
        string GenerateAndStoreOTP(string email);

        /// <summary>
        /// Verify OTP for email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="otp">OTP to verify</param>
        /// <returns>True if OTP is valid, false otherwise</returns>
        bool VerifyOTP(string email, string otp);

        /// <summary>
        /// Remove OTP from cache (after successful verification)
        /// </summary>
        /// <param name="email">Email address</param>
        void RemoveOTP(string email);
    }
}

