
using AuthServices.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthServices.Services
{
    
        public interface ITokenService
        {
            // Generates a stringified JWT using user parameters
            string CreateToken(string userId, string email, IEnumerable<string> roles);
        }
    

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly SymmetricSecurityKey _key;

        // Injecting the JwtSettings options pattern we set up in Program.cs
        public TokenService(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;

            if (string.IsNullOrEmpty(_jwtSettings.Key))
            {
                throw new ArgumentNullException(nameof(_jwtSettings.Key), "JWT Secret Key cannot be null or empty.");
            }

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        }

        public string CreateToken(string userId, string email, IEnumerable<string> roles)
        {
            // 1. Setup the Claims (Payload Metadata)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique ID to prevent replay attacks
            };

            // Dynamically add user application roles to the identity map
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // 2. Setup the Signing Credentials (Using HMAC-SHA256)
            var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

            double totalExpiryMinutes = (_jwtSettings.ExpiryMinutes ?? 0) == 0 
                ? 60 
                : _jwtSettings.ExpiryMinutes.Value; // .Value safely extracts the int from int?

            // 3. Define the Token Blueprint Descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(totalExpiryMinutes), // Flawless compilation
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = credentials
            };

            // 4. Generate and write the finalized token string
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
    