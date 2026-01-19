using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace AppBackend.Services.ServicesHelpers
{
    public class FirebaseHelper
    {
        private readonly IConfiguration _configuration;

        public FirebaseHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Decode Firebase JWT token to extract user information
        /// No verification - just decode the token to get claims
        /// </summary>
        /// <param name="firebaseToken">Firebase ID token from frontend</param>
        /// <returns>Dictionary of claims from the token</returns>
        public Dictionary<string, string> DecodeFirebaseToken(string firebaseToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                
                // Check if token can be read
                if (!handler.CanReadToken(firebaseToken))
                {
                    throw new Exception("Invalid JWT token format");
                }

                // Decode token without verification
                var jwtToken = handler.ReadJwtToken(firebaseToken);
                
                // Extract claims to dictionary
                var claims = new Dictionary<string, string>();
                
                foreach (var claim in jwtToken.Claims)
                {
                    // Handle multiple claims with same type by taking the first one
                    if (!claims.ContainsKey(claim.Type))
                    {
                        claims[claim.Type] = claim.Value;
                    }
                }

                return claims;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error decoding Firebase token: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract user information from decoded token claims
        /// </summary>
        public (string Email, string? Name, string? Picture) ExtractUserInfo(Dictionary<string, string> claims)
        {
            var email = claims.ContainsKey("email") ? claims["email"] : string.Empty;
            var name = claims.ContainsKey("name") ? claims["name"] : null;
            var picture = claims.ContainsKey("picture") ? claims["picture"] : null;

            return (email, name, picture);
        }
    }
}
