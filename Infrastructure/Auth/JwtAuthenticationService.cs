using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth;

public class JwtAuthenticationService(IConfiguration configuration) : IJwtAuthenticationService {
    private readonly string key      = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
    private readonly string issuer   = configuration["Jwt:Issuer"] ?? "LWService";
    private readonly string audience = configuration["Jwt:Audience"] ?? "LWService";

    public string GenerateToken(User user, DateTime expiresAt) {
        var    tokenHandler = new JwtSecurityTokenHandler();
        byte[] key          = Encoding.ASCII.GetBytes(this.key);

        var claims = new List<Claim> {
            new("UserId", user.Id.ToString()),
            new("FullName", user.FullName),
            new("SuperUser", user.SuperUser.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        // Add authorization group ID if present
        if (user.AuthorizationGroupId.HasValue) {
            claims.Add(new Claim("AuthorizationGroupId", user.AuthorizationGroupId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject            = new ClaimsIdentity(claims),
            Expires            = expiresAt,
            Issuer             = issuer,
            Audience           = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token) {
        var    tokenHandler = new JwtSecurityTokenHandler();
        byte[] key          = Encoding.ASCII.GetBytes(this.key);

        var validationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(key),
            ValidateIssuer           = true,
            ValidIssuer              = issuer,
            ValidateAudience         = true,
            ValidAudience            = audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };

        try {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch {
            return null;
        }
    }
}