using Microsoft.Extensions.Caching.Memory;

namespace AppBackend.Services.Services.OTP
{
    public class OTPService : IOTPService
    {
        private readonly IMemoryCache _memoryCache;
        private const int OTP_EXPIRY_MINUTES = 10;
        private const string OTP_CACHE_PREFIX = "OTP_";

        public OTPService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public string GenerateAndStoreOTP(string email)
        {
            // Generate 6-digit OTP
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();

            // Store in cache with expiry
            var cacheKey = $"{OTP_CACHE_PREFIX}{email.ToLowerInvariant()}";
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES),
                SlidingExpiration = null
            };

            _memoryCache.Set(cacheKey, otp, cacheOptions);

            return otp;
        }

        public bool VerifyOTP(string email, string otp)
        {
            var cacheKey = $"{OTP_CACHE_PREFIX}{email.ToLowerInvariant()}";
            
            if (!_memoryCache.TryGetValue(cacheKey, out string? storedOtp))
            {
                return false; // OTP not found or expired
            }

            return storedOtp == otp;
        }

        public void RemoveOTP(string email)
        {
            var cacheKey = $"{OTP_CACHE_PREFIX}{email.ToLowerInvariant()}";
            _memoryCache.Remove(cacheKey);
        }
    }
}

